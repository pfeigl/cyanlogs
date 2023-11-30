using System.Diagnostics;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using Serilog.Events;
using Serilog.Parsing;
using static OpenTelemetry.Proto.Logs.V1.SeverityNumber;

namespace Cyan.Logs.Server.Services;

public static class LogEventBuilder
{
    public static LogEvent ToLogEvent(this LogRecord logRecord)
    {
        var logEvent = new LogEvent(
            GetTimestamp(logRecord),
            GetLevel(logRecord),
            GetException(logRecord),
            GetMessage(logRecord),
            GetProperties(logRecord),
            logRecord.TraceId.IsEmpty ? default : ActivityTraceId.CreateFromBytes(logRecord.TraceId.ToByteArray()),
            logRecord.SpanId.IsEmpty ? default : ActivitySpanId.CreateFromBytes(logRecord.SpanId.ToByteArray())
        );

        return logEvent;
    }

    private static MessageTemplate GetMessage(LogRecord logRecord)
    {
        return new MessageTemplate(new MessageTemplateToken[] { new TextToken(logRecord.Body.StringValue) });
    }

    private static LogEventLevel GetLevel(LogRecord logRecord)
    {
        return logRecord.SeverityNumber switch
        {
            SeverityNumber.Trace or Trace2 or Trace3 or Trace4 => LogEventLevel.Verbose,
            SeverityNumber.Debug or Debug2 or Debug3 or Debug4 => LogEventLevel.Debug,
            Info or Info2 or Info3 or Info4 => LogEventLevel.Information,
            Warn or Warn2 or Warn3 or Warn4 => LogEventLevel.Warning,
            Error or Error2 or Error3 or Error4 => LogEventLevel.Error,
            Fatal or Fatal2 or Fatal3 or Fatal4 => LogEventLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static IEnumerable<LogEventProperty> GetProperties(LogRecord logRecord)
    {
        foreach (var attribute in logRecord.Attributes)
        {
            if (attribute.Key.StartsWith("exception.") || attribute.Key.StartsWith("message_template."))
            {
                continue;
            }
            
            yield return new LogEventProperty(attribute.Key, ToPropertyValue(attribute.Value)); 
        }
    }

    private static ScalarValue ToPropertyValue(AnyValue value)
    {
        return new ScalarValue(value switch
        {
            _ when value.HasStringValue => value.StringValue,
            _ when value.HasIntValue => value.IntValue,
            _ when value.HasDoubleValue => value.DoubleValue,
            _ when value.HasBoolValue => value.BoolValue,
            _ when value.HasBytesValue => value.BytesValue,
            _ => null
        });
    }

    private static DateTimeOffset GetTimestamp(LogRecord logRecord)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(((long)logRecord.TimeUnixNano) / 1000 / 1000);
    }

    private static Exception? GetException(LogRecord logRecord)
    {
        var stackTrace = logRecord.Attributes.FirstOrDefault(kv => kv.Key.Equals("exception.stacktrace"))?.Value.StringValue;
        return string.IsNullOrEmpty(stackTrace) ? null : new PreformattedException(stackTrace);
    }
}