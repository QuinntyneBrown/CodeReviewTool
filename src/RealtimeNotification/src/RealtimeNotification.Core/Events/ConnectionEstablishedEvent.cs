// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace RealtimeNotification.Core.Events;

public record ConnectionEstablishedEvent(string ConnectionId, string UserId, DateTime ConnectedAt);