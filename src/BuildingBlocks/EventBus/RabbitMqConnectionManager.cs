using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EventBus;

internal sealed class RabbitMqConnectionManager(
    IOptions<EventBusOptions> options,
    ILogger<RabbitMqConnectionManager> logger) : IAsyncDisposable
{
    private const int MaxConnectRetries = 12;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetConnectionAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _lock.WaitAsync(ct);
        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            var factory = new ConnectionFactory
            {
                HostName = options.Value.Host,
                UserName = options.Value.User,
                Password = options.Value.Password,
            };

            for (var attempt = 1; attempt < MaxConnectRetries; attempt++)
            {
                try
                {
                    _connection = await factory.CreateConnectionAsync(ct);
                    return _connection;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "RabbitMQ'ya bağlanılamadı ({Attempt}/{Max} deneme), {Delay}s sonra tekrar denenecek",
                        attempt, MaxConnectRetries, RetryDelay.TotalSeconds);
                    await Task.Delay(RetryDelay, ct);
                }
            }

            _connection = await factory.CreateConnectionAsync(ct);
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
