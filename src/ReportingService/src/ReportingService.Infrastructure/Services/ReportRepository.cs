// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using ReportingService.Core.Entities;
using ReportingService.Core.Interfaces;

namespace ReportingService.Infrastructure.Services;

public class ReportRepository : IReportRepository
{
    private readonly ConcurrentDictionary<string, Report> _reports = new();

    public Task<Report> SaveAsync(Report report, CancellationToken cancellationToken = default)
    {
        _reports[report.Id] = report;
        return Task.FromResult(report);
    }

    public Task<Report?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _reports.TryGetValue(id, out var report);
        return Task.FromResult(report);
    }

    public Task<IEnumerable<Report>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Report>>(_reports.Values.ToList());
    }

    public Task<IEnumerable<Report>> GetByTypeAsync(string type, CancellationToken cancellationToken = default)
    {
        var reports = _reports.Values.Where(r => r.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult<IEnumerable<Report>>(reports);
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _reports.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
