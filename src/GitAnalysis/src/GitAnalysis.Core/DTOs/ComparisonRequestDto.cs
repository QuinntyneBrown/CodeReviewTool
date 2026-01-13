// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace GitAnalysis.Core.DTOs;

/// <summary>
/// DTO for creating a new Git comparison request.
/// </summary>
public class ComparisonRequestDto
{
    public required string RepositoryPath { get; set; }
    public required string SourceBranch { get; set; }
    public required string TargetBranch { get; set; }
}