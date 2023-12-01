using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using J2N.Text;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers.Flexible.Standard;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Microsoft.AspNetCore.SignalR;

namespace Cyan.Logs.Server.Hubs;

public class LogsHub(SearcherManager searcherManager) : Hub
{
    public async IAsyncEnumerable<IDictionary<string, string>> Query(ChannelReader<string> searchStream,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await searchStream.WaitToReadAsync(cancellationToken))
        {
            while (searchStream.TryRead(out var search))
            {
                var queryParserHelper = new StandardQueryParser(new StandardAnalyzer(LuceneVersion.LUCENE_48));
                var query = string.IsNullOrEmpty(search)
                    ? new MatchAllDocsQuery()
                    : queryParserHelper.Parse(search, "@m");

                string maxTimestamp = null;

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (searchStream.TryPeek(out _))
                    {
                        break;
                    }
                    
                    var searcher = searcherManager.Acquire();

                    try
                    {
                        // We start with the newest search results
                        var revertSort = true;

                        TermRangeFilter? filter = null;
                        if (!string.IsNullOrEmpty(maxTimestamp))
                        {
                            filter = new TermRangeFilter("@t", new BytesRef(maxTimestamp), null, false, false);
                            revertSort = false;
                        }

                        var sort = new Sort(new SortField("@t", SortFieldType.STRING, revertSort));
                        var hits = searcher.Search(query, filter, 1000, sort, false, false).ScoreDocs;

                        foreach (var hit in hits)
                        {
                            var doc = searcher.Doc(hit.Doc);
                            var timestamp = doc.Get("@t");

                            if (maxTimestamp.CompareToOrdinal(timestamp) < 0)
                            {
                                maxTimestamp = timestamp;
                            }

                            var dict = doc.ToDictionary(f => f.Name, f => f.GetStringValue());
                            dict["@t"] = DateTools.StringToDate(timestamp)
                                .ToString("s", System.Globalization.CultureInfo.InvariantCulture);

                            yield return dict;
                        }
                    }
                    finally
                    {
                        searcherManager.Release(searcher);
                    }

                    await Task.Delay(1_000, cancellationToken);
                }
            }
        }
    }
}