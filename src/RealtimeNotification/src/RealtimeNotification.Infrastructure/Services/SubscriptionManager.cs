// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using RealtimeNotification.Core.Entities;
using RealtimeNotification.Core.Interfaces;

namespace RealtimeNotification.Infrastructure.Services;

/// <summary>
/// In-memory subscription manager for channel subscriptions.
/// </summary>
public class SubscriptionManager : ISubscriptionManager
{
    private readonly ConcurrentDictionary<string, Subscription> subscriptionsByConnection = new();
    private readonly ConcurrentDictionary<string, ConcurrentBag<Subscription>> subscriptionsByChannel = new();

    public Task<Subscription> CreateSubscriptionAsync(string userId, string connectionId, List<string> channels, CancellationToken cancellationToken = default)
    {
        var subscription = new Subscription
        {
            UserId = userId,
            ConnectionId = connectionId,
            Channels = channels
        };

        subscriptionsByConnection.TryAdd(connectionId, subscription);

        foreach (var channel in channels)
        {
            subscriptionsByChannel.AddOrUpdate(
                channel,
                _ => new ConcurrentBag<Subscription> { subscription },
                (_, bag) =>
                {
                    bag.Add(subscription);
                    return bag;
                });
        }

        return Task.FromResult(subscription);
    }

    public Task UpdateSubscriptionAsync(Guid subscriptionId, List<string> channels, CancellationToken cancellationToken = default)
    {
        var subscription = subscriptionsByConnection.Values
            .FirstOrDefault(s => s.SubscriptionId == subscriptionId);

        if (subscription != null)
        {
            // Remove from old channels - mark as inactive and remove from bags
            var oldChannels = subscription.Channels.ToList();
            subscription.Channels = channels;

            foreach (var oldChannel in oldChannels)
            {
                if (subscriptionsByChannel.TryGetValue(oldChannel, out var bag))
                {
                    // Note: ConcurrentBag doesn't support removal, so we rely on IsActive flag
                    // The bag will be cleaned up naturally when GetSubscriptionsForChannelAsync filters
                }
            }

            // Add to new channel subscriptions
            foreach (var channel in channels)
            {
                subscriptionsByChannel.AddOrUpdate(
                    channel,
                    _ => new ConcurrentBag<Subscription> { subscription },
                    (_, bag) =>
                    {
                        bag.Add(subscription);
                        return bag;
                    });
            }
        }

        return Task.CompletedTask;
    }

    public Task RemoveSubscriptionAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        if (subscriptionsByConnection.TryRemove(connectionId, out var subscription))
        {
            subscription.IsActive = false;
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<Subscription>> GetSubscriptionsForChannelAsync(string channel, CancellationToken cancellationToken = default)
    {
        if (subscriptionsByChannel.TryGetValue(channel, out var bag))
        {
            return Task.FromResult(bag.Where(s => s.IsActive && s.Channels.Contains(channel)).AsEnumerable());
        }

        return Task.FromResult(Enumerable.Empty<Subscription>());
    }

    public Task<Subscription?> GetSubscriptionByConnectionIdAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        subscriptionsByConnection.TryGetValue(connectionId, out var subscription);
        return Task.FromResult(subscription);
    }
}