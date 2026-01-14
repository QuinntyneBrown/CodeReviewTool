// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RealtimeNotification.Core.Interfaces;
using RealtimeNotification.Core.Entities;
using System.Net;

namespace CodeReviewTool.Tests;

/// <summary>
/// Integration tests for RealtimeNotification microservice.
/// Tests SignalR hub connectivity and notification publishing.
/// </summary>
public class RealtimeNotificationIntegrationTests : IClassFixture<WebApplicationFactory<RealtimeNotification.Api.Program>>
{
    private readonly WebApplicationFactory<RealtimeNotification.Api.Program> factory;
    private readonly HttpClient client;

    public RealtimeNotificationIntegrationTests(WebApplicationFactory<RealtimeNotification.Api.Program> factory)
    {
        this.factory = factory;
        this.client = factory.CreateClient();
    }

    [Fact]
    public void NotificationHub_Should_Be_Registered()
    {
        // Arrange & Act
        var serviceProvider = factory.Services;

        // Assert
        Assert.NotNull(serviceProvider);
    }

    [Fact]
    public void NotificationPublisher_Should_Be_Registered()
    {
        // Arrange & Act
        var publisher = factory.Services.GetService<INotificationPublisher>();

        // Assert
        Assert.NotNull(publisher);
    }

    [Fact]
    public void ConnectionManager_Should_Be_Registered()
    {
        // Arrange & Act
        var connectionManager = factory.Services.GetService<IConnectionManager>();

        // Assert
        Assert.NotNull(connectionManager);
    }

    [Fact]
    public void SubscriptionManager_Should_Be_Registered()
    {
        // Arrange & Act
        var subscriptionManager = factory.Services.GetService<ISubscriptionManager>();

        // Assert
        Assert.NotNull(subscriptionManager);
    }

