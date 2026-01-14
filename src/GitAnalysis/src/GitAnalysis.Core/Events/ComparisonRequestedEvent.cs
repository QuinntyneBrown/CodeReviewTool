// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace GitAnalysis.Core.Events;

public record ComparisonRequestedEvent(Guid RequestId, string RepositoryPath, string FromBranch, string IntoBranch, string? UserId, DateTime RequestedAt);