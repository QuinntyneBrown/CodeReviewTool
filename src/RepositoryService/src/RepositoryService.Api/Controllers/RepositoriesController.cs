// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CodeReviewTool.Shared.Messaging;
using Microsoft.AspNetCore.Mvc;
using RepositoryService.Core.Entities;
using RepositoryService.Core.Interfaces;
using RepositoryService.Core.Messages;

namespace RepositoryService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RepositoriesController : ControllerBase
{
    private readonly IRepositoryRepository _repositoryRepository;
    private readonly IGitService _gitService;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<RepositoriesController> _logger;

    public RepositoriesController(
        IRepositoryRepository repositoryRepository,
        IGitService gitService,
        IMessagePublisher messagePublisher,
        ILogger<RepositoriesController> logger)
    {
        _repositoryRepository = repositoryRepository;
        _gitService = gitService;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Repository>>> GetAll(CancellationToken cancellationToken)
    {
        var repositories = await _repositoryRepository.GetAllAsync(cancellationToken);
        return Ok(repositories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Repository>> GetById(string id, CancellationToken cancellationToken)
    {
        var repository = await _repositoryRepository.GetByIdAsync(id, cancellationToken);
        if (repository == null)
        {
            return NotFound();
        }

        return Ok(repository);
    }

    [HttpPost]
    public async Task<ActionResult<Repository>> Create([FromBody] CreateRepositoryRequest request, CancellationToken cancellationToken)
    {
        var repository = new Repository
        {
            Name = request.Name,
            Url = request.Url,
            LocalPath = request.LocalPath,
            Provider = request.Provider,
            DefaultBranch = request.DefaultBranch ?? "main",
            Configuration = request.Configuration ?? new Dictionary<string, string>()
        };

        try
        {
            await _gitService.CloneRepositoryAsync(repository.Url, repository.LocalPath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clone repository");
            return BadRequest(new { error = "Failed to clone repository", details = ex.Message });
        }

        var created = await _repositoryRepository.CreateAsync(repository, cancellationToken);

        var message = new RepositoryRegisteredMessage
        {
            RepositoryId = created.Id,
            Name = created.Name,
            Url = created.Url,
            Provider = created.Provider.ToString()
        };

        await _messagePublisher.PublishAsync(message, cancellationToken);
        _logger.LogInformation("Repository {Name} registered with ID {Id}", created.Name, created.Id);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Repository>> Update(string id, [FromBody] UpdateRepositoryRequest request, CancellationToken cancellationToken)
    {
        var repository = await _repositoryRepository.GetByIdAsync(id, cancellationToken);
        if (repository == null)
        {
            return NotFound();
        }

        var changes = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(request.Name) && request.Name != repository.Name)
        {
            changes["Name"] = request.Name;
            repository.Name = request.Name;
        }

        if (request.IsActive.HasValue && request.IsActive.Value != repository.IsActive)
        {
            changes["IsActive"] = request.IsActive.Value.ToString();
            repository.IsActive = request.IsActive.Value;
        }

        if (request.Configuration != null)
        {
            repository.Configuration = request.Configuration;
            changes["Configuration"] = "Updated";
        }

        var updated = await _repositoryRepository.UpdateAsync(repository, cancellationToken);

        if (changes.Count > 0)
        {
            var message = new RepositoryUpdatedMessage
            {
                RepositoryId = updated.Id,
                Name = updated.Name,
                Changes = changes
            };

            await _messagePublisher.PublishAsync(message, cancellationToken);
        }

        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var repository = await _repositoryRepository.GetByIdAsync(id, cancellationToken);
        if (repository == null)
        {
            return NotFound();
        }

        await _repositoryRepository.DeleteAsync(id, cancellationToken);

        var message = new RepositoryDeletedMessage
        {
            RepositoryId = repository.Id,
            Name = repository.Name
        };

        await _messagePublisher.PublishAsync(message, cancellationToken);
        _logger.LogInformation("Repository {Name} deleted", repository.Name);

        return NoContent();
    }
}

public record CreateRepositoryRequest(
    string Name,
    string Url,
    string LocalPath,
    GitProvider Provider,
    string? DefaultBranch,
    Dictionary<string, string>? Configuration);

public record UpdateRepositoryRequest(
    string? Name,
    bool? IsActive,
    Dictionary<string, string>? Configuration);
