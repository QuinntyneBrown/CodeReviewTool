// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using GitAnalysis.Core.DTOs;
using GitAnalysis.Core.Entities;
using GitAnalysis.Core.Interfaces;
using GitAnalysis.Infrastructure.BackgroundServices;

namespace GitAnalysis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComparisonController : ControllerBase
{
    private readonly ILogger<ComparisonController> logger;
    private readonly IComparisonRequestRepository repository;
    private readonly ComparisonProcessorService processorService;
    private readonly IGitService gitService;

    public ComparisonController(
        ILogger<ComparisonController> logger,
        IComparisonRequestRepository repository,
        ComparisonProcessorService processorService,
        IGitService gitService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.processorService = processorService ?? throw new ArgumentNullException(nameof(processorService));
        this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
    }

    /// <summary>
    /// Request a new Git comparison. Returns immediately with Accepted status.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ComparisonResultDto>> RequestComparison(
        [FromBody] ComparisonRequestDto dto,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received comparison request for {Repo} between {Source} and {Target}",
            dto.RepositoryPath, dto.SourceBranch, dto.TargetBranch);

        var request = new GitComparisonRequest
        {
            RepositoryPath = dto.RepositoryPath,
            SourceBranch = dto.SourceBranch,
            TargetBranch = dto.TargetBranch,
            UserId = User?.Identity?.Name
        };

        request = await repository.CreateAsync(request, cancellationToken);
        await processorService.EnqueueRequestAsync(request.RequestId, cancellationToken);

        var result = new ComparisonResultDto
        {
            RequestId = request.RequestId,
            Status = request.Status.ToString()
        };

        return Accepted($"/api/comparison/{request.RequestId}", result);
    }

    /// <summary>
    /// Get the status and result of a comparison request.
    /// </summary>
    [HttpGet("{requestId}")]
    public async Task<ActionResult<ComparisonResultDto>> GetComparison(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var request = await repository.GetByIdAsync(requestId, cancellationToken);
        if (request == null)
            return NotFound();

        var result = new ComparisonResultDto
        {
            RequestId = request.RequestId,
            Status = request.Status.ToString(),
            CompletedAt = request.CompletedAt,
            ErrorMessage = request.ErrorMessage
        };

        return Ok(result);
    }

    /// <summary>
    /// Get all branches in a repository.
    /// </summary>
    [HttpGet("branches")]
    public async Task<ActionResult<IEnumerable<string>>> GetBranches(
        [FromQuery] string repositoryPath,
        CancellationToken cancellationToken)
    {
        var branches = await gitService.GetBranchesAsync(repositoryPath, cancellationToken);
        return Ok(branches);
    }
}