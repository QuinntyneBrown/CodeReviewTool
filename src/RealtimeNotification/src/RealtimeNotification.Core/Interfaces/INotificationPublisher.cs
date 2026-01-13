// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using RealtimeNotification.Core.Entities;

namespace RealtimeNotification.Core.Interfaces;

/// <summary>
/// Publishes notifications to connected clients.
/// </summary>
public interface INotificationPublisher
{
    Task PublishToUserAsync(string userId, NotificationMessage message, CancellationToken cancellationToken = default);
    Task PublishToConnectionAsync(string connectionId, NotificationMessage message, CancellationToken cancellationToken = default);
    Task PublishToAllAsync(NotificationMessage message, CancellationToken cancellationToken = default);
    Task PublishToChannelAsync(string channel, NotificationMessage message, CancellationToken cancellationToken = default);
}