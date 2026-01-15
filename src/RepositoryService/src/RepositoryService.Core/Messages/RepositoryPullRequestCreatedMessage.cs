// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CodeReviewTool.Shared.Messages;
using MessagePack;

namespace RepositoryService.Core.Messages;

[MessagePackObject]
public class RepositoryPullRequestCreatedMessage : IMessage
{
    [Key(0)]
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    [Key(1)]
    public string MessageType { get; set; } = nameof(RepositoryPullRequestCreatedMessage);

    [Key(2)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Key(3)]
    public string RepositoryId { get; set; } = string.Empty;

    [Key(4)]
    public string PullRequestId { get; set; } = string.Empty;

    [Key(5)]
    public string Number { get; set; } = string.Empty;

    [Key(6)]
    public string Title { get; set; } = string.Empty;

    [Key(7)]
    public string SourceBranch { get; set; } = string.Empty;

    [Key(8)]
    public string TargetBranch { get; set; } = string.Empty;

    [Key(9)]
    public string Author { get; set; } = string.Empty;
}
