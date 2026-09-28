using Microsoft.Extensions.Logging;

namespace Common.Logging;

public static class StructuredLoggingExtensions
{
    /// <summary>
    /// Konsol logger'ının ILogger.BeginScope ile eklenen alanları (ör. CorrelationId) basmasını sağlar.
    /// Bunsuz BeginScope hiçbir şey yazdırmaz.
    /// </summary>
    public static ILoggingBuilder AddStructuredLogging(this ILoggingBuilder logging)
    {
        logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
        });

        return logging;
    }
}
