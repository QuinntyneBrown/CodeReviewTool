// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using ReportingService.Core.Entities;
using ReportingService.Infrastructure.Services;

namespace ReportingService.Infrastructure.Tests;

public class ReportRepositoryTests
{
    [Fact]
    public async Task SaveAsync_ShouldStoreReport()
    {
        var repository = new ReportRepository();
        var report = new Report
        {
            Type = "Analysis",
            Format = "HTML",
            Content = "<html>Test</html>"
        };

        var result = await repository.SaveAsync(report);

        Assert.NotNull(result);
        Assert.Equal(report.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnReport()
    {
        var repository = new ReportRepository();
        var report = new Report
        {
            Type = "Analysis",
            Format = "HTML"
        };
        await repository.SaveAsync(report);

        var result = await repository.GetByIdAsync(report.Id);

        Assert.NotNull(result);
        Assert.Equal(report.Id, result.Id);
        Assert.Equal(report.Type, result.Type);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        var repository = new ReportRepository();

        var result = await repository.GetByIdAsync("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByTypeAsync_ShouldReturnReportsOfType()
    {
        var repository = new ReportRepository();
        var report1 = new Report { Type = "Analysis", Format = "HTML" };
        var report2 = new Report { Type = "Analytics", Format = "CSV" };
        await repository.SaveAsync(report1);
        await repository.SaveAsync(report2);

        var results = await repository.GetByTypeAsync("Analysis");

        Assert.Single(results);
        Assert.Equal(report1.Id, results.First().Id);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveReport()
    {
        var repository = new ReportRepository();
        var report = new Report { Type = "Analysis", Format = "HTML" };
        await repository.SaveAsync(report);

        await repository.DeleteAsync(report.Id);
        var result = await repository.GetByIdAsync(report.Id);

        Assert.Null(result);
    }
}
