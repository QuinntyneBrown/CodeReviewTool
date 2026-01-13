// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using RealtimeNotification.Core.Entities;
using RealtimeNotification.Core.Interfaces;

namespace RealtimeNotification.Infrastructure.Services;

/// <summary>
/// In-memory connection manager with state tracking for connection resilience.
/// </summary>
public class ConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, NotificationConnection> connections = new();

    public Task AddConnectionAsync(NotificationConnection connection, CancellationToken cancellationToken = default)
    {
        connections.TryAdd(connection.ConnectionId, connection);
        return Task.CompletedTask;
    }

    public Task RemoveConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        connections.TryRemove(connectionId, out _);
        return Task.CompletedTask;
    }

    public Task<NotificationConnection?> GetConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        connections.TryGetValue(connectionId, out var connection);
        return Task.FromResult(connection);
    }

    public Task<IEnumerable<NotificationConnection>> GetConnectionsByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var userConnections = connections.Values
            .Where(c => c.UserId == userId && c.IsActive)
            .AsEnumerable();
        return Task.FromResult(userConnections);
    }

    public Task UpdateLastActivityAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        if (connections.TryGetValue(connectionId, out var connection))
        {
            connection.LastActivityAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<NotificationConnection>> GetActiveConnectionsAsync(CancellationToken cancellationToken = default)
    {
        var activeConnections = connections.Values
            .Where(c => c.IsActive)
            .AsEnumerable();
        return Task.FromResult(activeConnections);
    }
}