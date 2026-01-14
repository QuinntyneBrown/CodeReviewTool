// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR;
using RealtimeNotification.Core.DTOs;
using RealtimeNotification.Core.Entities;
using RealtimeNotification.Core.Interfaces;

namespace RealtimeNotification.Api.Hubs;

/// <summary>
/// SignalR hub for real-time notifications.
/// Supports JWT authentication and automatic reconnection.
/// </summary>
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> logger;
    private readonly IConnectionManager connectionManager;
    private readonly ISubscriptionManager subscriptionManager;

    public NotificationHub(
        ILogger<NotificationHub> logger,
        IConnectionManager connectionManager,
        ISubscriptionManager subscriptionManager)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        this.subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.Identity?.Name ?? Context.ConnectionId;
        logger.LogInformation("Client connected: {ConnectionId}, User: {UserId}", Context.ConnectionId, userId);

        var connection = new NotificationConnection
        {
            ConnectionId = Context.ConnectionId,
            UserId = userId
        };

        await connectionManager.AddConnectionAsync(connection);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await connectionManager.RemoveConnectionAsync(Context.ConnectionId);
        await subscriptionManager.RemoveSubscriptionAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to notification channels.
    /// </summary>
    public async Task Subscribe(SubscriptionRequestDto request)
    {
        var userId = Context.User?.Identity?.Name ?? Context.ConnectionId;
        logger.LogInformation("User {UserId} subscribing to channels: {Channels}",
            userId, string.Join(", ", request.Channels));

        var subscription = await subscriptionManager.CreateSubscriptionAsync(
            userId,
            Context.ConnectionId,
            request.Channels);

        await Clients.Caller.SendAsync("SubscriptionConfirmed", subscription.SubscriptionId);
    }

    /// <summary>
    /// Update subscription channels.
    /// </summary>
    public async Task UpdateSubscription(SubscriptionRequestDto request)
    {
        var subscription = await subscriptionManager.GetSubscriptionByConnectionIdAsync(Context.ConnectionId);
        
        if (subscription != null)
        {
            await subscriptionManager.UpdateSubscriptionAsync(subscription.SubscriptionId, request.Channels);
            await Clients.Caller.SendAsync("SubscriptionUpdated");
        }
    }

    /// <summary>
    /// Unsubscribe from all channels.
    /// </summary>
    public async Task Unsubscribe()
    {
        logger.LogInformation("Client {ConnectionId} unsubscribing", Context.ConnectionId);
        await subscriptionManager.RemoveSubscriptionAsync(Context.ConnectionId);
        await Clients.Caller.SendAsync("UnsubscriptionConfirmed");
    }

    /// <summary>
    /// Heartbeat to update last activity.
    /// </summary>
    public async Task Ping()
    {
        await connectionManager.UpdateLastActivityAsync(Context.ConnectionId);
        await Clients.Caller.SendAsync("Pong");
    }
}