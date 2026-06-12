using Microsoft.Extensions.Options;
using ShopBridge.Configuration;
using ShopBridge.Interface;
using ShopBridge.Models.JobAutomation;

namespace ShopBridge.Services
{
    public class JobAutomationService : IJobAutomationService
    {
        private readonly JobAutomationOptions _options;
        private readonly IJobAutomationNotifier _notifier;
        private readonly ILogger<JobAutomationService> _logger;
        private readonly object _syncRoot = new();
        private CandidateProfile? _profile;
        private readonly Dictionary<string, JobPosting> _jobs = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _reviewedJobs = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Guid, ApplicationQueueItem> _queue = new();
        private readonly List<ApplicationOutcomeRecord> _outcomes = new();
        private readonly List<AutomationAlert> _alerts = new();
        private readonly Dictionary<string, DateTimeOffset> _companyCooldowns = new(StringComparer.OrdinalIgnoreCase);

        public JobAutomationService(
            IOptions<JobAutomationOptions> options,
            IJobAutomationNotifier notifier,
            ILogger<JobAutomationService> logger)
        {
            _options = options.Value;
            _notifier = notifier;
            _logger = logger;
        }

        public Task<JobAutomationPolicy> GetPolicyAsync()
        {
            return Task.FromResult(new JobAutomationPolicy
            {
                Platform = _options.SupportedPlatform,
                DirectAutoSubmissionAllowed = _options.AllowDirectSubmission,
                RecommendedMode = "Prepare application drafts and require manual LinkedIn submission approval.",
                Guidance = "Use official integrations when available. This service never performs unsupported direct LinkedIn submission."
            });
        }

        public Task<CandidateProfile> SaveProfileAsync(CandidateProfile profile)
        {
            var normalizedProfile = new CandidateProfile
            {
                CandidateId = string.IsNullOrWhiteSpace(profile.CandidateId) ? "default" : profile.CandidateId.Trim(),
                FullName = profile.FullName.Trim(),
                ResumeHeadline = profile.ResumeHeadline.Trim(),
                YearsOfExperience = profile.YearsOfExperience,
                NeedsVisaSponsorship = profile.NeedsVisaSponsorship,
                MinimumCompensation = profile.MinimumCompensation,
                DailyApplicationCap = profile.DailyApplicationCap > 0 ? profile.DailyApplicationCap : _options.DailyApplicationCap,
                CompanyCooldownHours = profile.CompanyCooldownHours > 0 ? profile.CompanyCooldownHours : _options.CompanyCooldownHours,
                MinimumMatchScore = profile.MinimumMatchScore > 0 ? profile.MinimumMatchScore : _options.MinimumMatchScore,
                Skills = NormalizeList(profile.Skills),
                PreferredTitles = NormalizeList(profile.PreferredTitles),
                PreferredLocations = NormalizeList(profile.PreferredLocations),
                PreferredWorkModes = NormalizeList(profile.PreferredWorkModes),
                BlacklistedCompanies = NormalizeList(profile.BlacklistedCompanies),
                WhitelistedCompanies = NormalizeList(profile.WhitelistedCompanies),
                VerifiedHighlights = NormalizeList(profile.VerifiedHighlights)
            };

            lock (_syncRoot)
            {
                _profile = normalizedProfile;
            }

            return Task.FromResult(normalizedProfile);
        }

        public Task<JobImportResult> ImportJobsAsync(IEnumerable<JobPosting> jobs)
        {
            var result = new JobImportResult();

            lock (_syncRoot)
            {
                foreach (var job in jobs)
                {
                    if (string.IsNullOrWhiteSpace(job.JobId) || string.IsNullOrWhiteSpace(job.Title) || string.IsNullOrWhiteSpace(job.Company))
                    {
                        continue;
                    }

                    var normalizedJob = NormalizeJob(job);
                    if (_jobs.ContainsKey(normalizedJob.JobId))
                    {
                        result.DuplicateCount++;
                        continue;
                    }

                    _jobs[normalizedJob.JobId] = normalizedJob;
                    result.ImportedCount++;
                }
            }

            return Task.FromResult(result);
        }

