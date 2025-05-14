using Contracts;
using Microsoft.Extensions.Hosting;

namespace Registration;

// 被动探测
public class Registration : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        new HeartHandles(userSecretsId: 123)//SnowflakeId.NewSnowflakeId()
                  .ToBytes(out var bytes);

        await Task.FromResult(0);
    }
}