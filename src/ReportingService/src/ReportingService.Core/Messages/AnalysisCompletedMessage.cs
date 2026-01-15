// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CodeReviewTool.Shared.Messages;
using MessagePack;

namespace ReportingService.Core.Messages;

[MessagePackObject]
public class AnalysisCompletedMessage : IMessage
{
    [Key(0)]
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    [Key(1)]
    public string MessageType { get; set; } = nameof(AnalysisCompletedMessage);

    [Key(2)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Key(3)]
    public string AnalysisId { get; set; } = string.Empty;

    [Key(4)]
    public string RepositoryId { get; set; } = string.Empty;

    [Key(5)]
    public Dictionary<string, object> AnalysisData { get; set; } = new();
}
