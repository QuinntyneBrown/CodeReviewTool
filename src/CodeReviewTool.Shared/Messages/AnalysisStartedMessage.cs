// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using MessagePack;

namespace CodeReviewTool.Shared.Messages;

[MessagePackObject]
public class AnalysisStartedMessage : IMessage
{
    [Key(0)]
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    [Key(1)]
    public string MessageType { get; set; } = nameof(AnalysisStartedMessage);

    [Key(2)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Key(3)]
    public string RequestId { get; set; } = string.Empty;

    [Key(4)]
    public string RepositoryPath { get; set; } = string.Empty;

    [Key(5)]
    public string FromBranch { get; set; } = string.Empty;

    [Key(6)]
    public string IntoBranch { get; set; } = string.Empty;
}
