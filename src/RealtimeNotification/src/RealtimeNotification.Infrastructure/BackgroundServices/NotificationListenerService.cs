// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RealtimeNotification.Core.Entities;
using RealtimeNotification.Core.Interfaces;

namespace RealtimeNotification.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that listens to Redis Pub/Sub for notification events.
/// Routes messages to specific users based on event data.
/// </summary>
public class NotificationListenerService : BackgroundService
{
    private readonly ILogger<NotificationListenerService> logger;
    private readonly IRedisSubscriber redisSubscriber;
    private readonly INotificationPublisher notificationPublisher;
    private readonly NotificationOptions options;

    public NotificationListenerService(
        ILogger<NotificationListenerService> logger,
        IRedisSubscriber redisSubscriber,
        INotificationPublisher notificationPublisher,
        IOptions<NotificationOptions> options)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.redisSubscriber = redisSubscriber ?? throw new ArgumentNullException(nameof(redisSubscriber));
        this.notificationPublisher = notificationPublisher ?? throw new ArgumentNullException(nameof(notificationPublisher));
        this.options = options?.Value ?? new NotificationOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Notification Listener Service starting");

        foreach (var channel in options.Channels)
        {
            await redisSubscriber.SubscribeAsync(channel, async (message) =>
            {
                await HandleMessageAsync(channel, message, stoppingToken);
            }, stoppingToken);
        }

        logger.LogInformation("Subscribed to {Count} Redis channels", options.Channels.Count);

        // Keep the service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(string channel, string message, CancellationToken cancellationToken)
    {
        try
        {
            var notification = JsonSerializer.Deserialize<NotificationMessage>(message);
            if (notification == null)
            {
                logger.LogWarning("Failed to deserialize message from channel {Channel}", channel);
                return;
            }

            // Route to specific user or broadcast
            if (!string.IsNullOrEmpty(notification.TargetUserId))
            {
                await notificationPublisher.PublishToUserAsync(notification.TargetUserId, notification, cancellationToken);
            }
            else
            {
                await notificationPublisher.PublishToChannelAsync(channel, notification, cancellationToken);
            }

            logger.LogDebug("Processed notification {MessageId} from channel {Channel}", notification.MessageId, channel);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling message from channel {Channel}", channel);
        }
    }
}

public class NotificationOptions
{
    /// <summary>
    /// Redis channels to subscribe to for notifications.
    /// </summary>
    public List<string> Channels { get; set; } = new() { "notifications", "git-analysis-results" };
}