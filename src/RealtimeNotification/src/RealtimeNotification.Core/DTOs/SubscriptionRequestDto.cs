// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace RealtimeNotification.Core.DTOs;

/// <summary>
/// DTO for subscription requests.
/// </summary>
public class SubscriptionRequestDto
{
    public List<string> Channels { get; set; } = new();
}