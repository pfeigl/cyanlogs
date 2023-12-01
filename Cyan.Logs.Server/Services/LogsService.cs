using Grpc.Core;
using Lucene.Net.Index;
using Lucene.Net.Search;
using OpenTelemetry.Proto.Collector.Logs.V1;

namespace Cyan.Logs.Server.Services;

public class LogsService(IndexWriter indexWriter, SearcherManager searcherManager, ILogger<LogsService> logger) : OpenTelemetry.Proto.Collector.Logs.V1.LogsService.LogsServiceBase
{
    public override Task<ExportLogsServiceResponse> Export(ExportLogsServiceRequest request, ServerCallContext context)
    {
        var logRecords = request.ResourceLogs[0].ScopeLogs[0].LogRecords;
        foreach (var logRecord in logRecords)
        {
            var document = logRecord.ToLuceneDocument();
            indexWriter.AddDocument(document);
        }
        
        indexWriter.Flush(triggerMerge: false, applyAllDeletes: false);
        searcherManager.MaybeRefresh();
        
        return Task.FromResult(new ExportLogsServiceResponse());
    }
}