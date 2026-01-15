// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace RepositoryService.Core.Entities;

public class Branch
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RepositoryId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LatestCommitSha { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
