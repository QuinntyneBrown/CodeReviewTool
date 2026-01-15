// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using ReportingService.Core.Entities;

namespace ReportingService.Core.Interfaces;

public interface IReportGenerator
{
    Task<Report> GenerateHtmlReportAsync(object data, CancellationToken cancellationToken = default);
    Task<Report> GeneratePdfReportAsync(object data, CancellationToken cancellationToken = default);
    Task<Report> GenerateSarifReportAsync(object data, CancellationToken cancellationToken = default);
    Task<Report> GenerateJUnitXmlReportAsync(object data, CancellationToken cancellationToken = default);
    Task<Report> GenerateCsvReportAsync(object data, CancellationToken cancellationToken = default);
}
