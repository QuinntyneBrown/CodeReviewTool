// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace RepositoryService.Core.Entities;

public class Commit
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RepositoryId { get; set; } = string.Empty;
    public string Sha { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CommitDate { get; set; }
    public string[] ParentShas { get; set; } = Array.Empty<string>();
}
