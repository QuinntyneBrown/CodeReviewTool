// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using MessagePack;

namespace CodeReviewTool.Shared.Messages;

[MessagePackObject]
public abstract class MessageBase : IMessage
{
    [Key(0)]
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    [Key(1)]
    public string MessageType { get; set; } = string.Empty;

    [Key(2)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    protected MessageBase()
    {
        MessageType = GetType().Name;
    }
}
