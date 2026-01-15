// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using ReportingService.Core.Interfaces;

namespace ReportingService.Infrastructure.Services;

public class LocalStorageService : IStorageService
{
    private readonly string _basePath;

    public LocalStorageService(string? basePath = null)
    {
        _basePath = basePath ?? Path.Combine(Path.GetTempPath(), "CodeReviewTool", "Reports");
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> StoreReportAsync(string content, string fileName, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, fileName);
        await File.WriteAllTextAsync(filePath, content, cancellationToken);
        return filePath;
    }

    public async Task<string> GetReportAsync(string location, CancellationToken cancellationToken = default)
    {
        return await File.ReadAllTextAsync(location, cancellationToken);
    }

    public Task DeleteReportAsync(string location, CancellationToken cancellationToken = default)
    {
        if (File.Exists(location))
        {
            File.Delete(location);
        }
        return Task.CompletedTask;
    }
}
