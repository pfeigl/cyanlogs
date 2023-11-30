namespace Cyan.Logs.Server.Services;

public class PreformattedException(string formattedException) : Exception
{
    public override string ToString()
    {
        return formattedException;
    }
}