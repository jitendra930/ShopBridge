using FluentAssertions.Common;
using Microsoft.EntityFrameworkCore;
using ShopBridge.Interface;
using ShopBridge.Methods;
using ShopBridge.Models;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddDbContext<IotDBContext>(op => op.UseSqlServer(builder.Configuration["ConnectionStrings:Database"]));
builder.Services.AddSingleton<IShopBridgeMethods, ShopBridgeMethods>();
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
