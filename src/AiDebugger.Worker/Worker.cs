using AiDebugger.Worker.Configuration;

namespace AiDebugger.Worker;

public class Worker : BackgroundService
{
    private readonly PipelineService _pipeline;
    private readonly ILogger<Worker> _logger;
    private readonly WorkerOptions _options;

    public Worker(PipelineService pipeline, ILogger<Worker> logger, Microsoft.Extensions.Options.IOptions<WorkerOptions> options)
    {
        _pipeline = pipeline;
        _logger = logger;
        _options = options?.Value ?? new WorkerOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI Debugger Worker started. Ingest interval: {Minutes} min", _options.IngestIntervalMinutes);
        var interval = TimeSpan.FromMinutes(_options.IngestIntervalMinutes);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _pipeline.RunAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pipeline run failed");
            }
            await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
        }
    }
}
