using Serilog;

namespace Cyan.Logs.Server.Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.OpenTelemetry(endpoint: "https://localhost:7255/")
            .CreateLogger();

        try
        {
            throw new Exception("my exception");
        }
        catch (Exception ex)
        {
            Log
                .ForContext("hello", "world")
                .ForContext("good", "bye")
                .Error(ex, "Hello World");
        }
        
        Log.CloseAndFlush();
        
    }
}