using AiDebugger.Ingest;
using AiDebugger.Orchestrator;
using AiDebugger.Packager;
using AiDebugger.Publisher;
using AiDebugger.Retrieval;
using AiDebugger.Storage;
using AiDebugger.Storage.Entities;
using AiDebugger.Worker;
using AiDebugger.Worker.Configuration;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables("AIDEBUGGER_");

builder.Services.Configure<LokiOptions>(builder.Configuration.GetSection(LokiOptions.Section));
builder.Services.Configure<GitOptions>(builder.Configuration.GetSection(GitOptions.Section));
builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection(LlmOptions.Section));
builder.Services.Configure<PublisherOptions>(builder.Configuration.GetSection(PublisherOptions.Section));
builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection(WorkerOptions.Section));

var conn = builder.Configuration.GetConnectionString("Default") ?? "Host=localhost;Database=aidebugger;Username=postgres;Password=postgres";
builder.Services.AddDbContextFactory<AppDbContext>(o => o.UseNpgsql(conn));

var lokiUrl = builder.Configuration["Loki:Url"] ?? "http://localhost:3100";
builder.Services.AddHttpClient<LokiClient>(_ => { }).ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler());
builder.Services.AddSingleton(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var client = factory.CreateClient();
    return new LokiClient(client, lokiUrl);
});

var watermark = int.TryParse(builder.Configuration["Worker:WatermarkSeconds"], out var w) ? w : 120;
builder.Services.AddSingleton(sp =>
{
    var db = sp.GetRequiredService<IDbContextFactory<AppDbContext>>();
    var loki = sp.GetRequiredService<LokiClient>();
    return new LokiIngestService(loki, db, watermark);
});

builder.Services.AddSingleton<PackagerService>();

var gitPath = builder.Configuration["Git:RepoPath"] ?? "";
builder.Services.AddSingleton<IRetrievalService>(_ => new GitRetrievalService(gitPath));

var llmModel = builder.Configuration["LLM:Model"] ?? "gpt-4o-mini";
var llmKey = builder.Configuration["LLM:ApiKey"];
var llmBaseUrl = builder.Configuration["LLM:BaseUrl"] ?? "https://api.openai.com";
builder.Services.AddHttpClient("LLM", client =>
{
    client.BaseAddress = new Uri(llmBaseUrl.TrimEnd('/') + "/");
    if (!string.IsNullOrEmpty(llmKey))
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", llmKey);
});
builder.Services.AddSingleton<ILLMOrchestrator>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var client = factory.CreateClient("LLM");
    var retrieval = sp.GetRequiredService<IRetrievalService>();
    return new OpenAIOrchestrator(client, llmModel, retrieval);
});

var pubOwner = builder.Configuration["Publisher:RepoOwner"] ?? "";
var pubRepo = builder.Configuration["Publisher:RepoName"] ?? "";
var pubToken = builder.Configuration["Publisher:Token"];
var pubLabels = builder.Configuration.GetSection("Publisher:Labels").Get<List<string>>() ?? new List<string> { "ai-debugger" };
builder.Services.AddHttpClient("GitHub", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AiDebugger/1.0");
    if (!string.IsNullOrEmpty(pubToken))
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", pubToken);
});
builder.Services.AddSingleton<IPublisher>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var client = factory.CreateClient("GitHub");
    return new GitHubPublisher(client, pubOwner, pubRepo, pubLabels);
});

builder.Services.AddSingleton<PipelineService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
    await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
    if (!await db.LokiQueries.AnyAsync().ConfigureAwait(false))
    {
        db.LokiQueries.Add(new LokiQuery
        {
            Id = "default_exceptions",
            Name = "Default exceptions",
            Logql = "{job=~\".+\"} |= \"Exception\"",
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync().ConfigureAwait(false);
    }
}

await host.RunAsync().ConfigureAwait(false);