        public Task<IReadOnlyCollection<JobMatchResult>> ReviewNewJobsAsync()
        {
            return Task.FromResult((IReadOnlyCollection<JobMatchResult>)ReviewNewJobsInternal());
        }

        public Task<IReadOnlyCollection<ApplicationQueueItem>> GetQueueAsync()
        {
            lock (_syncRoot)
            {
                return Task.FromResult((IReadOnlyCollection<ApplicationQueueItem>)_queue.Values
                    .OrderByDescending(item => item.CreatedAt)
                    .ToList());
            }
        }

        public Task<ApplicationQueueItem?> ReviewQueueItemAsync(Guid queueItemId, QueueReviewRequest request)
        {
            ApplicationQueueItem? updatedItem = null;
            AutomationAlert? alert = null;

            lock (_syncRoot)
            {
                if (!_queue.TryGetValue(queueItemId, out var queueItem))
                {
                    return Task.FromResult<ApplicationQueueItem?>(null);
                }

                queueItem.ReviewerNotes = request.ReviewerNotes?.Trim() ?? string.Empty;
                queueItem.UpdatedAt = DateTimeOffset.UtcNow;

                if (request.Approve)
                {
                    queueItem.Status = ReviewQueueStatus.ApprovedForManualSubmission;
                    _companyCooldowns[queueItem.Company] = DateTimeOffset.UtcNow;
                    alert = CreateAlert("Info", $"Application draft for {queueItem.Company} is ready for manual LinkedIn submission.");
                }
                else
                {
                    queueItem.Status = ReviewQueueStatus.Rejected;
                }

                updatedItem = CloneQueueItem(queueItem);
            }

            if (alert != null)
            {
                NotifyAsync(alert).GetAwaiter().GetResult();
            }

            return Task.FromResult<ApplicationQueueItem?>(updatedItem);
        }

