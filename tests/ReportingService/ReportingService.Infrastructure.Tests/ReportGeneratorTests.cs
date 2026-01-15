// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using ReportingService.Infrastructure.Services;

namespace ReportingService.Infrastructure.Tests;

public class ReportGeneratorTests
{
    [Fact]
    public async Task GenerateHtmlReportAsync_ShouldCreateHtmlReport()
    {
        var storageService = new LocalStorageService();
        var generator = new ReportGenerator(storageService);
        var data = new { TestData = "Sample" };

        var report = await generator.GenerateHtmlReportAsync(data);

        Assert.NotNull(report);
        Assert.Equal("HTML", report.Format);
        Assert.Contains("<!DOCTYPE html>", report.Content);
        Assert.NotEmpty(report.StorageLocation);
    }

    [Fact]
    public async Task GeneratePdfReportAsync_ShouldCreatePdfReport()
    {
        var storageService = new LocalStorageService();
        var generator = new ReportGenerator(storageService);
        var data = new { TestData = "Sample" };

        var report = await generator.GeneratePdfReportAsync(data);

        Assert.NotNull(report);
        Assert.Equal("PDF", report.Format);
        Assert.NotEmpty(report.Content);
        Assert.NotEmpty(report.StorageLocation);
    }

    [Fact]
    public async Task GenerateSarifReportAsync_ShouldCreateSarifReport()
    {
        var storageService = new LocalStorageService();
        var generator = new ReportGenerator(storageService);
        var data = new { TestData = "Sample" };

        var report = await generator.GenerateSarifReportAsync(data);

        Assert.NotNull(report);
        Assert.Equal("SARIF", report.Format);
        Assert.Contains("\"version\": \"2.1.0\"", report.Content);
        Assert.NotEmpty(report.StorageLocation);
    }

    [Fact]
    public async Task GenerateJUnitXmlReportAsync_ShouldCreateJUnitReport()
    {
        var storageService = new LocalStorageService();
        var generator = new ReportGenerator(storageService);
        var data = new { TestData = "Sample" };

        var report = await generator.GenerateJUnitXmlReportAsync(data);

        Assert.NotNull(report);
        Assert.Equal("JUnit XML", report.Format);
        Assert.Contains("<?xml version=\"1.0\"", report.Content);
        Assert.NotEmpty(report.StorageLocation);
    }

    [Fact]
    public async Task GenerateCsvReportAsync_ShouldCreateCsvReport()
    {
        var storageService = new LocalStorageService();
        var generator = new ReportGenerator(storageService);
        var data = new { TestData = "Sample" };

        var report = await generator.GenerateCsvReportAsync(data);

        Assert.NotNull(report);
        Assert.Equal("CSV", report.Format);
        Assert.Contains("ReportId,Type,Timestamp,Data", report.Content);
        Assert.NotEmpty(report.StorageLocation);
    }
}
