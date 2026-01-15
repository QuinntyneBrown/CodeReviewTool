// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace RepositoryService.Core.Entities;

public class Repository
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public GitProvider Provider { get; set; }
    public string DefaultBranch { get; set; } = "main";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public Dictionary<string, string> Configuration { get; set; } = new();
}

public enum GitProvider
{
    GitHub,
    GitLab,
    Bitbucket,
    AzureDevOps
}
