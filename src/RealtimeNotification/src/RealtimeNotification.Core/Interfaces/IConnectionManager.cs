// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using RealtimeNotification.Core.Entities;

namespace RealtimeNotification.Core.Interfaces;

/// <summary>
/// Manages WebSocket/SignalR connections with resilience tracking.
/// </summary>
public interface IConnectionManager
{
    Task AddConnectionAsync(NotificationConnection connection, CancellationToken cancellationToken = default);
    Task RemoveConnectionAsync(string connectionId, CancellationToken cancellationToken = default);
    Task<NotificationConnection?> GetConnectionAsync(string connectionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationConnection>> GetConnectionsByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateLastActivityAsync(string connectionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationConnection>> GetActiveConnectionsAsync(CancellationToken cancellationToken = default);
}