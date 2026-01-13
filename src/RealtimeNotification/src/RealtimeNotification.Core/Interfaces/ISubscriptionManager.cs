// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using RealtimeNotification.Core.Entities;

namespace RealtimeNotification.Core.Interfaces;

/// <summary>
/// Manages user subscriptions to notification channels.
/// </summary>
public interface ISubscriptionManager
{
    Task<Subscription> CreateSubscriptionAsync(string userId, string connectionId, List<string> channels, CancellationToken cancellationToken = default);
    Task UpdateSubscriptionAsync(Guid subscriptionId, List<string> channels, CancellationToken cancellationToken = default);
    Task RemoveSubscriptionAsync(string connectionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Subscription>> GetSubscriptionsForChannelAsync(string channel, CancellationToken cancellationToken = default);
    Task<Subscription?> GetSubscriptionByConnectionIdAsync(string connectionId, CancellationToken cancellationToken = default);
}