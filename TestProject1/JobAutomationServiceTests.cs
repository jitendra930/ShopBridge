using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ShopBridge.Configuration;
using ShopBridge.Interface;
using ShopBridge.Models.JobAutomation;
using ShopBridge.Services;

namespace ShopBridgeTest
{
    public class JobAutomationServiceTests
    {
        [Fact]
        public async Task ReviewNewJobs_QueuesEligibleLinkedInJobsForManualApproval()
        {
            var service = CreateService();
            await service.SaveProfileAsync(new CandidateProfile
            {
                FullName = "Jitendra Kumar",
                ResumeHeadline = "Backend engineer",
                YearsOfExperience = 6,
                MinimumCompensation = 100000,
                Skills = new List<string> { "C#", ".NET", "SQL", "Azure" },
                PreferredTitles = new List<string> { "Backend Engineer", "Software Engineer" },
                PreferredLocations = new List<string> { "Remote", "Bengaluru" },
                PreferredWorkModes = new List<string> { "Remote" },
                VerifiedHighlights = new List<string> { "Built production APIs", "Improved application reliability" }
            });

            await service.ImportJobsAsync(new[]
            {
                new JobPosting
                {
                    JobId = "linkedin-1",
                    Title = "Backend Engineer",
                    Company = "Contoso",
                    SourcePlatform = "LinkedIn",
                    Location = "Remote - India",
                    WorkMode = "Remote",
                    MinimumYearsExperience = 4,
                    CompensationMin = 120000,
                    RequiredSkills = new List<string> { "C#", ".NET", "SQL" }
                }
            });

            var matches = await service.ReviewNewJobsAsync();
            var queue = await service.GetQueueAsync();
            var policy = await service.GetPolicyAsync();

            Assert.Single(matches);
            Assert.Single(queue);
            Assert.True(matches.First().AddedToQueue);
            Assert.False(policy.DirectAutoSubmissionAllowed);
            Assert.Equal(ReviewQueueStatus.PendingApproval, queue.First().Status);
            Assert.Equal("ManualLinkedInSubmission", queue.First().SubmissionMode);
        }

        [Fact]
        public async Task ReviewNewJobs_RejectsBlacklistedAndLowMatchJobs()
        {
            var service = CreateService();
            await service.SaveProfileAsync(new CandidateProfile
            {
                FullName = "Jitendra Kumar",
                YearsOfExperience = 5,
                Skills = new List<string> { "C#", ".NET" },
                PreferredTitles = new List<string> { "Software Engineer" },
                PreferredLocations = new List<string> { "Remote" },
                PreferredWorkModes = new List<string> { "Remote" },
                BlacklistedCompanies = new List<string> { "Tailspin" }
            });

            await service.ImportJobsAsync(new[]
            {
                new JobPosting
                {
                    JobId = "linkedin-2",
                    Title = "Full Stack Developer",
                    Company = "Tailspin",
                    SourcePlatform = "LinkedIn",
                    Location = "Remote",
                    WorkMode = "Remote",
                    RequiredSkills = new List<string> { "JavaScript" }
                }
            });

            var matches = await service.ReviewNewJobsAsync();
            var queue = await service.GetQueueAsync();

            Assert.Single(matches);
            Assert.Empty(queue);
            Assert.False(matches.First().Eligible);
            Assert.Contains(matches.First().Reasons, reason => reason.Contains("blacklist", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task ReviewQueueAndRecordOutcome_UpdatesDashboardMetrics()
        {
            var service = CreateService();
            await service.SaveProfileAsync(new CandidateProfile
            {
                FullName = "Jitendra Kumar",
                YearsOfExperience = 7,
                MinimumCompensation = 90000,
                Skills = new List<string> { "C#", ".NET", "Azure", "Microservices" },
                PreferredTitles = new List<string> { "Software Engineer" },
                PreferredLocations = new List<string> { "Remote" },
                PreferredWorkModes = new List<string> { "Remote" },
                VerifiedHighlights = new List<string> { "Scaled APIs", "Delivered cloud services" }
            });

            await service.ImportJobsAsync(new[]
            {
                new JobPosting
                {
                    JobId = "linkedin-3",
                    Title = "Software Engineer",
                    Company = "Fabrikam",
                    SourcePlatform = "LinkedIn",
                    Location = "Remote",
                    WorkMode = "Remote",
                    MinimumYearsExperience = 5,
                    CompensationMin = 110000,
                    RequiredSkills = new List<string> { "C#", ".NET", "Azure" }
                }
            });

            await service.ReviewNewJobsAsync();
            var queueItem = (await service.GetQueueAsync()).Single();

            var reviewed = await service.ReviewQueueItemAsync(queueItem.QueueItemId, new QueueReviewRequest
            {
                Approve = true,
                ReviewerNotes = "Looks aligned with resume."
            });

            var outcome = await service.RecordOutcomeAsync(new ApplicationOutcomeRecord
            {
                QueueItemId = queueItem.QueueItemId,
                JobId = queueItem.JobId,
                Outcome = "Applied",
                GotResponse = true,
                InterviewScheduled = true
            });

            var dashboard = await service.GetDashboardAsync();

            Assert.NotNull(reviewed);
            Assert.Equal(ReviewQueueStatus.ApprovedForManualSubmission, reviewed!.Status);
            Assert.Equal("Applied", outcome.Outcome);
            Assert.Equal(1, dashboard.SubmittedApplications);
            Assert.Equal(1, dashboard.ResponsesReceived);
            Assert.Equal(1, dashboard.InterviewsScheduled);
            Assert.Equal(1, dashboard.ResponseRate);
            Assert.Equal(1, dashboard.InterviewRate);
        }

        private static JobAutomationService CreateService()
        {
            var options = Options.Create(new JobAutomationOptions());
            return new JobAutomationService(options, new FakeNotifier(), NullLogger<JobAutomationService>.Instance);
        }

        private sealed class FakeNotifier : IJobAutomationNotifier
        {
            public Task NotifyAsync(AutomationAlert alert, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}
