// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using ReportingService.Core.Entities;

namespace ReportingService.Core.Interfaces;

public interface IReportRepository
{
    Task<Report> SaveAsync(Report report, CancellationToken cancellationToken = default);
    Task<Report?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Report>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Report>> GetByTypeAsync(string type, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