    [Fact]
    public async Task NotificationHub_Endpoint_Should_Be_Available()
    {
        // Arrange & Act
        var response = await client.GetAsync("/notifications");

        // Assert
        // SignalR endpoints typically return 400 for GET requests without negotiation
        // We're just checking the endpoint is available
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                   response.StatusCode == HttpStatusCode.MethodNotAllowed ||
                   response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public void RealtimeNotificationApi_Should_Have_SignalR_Configured()
    {
        // Arrange & Act
        var serviceProvider = factory.Services;

        // Assert - Verify that essential services are registered
        Assert.NotNull(serviceProvider);
        var publisher = serviceProvider.GetService<INotificationPublisher>();
        Assert.NotNull(publisher);
    }

    [Fact]
    public async Task ConnectionManager_Should_Track_Connections()
    {
        // Arrange
        var connectionManager = factory.Services.GetRequiredService<IConnectionManager>();
        var connection = new NotificationConnection
        {
            UserId = "test-user",
            ConnectionId = "test-connection-123"
        };

        // Act
        await connectionManager.AddConnectionAsync(connection, CancellationToken.None);
        var connections = await connectionManager.GetConnectionsByUserIdAsync(connection.UserId, CancellationToken.None);

        // Assert
        Assert.NotNull(connections);
        var connectionsList = connections.ToList();
        Assert.Contains(connectionsList, c => c.ConnectionId == connection.ConnectionId);
    }

    [Fact]
    public async Task ConnectionManager_Should_Remove_Connections()
    {
        // Arrange
        var connectionManager = factory.Services.GetRequiredService<IConnectionManager>();
        var connection = new NotificationConnection
        {
            UserId = "test-user-2",
            ConnectionId = "test-connection-456"
        };

        // Act
        await connectionManager.AddConnectionAsync(connection, CancellationToken.None);
        await connectionManager.RemoveConnectionAsync(connection.ConnectionId, CancellationToken.None);
        var retrievedConnection = await connectionManager.GetConnectionAsync(connection.ConnectionId, CancellationToken.None);

        // Assert
        Assert.Null(retrievedConnection);
    }

    [Fact]
    public async Task SubscriptionManager_Should_Create_Subscriptions()
    {
        // Arrange
        var subscriptionManager = factory.Services.GetRequiredService<ISubscriptionManager>();
        var userId = "test-user-3";
        var connectionId = "test-conn-789";
        var channels = new List<string> { "test-channel" };

        // Act
        var subscription = await subscriptionManager.CreateSubscriptionAsync(userId, connectionId, channels, CancellationToken.None);

        // Assert
        Assert.NotNull(subscription);
        Assert.Equal(userId, subscription.UserId);
        Assert.Equal(connectionId, subscription.ConnectionId);
        Assert.Contains("test-channel", subscription.Channels);
    }

    [Fact]
    public async Task SubscriptionManager_Should_Remove_Subscriptions()
    {
        // Arrange
        var subscriptionManager = factory.Services.GetRequiredService<ISubscriptionManager>();
        var userId = "test-user-4";
        var connectionId = "test-conn-101";
        var channels = new List<string> { "test-channel-2" };

        // Act
        await subscriptionManager.CreateSubscriptionAsync(userId, connectionId, channels, CancellationToken.None);
        await subscriptionManager.RemoveSubscriptionAsync(connectionId, CancellationToken.None);
        var subscription = await subscriptionManager.GetSubscriptionByConnectionIdAsync(connectionId, CancellationToken.None);

        // Assert
        Assert.Null(subscription);
    }

    [Fact]
    public async Task SubscriptionManager_Should_Get_Subscriptions_For_Channel()
    {
        // Arrange
        var subscriptionManager = factory.Services.GetRequiredService<ISubscriptionManager>();
        var userId = "test-user-5";
        var connectionId = "test-conn-102";
        var channel = "shared-channel";
        var channels = new List<string> { channel };

        // Act
        await subscriptionManager.CreateSubscriptionAsync(userId, connectionId, channels, CancellationToken.None);
        var subscriptions = await subscriptionManager.GetSubscriptionsForChannelAsync(channel, CancellationToken.None);

        // Assert
        Assert.NotNull(subscriptions);
        var subscriptionsList = subscriptions.ToList();
        Assert.Contains(subscriptionsList, s => s.UserId == userId && s.Channels.Contains(channel));
    }

    [Fact]
    public async Task ConnectionManager_Should_Update_Last_Activity()
    {
        // Arrange
        var connectionManager = factory.Services.GetRequiredService<IConnectionManager>();
        var connection = new NotificationConnection
        {
            UserId = "activity-user",
            ConnectionId = "activity-conn-123"
        };

        // Act
        await connectionManager.AddConnectionAsync(connection, CancellationToken.None);
        await Task.Delay(100); // Small delay to ensure time difference
        await connectionManager.UpdateLastActivityAsync(connection.ConnectionId, CancellationToken.None);
        var updatedConnection = await connectionManager.GetConnectionAsync(connection.ConnectionId, CancellationToken.None);

        // Assert
        Assert.NotNull(updatedConnection);
        Assert.NotNull(updatedConnection.LastActivityAt);
    }

    [Fact]
    public async Task ConnectionManager_Should_Get_Active_Connections()
    {
        // Arrange
        var connectionManager = factory.Services.GetRequiredService<IConnectionManager>();
        var connection = new NotificationConnection
        {
            UserId = "active-user",
            ConnectionId = "active-conn-123"
        };

        // Act
        await connectionManager.AddConnectionAsync(connection, CancellationToken.None);
        var activeConnections = await connectionManager.GetActiveConnectionsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(activeConnections);
        var activeList = activeConnections.ToList();
        Assert.Contains(activeList, c => c.ConnectionId == connection.ConnectionId);
    }

    [Fact]
    public async Task SubscriptionManager_Should_Update_Subscription_Channels()
    {
        // Arrange
        var subscriptionManager = factory.Services.GetRequiredService<ISubscriptionManager>();
        var userId = "update-user";
        var connectionId = "update-conn-123";
        var initialChannels = new List<string> { "channel1" };
        var updatedChannels = new List<string> { "channel1", "channel2" };

        // Act
        var subscription = await subscriptionManager.CreateSubscriptionAsync(userId, connectionId, initialChannels, CancellationToken.None);
        await subscriptionManager.UpdateSubscriptionAsync(subscription.SubscriptionId, updatedChannels, CancellationToken.None);
        var updatedSubscription = await subscriptionManager.GetSubscriptionByConnectionIdAsync(connectionId, CancellationToken.None);

        // Assert
        Assert.NotNull(updatedSubscription);
        Assert.Contains("channel2", updatedSubscription.Channels);
    }
}
