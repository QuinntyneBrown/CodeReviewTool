// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace RealtimeNotification.Core.Interfaces;

/// <summary>
/// Subscribes to Redis Pub/Sub channels for event notifications.
/// </summary>
public interface IRedisSubscriber
{
    Task SubscribeAsync(string channel, Func<string, Task> handler, CancellationToken cancellationToken = default);
    Task UnsubscribeAsync(string channel, CancellationToken cancellationToken = default);
}