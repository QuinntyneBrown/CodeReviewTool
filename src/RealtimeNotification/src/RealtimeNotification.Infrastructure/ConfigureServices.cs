// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using RealtimeNotification.Core.Interfaces;
using RealtimeNotification.Infrastructure.Services;
using RealtimeNotification.Infrastructure.BackgroundServices;

namespace RealtimeNotification.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NotificationOptions>(configuration.GetSection("Notifications"));

        // Configure Redis connection
        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));

        services.AddSingleton<IConnectionManager, ConnectionManager>();
        services.AddSingleton<IRedisSubscriber, RedisSubscriber>();
        services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
        services.AddHostedService<NotificationListenerService>();

        return services;
    }
}