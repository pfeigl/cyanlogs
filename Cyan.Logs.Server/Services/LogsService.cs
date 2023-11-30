using Grpc.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using OpenTelemetry.Proto.Collector.Logs.V1;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Cyan.Logs.Server.Services;

public class LogsService : OpenTelemetry.Proto.Collector.Logs.V1.LogsService.LogsServiceBase
{
    private readonly ILogger<LogsService> _logger;
    
    private readonly RenderedCompactJsonFormatter _renderer = new();

    public LogsService(ILogger<LogsService> logger)
    {
        _logger = logger;
    }

    public override Task<ExportLogsServiceResponse> Export(ExportLogsServiceRequest request, ServerCallContext context)
    {
        var logRecords = request.ResourceLogs[0].ScopeLogs[0].LogRecords;
        foreach (var logRecord in logRecords)
        {
            var logEvent = logRecord.ToLogEvent();
            _renderer.Format(logEvent, Console.Out);
        }
        
        return Task.FromResult(new ExportLogsServiceResponse());
    }
}