        public Task<ApplicationOutcomeRecord> RecordOutcomeAsync(ApplicationOutcomeRecord outcome)
        {
            lock (_syncRoot)
            {
                outcome.JobId = outcome.JobId.Trim();
                outcome.Outcome = outcome.Outcome.Trim();
                outcome.Notes = outcome.Notes?.Trim() ?? string.Empty;
                outcome.RecordedAt = outcome.RecordedAt == default ? DateTimeOffset.UtcNow : outcome.RecordedAt;
                _outcomes.Add(outcome);

                if (outcome.QueueItemId.HasValue && _queue.TryGetValue(outcome.QueueItemId.Value, out var queueItem))
                {
                    queueItem.Status = ReviewQueueStatus.Submitted;
                    queueItem.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            return Task.FromResult(outcome);
        }

        public Task<AutomationDashboardSnapshot> GetDashboardAsync()
        {
            lock (_syncRoot)
            {
                var submittedCount = _queue.Values.Count(item => item.Status == ReviewQueueStatus.Submitted);
                var responseCount = _outcomes.Count(item => item.GotResponse);
                var interviewCount = _outcomes.Count(item => item.InterviewScheduled);
                var responseRate = submittedCount == 0 ? 0 : (double)responseCount / submittedCount;
                var interviewRate = submittedCount == 0 ? 0 : (double)interviewCount / submittedCount;

                return Task.FromResult(new AutomationDashboardSnapshot
                {
                    ImportedJobs = _jobs.Count,
                    QueueDepth = _queue.Count(item => item.Value.Status is ReviewQueueStatus.PendingApproval or ReviewQueueStatus.NeedsReview),
                    ApprovedForManualSubmission = _queue.Count(item => item.Value.Status == ReviewQueueStatus.ApprovedForManualSubmission),
                    SubmittedApplications = submittedCount,
                    ResponsesReceived = responseCount,
                    InterviewsScheduled = interviewCount,
                    ResponseRate = Math.Round(responseRate, 2),
                    InterviewRate = Math.Round(interviewRate, 2),
                    Alerts = _alerts.OrderByDescending(item => item.CreatedAt).Take(20).ToList()
                });
            }
        }

        public Task<ScheduledReviewSummary> RunScheduledReviewAsync(CancellationToken cancellationToken = default)
        {
            var matches = ReviewNewJobsInternal();
            var alertsRaised = matches.Sum(item => item.Reasons.Count(reason => reason.Contains("alert", StringComparison.OrdinalIgnoreCase)));
            return Task.FromResult(new ScheduledReviewSummary
            {
                JobsReviewed = matches.Count,
                QueueItemsCreated = matches.Count(item => item.AddedToQueue),
                AlertsRaised = alertsRaised
            });
        }

        private List<JobMatchResult> ReviewNewJobsInternal()
        {
            var results = new List<JobMatchResult>();
            List<AutomationAlert> alertsToSend = new();

            lock (_syncRoot)
            {
                if (_profile == null)
                {
                    var alert = CreateAlert("Warning", "Candidate profile is required before LinkedIn job review can run.");
                    alertsToSend.Add(alert);
                    return results;
                }

                foreach (var job in _jobs.Values.Where(item => !_reviewedJobs.Contains(item.JobId)).OrderByDescending(item => item.PostedAt))
                {
                    var matchResult = EvaluateJob(job, _profile);
                    results.Add(matchResult);
                    _reviewedJobs.Add(job.JobId);

                    if (!matchResult.AddedToQueue)
                    {
                        continue;
                    }

                    var queueItem = CreateQueueItem(job, _profile, matchResult);
                    _queue[queueItem.QueueItemId] = queueItem;

                    if (queueItem.Status == ReviewQueueStatus.NeedsReview)
                    {
                        alertsToSend.Add(CreateAlert("Info", $"Manual review recommended for {job.Title} at {job.Company}."));
                    }
                }
            }

            foreach (var alert in alertsToSend)
            {
                NotifyAsync(alert).GetAwaiter().GetResult();
            }

            return results;
        }

        private JobMatchResult EvaluateJob(JobPosting job, CandidateProfile profile)
        {
            var reasons = new List<string>();

            if (!job.SourcePlatform.Equals(_options.SupportedPlatform, StringComparison.OrdinalIgnoreCase))
            {
                reasons.Add($"Only {_options.SupportedPlatform} jobs are processed by this workflow.");
                return CreateRejectedResult(job, reasons);
            }

            if (profile.BlacklistedCompanies.Contains(job.Company, StringComparer.OrdinalIgnoreCase))
            {
                reasons.Add("Company is on the blacklist.");
                return CreateRejectedResult(job, reasons);
            }

            if (_queue.Values.Any(item => item.JobId.Equals(job.JobId, StringComparison.OrdinalIgnoreCase)) ||
                _outcomes.Any(item => item.JobId.Equals(job.JobId, StringComparison.OrdinalIgnoreCase)))
            {
                reasons.Add("Duplicate application prevented.");
                return CreateRejectedResult(job, reasons);
            }

            if (profile.NeedsVisaSponsorship && !job.OffersVisaSponsorship)
            {
                reasons.Add("Job does not meet visa sponsorship requirements.");
                return CreateRejectedResult(job, reasons);
            }

            if (profile.YearsOfExperience < job.MinimumYearsExperience)
            {
                reasons.Add("Job requires more experience than the candidate profile provides.");
                return CreateRejectedResult(job, reasons);
            }

            if (job.CompensationMin.HasValue && job.CompensationMin.Value < profile.MinimumCompensation)
            {
                reasons.Add("Compensation is below the minimum target.");
                return CreateRejectedResult(job, reasons);
            }

            if (HasReachedDailyCap(profile))
            {
                reasons.Add("Daily application cap reached alert.");
                return CreateRejectedResult(job, reasons);
            }

            if (IsCompanyInCooldown(job.Company, profile.CompanyCooldownHours))
            {
                reasons.Add("Company cooldown is still active alert.");
                return CreateRejectedResult(job, reasons);
            }

            var titleScore = ContainsPreferredValue(job.Title, profile.PreferredTitles) ? 0.25 : 0;
            var locationScore = ContainsPreferredValue(job.Location, profile.PreferredLocations) ? 0.15 : 0;
            var workModeScore = ContainsPreferredValue(job.WorkMode, profile.PreferredWorkModes) ? 0.1 : 0;
            var whitelistScore = profile.WhitelistedCompanies.Contains(job.Company, StringComparer.OrdinalIgnoreCase) ? 0.1 : 0;
            var compensationScore = job.CompensationMin.HasValue && job.CompensationMin.Value >= profile.MinimumCompensation ? 0.1 : 0;
            var skillScore = CalculateSkillScore(profile, job);
            var score = Math.Round(titleScore + locationScore + workModeScore + whitelistScore + compensationScore + skillScore, 2);

            reasons.Add($"Score computed as {score:0.00}.");

            if (score < profile.MinimumMatchScore)
            {
                reasons.Add("Score below minimum threshold.");
                return CreateRejectedResult(job, reasons, score);
            }

            var queueStatus = score >= _options.HumanReviewScoreThreshold
                ? ReviewQueueStatus.PendingApproval
                : ReviewQueueStatus.NeedsReview;

            reasons.Add(_options.AllowDirectSubmission
                ? "Direct submission is enabled by policy."
                : "Direct LinkedIn submission is disabled; manual approval queue created.");

            return new JobMatchResult
            {
                JobId = job.JobId,
                Company = job.Company,
                Title = job.Title,
                Score = score,
                Eligible = true,
                AddedToQueue = true,
                QueueStatus = queueStatus,
                Reasons = reasons
            };
        }

        private static double CalculateSkillScore(CandidateProfile profile, JobPosting job)
        {
            var candidateSkills = profile.Skills.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var requiredSkills = NormalizeList(job.RequiredSkills);
            var preferredSkills = NormalizeList(job.PreferredSkills);
            var totalSkills = requiredSkills.Count + preferredSkills.Count;

            if (totalSkills == 0)
            {
                return 0.3;
            }

            var matches = requiredSkills.Count(candidateSkills.Contains) + preferredSkills.Count(candidateSkills.Contains);
            return Math.Round(((double)matches / totalSkills) * 0.3, 2);
        }

        private ApplicationQueueItem CreateQueueItem(JobPosting job, CandidateProfile profile, JobMatchResult matchResult)
        {
            var highlightedSkills = job.RequiredSkills
                .Concat(job.PreferredSkills)
                .Where(skill => profile.Skills.Contains(skill, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToList();

            var highlights = profile.VerifiedHighlights.Take(2).ToList();
            if (highlightedSkills.Count == 0)
            {
                highlightedSkills.AddRange(profile.Skills.Take(3));
            }

            return new ApplicationQueueItem
            {
                JobId = job.JobId,
                Company = job.Company,
                Title = job.Title,
                MatchScore = matchResult.Score,
                Status = matchResult.QueueStatus ?? ReviewQueueStatus.PendingApproval,
                ResumeSummary = $"Target role: {job.Title}. Matching skills: {string.Join(", ", highlightedSkills)}. Highlights: {string.Join("; ", highlights)}",
                CoverLetterDraft = $"Hello {job.Company}, {profile.FullName} is a strong fit for {job.Title} with strengths in {string.Join(", ", highlightedSkills)} and experience highlighted by {string.Join("; ", highlights)}. Final submission must be reviewed before applying on LinkedIn.",
                ReviewerNotes = string.Empty,
                SubmissionMode = "ManualLinkedInSubmission"
            };
        }

        private JobMatchResult CreateRejectedResult(JobPosting job, List<string> reasons, double score = 0)
        {
            return new JobMatchResult
            {
                JobId = job.JobId,
                Company = job.Company,
                Title = job.Title,
                Score = score,
                Eligible = false,
                AddedToQueue = false,
                Reasons = reasons
            };
        }

        private bool HasReachedDailyCap(CandidateProfile profile)
        {
            var today = DateTimeOffset.UtcNow.Date;
            var cap = profile.DailyApplicationCap > 0 ? profile.DailyApplicationCap : _options.DailyApplicationCap;
            return _queue.Values.Count(item =>
                item.CreatedAt.UtcDateTime.Date == today &&
                item.Status is ReviewQueueStatus.PendingApproval or ReviewQueueStatus.NeedsReview or ReviewQueueStatus.ApprovedForManualSubmission or ReviewQueueStatus.Submitted) >= cap;
        }

        private bool IsCompanyInCooldown(string company, int cooldownHours)
        {
            if (!_companyCooldowns.TryGetValue(company, out var lastQueuedAt))
            {
                return false;
            }

            return DateTimeOffset.UtcNow < lastQueuedAt.AddHours(cooldownHours);
        }

        private static bool ContainsPreferredValue(string value, IEnumerable<string> preferredValues)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return preferredValues.Any(item => value.Contains(item, StringComparison.OrdinalIgnoreCase));
        }

        private JobPosting NormalizeJob(JobPosting job)
        {
            return new JobPosting
            {
                JobId = job.JobId.Trim(),
                Title = job.Title.Trim(),
                Company = job.Company.Trim(),
                SourcePlatform = string.IsNullOrWhiteSpace(job.SourcePlatform) ? _options.SupportedPlatform : job.SourcePlatform.Trim(),
                Location = job.Location?.Trim() ?? string.Empty,
                WorkMode = job.WorkMode?.Trim() ?? string.Empty,
                MinimumYearsExperience = job.MinimumYearsExperience,
                OffersVisaSponsorship = job.OffersVisaSponsorship,
                CompensationMin = job.CompensationMin,
                ApplicationUrl = job.ApplicationUrl?.Trim() ?? string.Empty,
                PostedAt = job.PostedAt == default ? DateTimeOffset.UtcNow : job.PostedAt,
                RequiredSkills = NormalizeList(job.RequiredSkills),
                PreferredSkills = NormalizeList(job.PreferredSkills)
            };
        }

        private AutomationAlert CreateAlert(string level, string message)
        {
            var alert = new AutomationAlert
            {
                Level = level,
                Message = message,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _alerts.Add(alert);
            _logger.LogInformation("Job automation alert [{Level}] {Message}", level, message);
            return alert;
        }

        private Task NotifyAsync(AutomationAlert alert)
        {
            return _notifier.NotifyAsync(alert);
        }

        private static List<string> NormalizeList(IEnumerable<string>? values)
        {
            return values?
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
        }

        private static ApplicationQueueItem CloneQueueItem(ApplicationQueueItem queueItem)
        {
            return new ApplicationQueueItem
            {
                QueueItemId = queueItem.QueueItemId,
                JobId = queueItem.JobId,
                Company = queueItem.Company,
                Title = queueItem.Title,
                MatchScore = queueItem.MatchScore,
                Status = queueItem.Status,
                ResumeSummary = queueItem.ResumeSummary,
                CoverLetterDraft = queueItem.CoverLetterDraft,
                ReviewerNotes = queueItem.ReviewerNotes,
                SubmissionMode = queueItem.SubmissionMode,
                CreatedAt = queueItem.CreatedAt,
                UpdatedAt = queueItem.UpdatedAt
            };
        }
    }
}
