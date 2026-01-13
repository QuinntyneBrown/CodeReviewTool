// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using RealtimeNotification.Core.Interfaces;

namespace RealtimeNotification.Infrastructure.Services;

/// <summary>
/// Redis Pub/Sub subscriber for receiving events from event bus.
/// </summary>
public class RedisSubscriber : IRedisSubscriber
{
    private readonly ILogger<RedisSubscriber> logger;
    private readonly IConnectionMultiplexer redis;

    public RedisSubscriber(ILogger<RedisSubscriber> logger, IConnectionMultiplexer redis)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.redis = redis ?? throw new ArgumentNullException(nameof(redis));
    }

    public async Task SubscribeAsync(string channel, Func<string, Task> handler, CancellationToken cancellationToken = default)
    {
        var subscriber = redis.GetSubscriber();
        
        await subscriber.SubscribeAsync(channel, async (ch, message) =>
        {
            try
            {
                logger.LogDebug("Received message on channel {Channel}", channel);
                await handler(message.ToString());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling message from channel {Channel}", channel);
            }
        });

        logger.LogInformation("Subscribed to Redis channel: {Channel}", channel);
    }

    public async Task UnsubscribeAsync(string channel, CancellationToken cancellationToken = default)
    {
        var subscriber = redis.GetSubscriber();
        await subscriber.UnsubscribeAsync(channel);
        logger.LogInformation("Unsubscribed from Redis channel: {Channel}", channel);
    }
}