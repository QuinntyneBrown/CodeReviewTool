// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CodeReviewTool.Shared.Messages;
using MessagePack;

namespace RepositoryService.Core.Messages;

[MessagePackObject]
public class RepositoryPullRequestMergedMessage : IMessage
{
    [Key(0)]
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    [Key(1)]
    public string MessageType { get; set; } = nameof(RepositoryPullRequestMergedMessage);

    [Key(2)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Key(3)]
    public string RepositoryId { get; set; } = string.Empty;

    [Key(4)]
    public string PullRequestId { get; set; } = string.Empty;

    [Key(5)]
    public string Number { get; set; } = string.Empty;

    [Key(6)]
    public string MergedBy { get; set; } = string.Empty;

    [Key(7)]
    public string MergeCommitSha { get; set; } = string.Empty;
}
