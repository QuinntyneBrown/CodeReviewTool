// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Couchbase.Lite;
using Couchbase.Lite.Query;
using RepositoryService.Core.Entities;
using RepositoryService.Core.Interfaces;
using System.Text.Json;

namespace RepositoryService.Infrastructure.Repositories;

public class RepositoryRepository : IRepositoryRepository
{
    private readonly Database _database;
    private const string CollectionName = "repositories";

    public RepositoryRepository(Database database)
    {
        _database = database;
    }

    public async Task<Repository?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var document = _database.GetDocument(id);
            return document != null ? DocumentToRepository(document) : null;
        }, cancellationToken);
    }

    public async Task<IEnumerable<Repository>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var query = QueryBuilder.Select(SelectResult.All())
                .From(DataSource.Database(_database))
                .Where(Expression.Property("type").EqualTo(Expression.String(CollectionName)));

            var results = query.Execute();
            var repositories = new List<Repository>();

            foreach (var result in results)
            {
                var dict = result.GetDictionary(0);
                if (dict != null)
                {
                    var repo = JsonSerializer.Deserialize<Repository>(dict.ToJSON());
                    if (repo != null)
                    {
                        repositories.Add(repo);
                    }
                }
            }

            return repositories;
        }, cancellationToken);
    }

    public async Task<Repository> CreateAsync(Repository repository, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var mutableDocument = new MutableDocument(repository.Id, RepositoryToDict(repository));
            mutableDocument.SetString("type", CollectionName);
            _database.Save(mutableDocument);
            return repository;
        }, cancellationToken);
    }

    public async Task<Repository> UpdateAsync(Repository repository, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var document = _database.GetDocument(repository.Id);
            if (document == null)
            {
                throw new InvalidOperationException($"Repository with ID {repository.Id} not found");
            }

            var mutableDocument = document.ToMutable();
            foreach (var kvp in RepositoryToDict(repository))
            {
                mutableDocument.SetValue(kvp.Key, kvp.Value);
            }

            repository.UpdatedAt = DateTime.UtcNow;
            mutableDocument.SetDate("UpdatedAt", repository.UpdatedAt);
            _database.Save(mutableDocument);
            return repository;
        }, cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            var document = _database.GetDocument(id);
            if (document != null)
            {
                _database.Delete(document);
            }
        }, cancellationToken);
    }

    private Repository DocumentToRepository(Document document)
    {
        var json = JsonSerializer.Serialize(document.ToDictionary());
        return JsonSerializer.Deserialize<Repository>(json) ?? new Repository();
    }

    private Dictionary<string, object> RepositoryToDict(Repository repository)
    {
        return new Dictionary<string, object>
        {
            ["Id"] = repository.Id,
            ["Name"] = repository.Name,
            ["Url"] = repository.Url,
            ["LocalPath"] = repository.LocalPath,
            ["Provider"] = repository.Provider.ToString(),
            ["DefaultBranch"] = repository.DefaultBranch,
            ["CreatedAt"] = repository.CreatedAt,
            ["UpdatedAt"] = repository.UpdatedAt,
            ["IsActive"] = repository.IsActive,
            ["Configuration"] = JsonSerializer.Serialize(repository.Configuration)
        };
    }
}
