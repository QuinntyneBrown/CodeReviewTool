// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text;
using ReportingService.Core.Entities;
using ReportingService.Core.Interfaces;

namespace ReportingService.Infrastructure.Services;

public class ReportGenerator : IReportGenerator
{
    private readonly IStorageService _storageService;

    public ReportGenerator(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<Report> GenerateHtmlReportAsync(object data, CancellationToken cancellationToken = default)
    {
        var report = new Report
        {
            Type = "Analysis",
            Format = "HTML"
        };

        var htmlContent = GenerateHtmlContent(data);
        report.Content = htmlContent;

        var fileName = $"{report.Id}.html";
        report.StorageLocation = await _storageService.StoreReportAsync(htmlContent, fileName, cancellationToken);

        return report;
    }

    public async Task<Report> GeneratePdfReportAsync(object data, CancellationToken cancellationToken = default)
    {
        var report = new Report
        {
            Type = "Analysis",
            Format = "PDF"
        };

        var pdfContent = GeneratePdfContent(data);
        report.Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(pdfContent));

        var fileName = $"{report.Id}.pdf";
        report.StorageLocation = await _storageService.StoreReportAsync(report.Content, fileName, cancellationToken);

        return report;
    }

    public async Task<Report> GenerateSarifReportAsync(object data, CancellationToken cancellationToken = default)
    {
        var report = new Report
        {
            Type = "Analysis",
            Format = "SARIF"
        };

        var sarifContent = GenerateSarifContent(data);
        report.Content = sarifContent;

        var fileName = $"{report.Id}.sarif";
        report.StorageLocation = await _storageService.StoreReportAsync(sarifContent, fileName, cancellationToken);

        return report;
    }

    public async Task<Report> GenerateJUnitXmlReportAsync(object data, CancellationToken cancellationToken = default)
    {
        var report = new Report
        {
            Type = "Analysis",
            Format = "JUnit XML"
        };

        var junitContent = GenerateJUnitXmlContent(data);
        report.Content = junitContent;

        var fileName = $"{report.Id}.xml";
        report.StorageLocation = await _storageService.StoreReportAsync(junitContent, fileName, cancellationToken);

        return report;
    }

    public async Task<Report> GenerateCsvReportAsync(object data, CancellationToken cancellationToken = default)
    {
        var report = new Report
        {
            Type = "Analysis",
            Format = "CSV"
        };

        var csvContent = GenerateCsvContent(data);
        report.Content = csvContent;

        var fileName = $"{report.Id}.csv";
        report.StorageLocation = await _storageService.StoreReportAsync(csvContent, fileName, cancellationToken);

        return report;
    }

    private string GenerateHtmlContent(object data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head><title>Analysis Report</title></head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<h1>Analysis Report</h1>");
        sb.AppendLine($"<p>Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
        sb.AppendLine("<div class='content'>");
        sb.AppendLine($"<pre>{System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })}</pre>");
        sb.AppendLine("</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private string GeneratePdfContent(object data)
    {
        return $"PDF Report - Generated at: {DateTime.UtcNow}\nData: {System.Text.Json.JsonSerializer.Serialize(data)}";
    }

    private string GenerateSarifContent(object data)
    {
        return @"{
  ""version"": ""2.1.0"",
  ""$schema"": ""https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json"",
  ""runs"": [
    {
      ""tool"": {
        ""driver"": {
          ""name"": ""CodeReviewTool"",
          ""version"": ""1.0.0""
        }
      },
      ""results"": []
    }
  ]
}";
    }

    private string GenerateJUnitXmlContent(object data)
    {
        return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<testsuites>
  <testsuite name=""CodeReviewAnalysis"" tests=""0"" failures=""0"" errors=""0"" time=""0"">
  </testsuite>
</testsuites>";
    }

    private string GenerateCsvContent(object data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ReportId,Type,Timestamp,Data");
        sb.AppendLine($"{Guid.NewGuid()},Analysis,{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss},{System.Text.Json.JsonSerializer.Serialize(data)}");
        return sb.ToString();
    }
}
