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

        // Configure Redis connection (optional - gracefully handle missing Redis)
        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        var useRedis = configuration.GetValue<bool>("UseRedis", false);

        if (useRedis)
        {
            try
            {
                var configOptions = ConfigurationOptions.Parse(redisConnection);
                configOptions.AbortOnConnectFail = false;
                services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(configOptions));
                services.AddSingleton<IRedisSubscriber, RedisSubscriber>();
            }
            catch (RedisConnectionException)
            {
                // Redis not available - register a null implementation
                services.AddSingleton<IRedisSubscriber, NullRedisSubscriber>();
            }
        }
        else
        {
            // Redis disabled - use in-memory only mode
            services.AddSingleton<IRedisSubscriber, NullRedisSubscriber>();
        }

        services.AddSingleton<IConnectionManager, ConnectionManager>();
        services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
        services.AddHostedService<NotificationListenerService>();

        return services;
    }
}