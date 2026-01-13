// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR;
using RealtimeNotification.Api.Hubs;
using RealtimeNotification.Core.DTOs;
using RealtimeNotification.Core.Entities;
using RealtimeNotification.Core.Interfaces;

namespace RealtimeNotification.Api.Services;

/// <summary>
/// SignalR implementation of notification publisher.
/// Provides targeted broadcasting to specific users and connections.
/// </summary>
public class SignalRNotificationPublisher : INotificationPublisher
{
    private readonly IHubContext<NotificationHub> hubContext;
    private readonly IConnectionManager connectionManager;
    private readonly ISubscriptionManager subscriptionManager;

    public SignalRNotificationPublisher(
        IHubContext<NotificationHub> hubContext,
        IConnectionManager connectionManager,
        ISubscriptionManager subscriptionManager)
    {
        this.hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        this.connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        this.subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
    }

    public async Task PublishToUserAsync(string userId, NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var connections = await connectionManager.GetConnectionsByUserIdAsync(userId, cancellationToken);
        var dto = MapToDto(message);

        foreach (var connection in connections)
        {
            await hubContext.Clients.Client(connection.ConnectionId)
                .SendAsync("ReceiveNotification", dto, cancellationToken);
        }
    }

    public async Task PublishToConnectionAsync(string connectionId, NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var dto = MapToDto(message);
        await hubContext.Clients.Client(connectionId)
            .SendAsync("ReceiveNotification", dto, cancellationToken);
    }

    public async Task PublishToAllAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var dto = MapToDto(message);
        await hubContext.Clients.All
            .SendAsync("ReceiveNotification", dto, cancellationToken);
    }

    public async Task PublishToChannelAsync(string channel, NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var subscriptions = await subscriptionManager.GetSubscriptionsForChannelAsync(channel, cancellationToken);
        var dto = MapToDto(message);

        foreach (var subscription in subscriptions)
        {
            await hubContext.Clients.Client(subscription.ConnectionId)
                .SendAsync("ReceiveNotification", dto, cancellationToken);
        }
    }

    private static NotificationDto MapToDto(NotificationMessage message)
    {
        return new NotificationDto
        {
            MessageId = message.MessageId,
            Type = message.Type,
            Payload = message.Payload,
            CreatedAt = message.CreatedAt
        };
    }
}