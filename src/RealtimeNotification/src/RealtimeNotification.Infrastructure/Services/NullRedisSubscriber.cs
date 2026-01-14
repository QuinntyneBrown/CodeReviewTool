// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using RealtimeNotification.Core.Interfaces;

namespace RealtimeNotification.Infrastructure.Services;

/// <summary>
/// Null implementation of IRedisSubscriber for when Redis is not available.
/// SignalR will still work for real-time notifications within a single instance.
/// </summary>
public class NullRedisSubscriber : IRedisSubscriber
{
    private readonly ILogger<NullRedisSubscriber> logger;

    public NullRedisSubscriber(ILogger<NullRedisSubscriber> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task SubscribeAsync(string channel, Func<string, Task> handler, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("Redis is not configured. Subscription to channel {Channel} will not receive external events.", channel);
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(string channel, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Redis is not configured. Unsubscribe from channel {Channel} is a no-op.", channel);
        return Task.CompletedTask;
    }
}
