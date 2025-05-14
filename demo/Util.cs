using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace System;

internal static class Util
{
    /// <summary>
    /// 注册Application的IHostedService服务
    /// </summary>
    internal static void AddApplicaitonHostedServices(this IServiceCollection services)
    {
        var serviceType = typeof(IHostedService);
        var implTypes = Assembly.GetExecutingAssembly()
            .ExportedTypes.Where(type => type.IsAssignableTo(serviceType) && !type.IsAbstract).ToList();
        implTypes.ForEach(implType =>
        {
            services.AddSingleton(serviceType, implType);
        });
    }
}