using Grpc.Net.Client.Balancer;

namespace PackerAI.Infrastructure.Consul.Discover.GrpcResolver;

//https://docs.microsoft.com/zh-cn/aspnet/core/grpc/loadbalancing?view=aspnetcore-6.0
public sealed class ConsulGrpcResolver : PollingResolver
{
    private readonly Uri _address;
    private readonly int _port;
    private int _balancer = 0;
    private readonly ConsulClient _client;

    public ConsulGrpcResolver(Uri address, int defaultPort, ConsulClient client, ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        _address = address;
        _port = defaultPort;
        _client = client;
    }

    private async Task<List<BalancerAddress>> GetBalancerAsync()
    {
        var address = _address.Host.Replace("consul://", string.Empty);
        var _consulServiceProvider = new DiscoverProviderBuilder(_client).WithServiceName(address).WithCacheSeconds(5).Build();
        var results = await _consulServiceProvider.GetAllHealthServicesAsync();
        var balancer = new List<BalancerAddress>();
        results.ForEach(result =>
        {
            var addressArray = result.Split(":");
            var host = addressArray[0];
            var port = int.Parse(addressArray[1]) + 1;
            balancer.Add(new BalancerAddress(host, port));
        });
        return balancer;
    }

    protected override async Task ResolveAsync(CancellationToken cancellationToken = default)
    {
        if (Listener is not null)
            Listener(ResolverResult.ForResult(await GetBalancerAsync()));
    }

    /// <summary>
    /// 从consul注册中心获取服务如果改变就刷新平衡器
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    [Obsolete("此方法临时用于刷新注册中心服务，如果后期SDK更新请移除！")]
    public ConsulGrpcResolver RefreshAsync(CancellationToken stoppingToken = default)
    {
        Task.Factory.StartNew(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var balancer = await GetBalancerAsync();
                if (_balancer != balancer.Count)
                {
                    _balancer = balancer.Count;
                    await ResolveAsync();
                }
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        return this;
    }
}

public class ConsulGrpcResolverFactory : ResolverFactory
{
    private ConsulClient _consulClient;

    public ConsulGrpcResolverFactory(ConsulClient consulClient) => _consulClient = consulClient;

    public override string Name => "consul";

    public override Resolver Create(ResolverOptions options)
    {
        return new ConsulGrpcResolver(options.Address, options.DefaultPort, _consulClient, options.LoggerFactory).RefreshAsync();
    }
}