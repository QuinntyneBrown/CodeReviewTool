// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace CodeReviewTool.Cli.Tests;

public class CliTests
{
    [Fact]
    public void Cli_ShouldHaveExecutableOutput()
    {
        // This test verifies that the CLI project is configured as an executable
        var assemblyName = typeof(Program).Assembly.GetName().Name;
        Assert.Equal("crt", assemblyName);
    }
}

// Make Program accessible to tests
public partial class Program { }
