// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace RealtimeNotification.Core.DTOs;

/// <summary>
/// DTO for notification messages.
/// </summary>
public class NotificationDto
{
    public Guid MessageId { get; set; }
    public required string Type { get; set; }
    public required string Payload { get; set; }
    public DateTime CreatedAt { get; set; }
}