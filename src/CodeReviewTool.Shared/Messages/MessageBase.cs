// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace CodeReviewTool.Shared.Messages;

public abstract class MessageBase : IMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public string MessageType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    protected MessageBase()
    {
        MessageType = GetType().Name;
    }
}
