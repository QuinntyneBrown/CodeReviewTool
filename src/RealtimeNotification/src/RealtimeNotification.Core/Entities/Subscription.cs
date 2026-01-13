// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace RealtimeNotification.Core.Entities;

/// <summary>
/// User subscription to notification channels.
/// </summary>
public class Subscription
{
    public Guid SubscriptionId { get; set; } = Guid.NewGuid();
    public required string UserId { get; set; }
    public required string ConnectionId { get; set; }
    public List<string> Channels { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}