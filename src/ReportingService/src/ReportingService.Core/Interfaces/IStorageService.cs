// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace ReportingService.Core.Interfaces;

public interface IStorageService
{
    Task<string> StoreReportAsync(string content, string fileName, CancellationToken cancellationToken = default);
    Task<string> GetReportAsync(string location, CancellationToken cancellationToken = default);
    Task DeleteReportAsync(string location, CancellationToken cancellationToken = default);
}
