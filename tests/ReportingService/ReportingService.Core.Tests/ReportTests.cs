// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using ReportingService.Core.Entities;

namespace ReportingService.Core.Tests;

public class ReportTests
{
    [Fact]
    public void Report_ShouldHaveDefaultValues()
    {
        var report = new Report();

        Assert.NotNull(report.Id);
        Assert.NotEmpty(report.Id);
        Assert.Equal(string.Empty, report.Type);
        Assert.Equal(string.Empty, report.Format);
        Assert.Equal(string.Empty, report.Content);
        Assert.Equal(string.Empty, report.StorageLocation);
        Assert.NotEqual(default, report.CreatedAt);
        Assert.NotNull(report.Metadata);
    }

    [Fact]
    public void Report_ShouldSetProperties()
    {
        var report = new Report
        {
            Type = "Analysis",
            Format = "HTML",
            Content = "<html>Test</html>",
            StorageLocation = "/tmp/report.html"
        };

        Assert.Equal("Analysis", report.Type);
        Assert.Equal("HTML", report.Format);
        Assert.Equal("<html>Test</html>", report.Content);
        Assert.Equal("/tmp/report.html", report.StorageLocation);
    }
}
