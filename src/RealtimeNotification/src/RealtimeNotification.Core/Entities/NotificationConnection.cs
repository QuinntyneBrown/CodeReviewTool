// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace RealtimeNotification.Core.Entities;

/// <summary>
/// Represents an active WebSocket/SignalR connection.
/// </summary>
public class NotificationConnection
{
    public required string ConnectionId { get; set; }
    public required string UserId { get; set; }
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastActivityAt { get; set; }
    public bool IsActive { get; set; } = true;
    public Dictionary<string, string> Metadata { get; set; } = new();
}