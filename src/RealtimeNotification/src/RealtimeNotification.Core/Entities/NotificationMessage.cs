// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace RealtimeNotification.Core.Entities;

/// <summary>
/// Notification message to be sent to clients.
/// </summary>
public class NotificationMessage
{
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public required string Type { get; set; }
    public required string Payload { get; set; }
    public string? TargetUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
}

public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Critical
}