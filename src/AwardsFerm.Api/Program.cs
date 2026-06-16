using System.Text.Json;
using System.Text.Json.Serialization;
using AwardsFerm.Api.Hubs;
using AwardsFerm.Api.Options;
using AwardsFerm.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<YandexRsyaOptions>(builder.Configuration.GetSection(YandexRsyaOptions.SectionName));
builder.Services.AddHttpClient<YandexRsyaStatisticsService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddSingleton<SessionSlotStore>();
builder.Services.AddSingleton<SessionManager>();
builder.Services.AddSingleton<SessionRunnerService>();
builder.Services.AddHostedService<ScheduledSessionService>();
builder.Services.AddSingleton<SessionEventBroadcaster>();
builder.Services.AddSingleton<AwardsFerm.Core.Interfaces.ISessionEventReporter>(sp =>
    sp.GetRequiredService<SessionEventBroadcaster>());

builder.Services.AddHttpClient("worker", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "http://localhost:8080")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("web");
app.MapControllers();
app.MapHub<SessionHub>("/hubs/session");

app.Run();
