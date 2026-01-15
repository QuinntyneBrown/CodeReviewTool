// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Couchbase.Lite;
using Couchbase.Lite.Query;
using RepositoryService.Core.Entities;
using RepositoryService.Core.Interfaces;
using System.Text.Json;

namespace RepositoryService.Infrastructure.Repositories;

public class BranchRepository : IBranchRepository
{
    private readonly Database _database;
    private const string CollectionName = "branches";

    public BranchRepository(Database database)
    {
        _database = database;
    }

    public async Task<Branch?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var document = _database.GetDocument(id);
            return document != null ? DocumentToBranch(document) : null;
        }, cancellationToken);
    }

    public async Task<IEnumerable<Branch>> GetByRepositoryIdAsync(string repositoryId, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var query = QueryBuilder.Select(SelectResult.All())
                .From(DataSource.Database(_database))
                .Where(Expression.Property("type").EqualTo(Expression.String(CollectionName))
                    .And(Expression.Property("RepositoryId").EqualTo(Expression.String(repositoryId))));

            var results = query.Execute();
            var branches = new List<Branch>();

            foreach (var result in results)
            {
                var dict = result.GetDictionary(0);
                if (dict != null)
                {
                    var branch = JsonSerializer.Deserialize<Branch>(dict.ToJSON());
                    if (branch != null)
                    {
                        branches.Add(branch);
                    }
                }
            }

            return branches;
        }, cancellationToken);
    }

    public async Task<Branch> CreateAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var mutableDocument = new MutableDocument(branch.Id, BranchToDict(branch));
            mutableDocument.SetString("type", CollectionName);
            _database.Save(mutableDocument);
            return branch;
        }, cancellationToken);
    }

    public async Task<Branch> UpdateAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var document = _database.GetDocument(branch.Id);
            if (document == null)
            {
                throw new InvalidOperationException($"Branch with ID {branch.Id} not found");
            }

            var mutableDocument = document.ToMutable();
            foreach (var kvp in BranchToDict(branch))
            {
                mutableDocument.SetValue(kvp.Key, kvp.Value);
            }

            branch.LastUpdated = DateTime.UtcNow;
            mutableDocument.SetDate("LastUpdated", branch.LastUpdated);
            _database.Save(mutableDocument);
            return branch;
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

    private Branch DocumentToBranch(Document document)
    {
        var json = JsonSerializer.Serialize(document.ToDictionary());
        return JsonSerializer.Deserialize<Branch>(json) ?? new Branch();
    }

    private Dictionary<string, object> BranchToDict(Branch branch)
    {
        return new Dictionary<string, object>
        {
            ["Id"] = branch.Id,
            ["RepositoryId"] = branch.RepositoryId,
            ["Name"] = branch.Name,
            ["LatestCommitSha"] = branch.LatestCommitSha,
            ["LastUpdated"] = branch.LastUpdated
        };
    }
}
