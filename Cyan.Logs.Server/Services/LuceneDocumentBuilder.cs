using Lucene.Net.Documents;
using Lucene.Net.Documents.Extensions;
using Lucene.Net.Index;
using Lucene.Net.Util;
using OpenTelemetry.Proto.Logs.V1;

namespace Cyan.Logs.Server.Services;

public static class LuceneDocumentBuilder
{
    public static Document ToLuceneDocument(this LogRecord logRecord)
    {
        var doc = new Document
        {
            new StringField("@t", 
                DateTools.DateToString(GetTimestamp(logRecord), DateResolution.MILLISECOND), 
                Field.Store.YES),
            new StringField("@l", logRecord.SeverityText ?? logRecord.SeverityNumber.ToString(), Field.Store.YES),
            new TextField("@m", logRecord.Body.StringValue, Field.Store.YES)
        };

        if (GetException(logRecord) is {} exception)
        {
            doc.AddTextField("@x", exception, Field.Store.YES);
        }

        if (!logRecord.TraceId.IsEmpty)
        {
            doc.AddStringField("@i", logRecord.TraceId.ToString(), Field.Store.YES);
        }
        
        if (!logRecord.SpanId.IsEmpty)
        {
            doc.AddStringField("SpanId", logRecord.SpanId.ToString(), Field.Store.YES);
        }
        
        foreach (var field in GetProperties(logRecord))
        {
            doc.Add(field);
        }

        return doc;
    }

    private static IEnumerable<IIndexableField> GetProperties(LogRecord logRecord)
    {
        foreach (var attribute in logRecord.Attributes)
        {
            if (attribute.Key.StartsWith("exception.") || attribute.Key.StartsWith("message_template."))
            {
                continue;
            }

            var value = attribute.Value;
            yield return attribute.Value switch
            {
                _ when value.HasStringValue => new StringField(attribute.Key, value.StringValue, Field.Store.YES),
                _ when value.HasIntValue => new Int64Field(attribute.Key, value.IntValue, Field.Store.YES),
                _ when value.HasDoubleValue => new DoubleField(attribute.Key, value.DoubleValue, Field.Store.YES),
                _ when value.HasBoolValue => new StringField(attribute.Key, value.BoolValue.ToString(), Field.Store.YES),
                _ when value.HasBytesValue => new BinaryDocValuesField(attribute.Key, new BytesRef(value.BytesValue.ToByteArray())),
                _ => throw new ArgumentException("Invalid value type in properties", attribute.Key)
            };
        }
    }

    private static DateTimeOffset GetTimestamp(LogRecord logRecord)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(((long)logRecord.TimeUnixNano) / 1000 / 1000);
    }

    private static string? GetException(LogRecord logRecord)
    {
        return logRecord.Attributes.FirstOrDefault(kv => kv.Key.Equals("exception.stacktrace"))?.Value.StringValue;
    }
}