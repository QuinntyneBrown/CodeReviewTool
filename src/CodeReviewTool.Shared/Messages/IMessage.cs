// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace CodeReviewTool.Shared.Messages;

public interface IMessage
{
    string MessageId { get; }
    string MessageType { get; }
    DateTime Timestamp { get; }
}
