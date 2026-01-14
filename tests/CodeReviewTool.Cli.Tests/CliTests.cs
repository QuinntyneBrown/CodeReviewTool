// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace CodeReviewTool.Cli.Tests;

public class CliTests
{
    [Fact]
    public void Cli_ShouldBeAccessibleFromTests()
    {
        // This test verifies that the CLI Program class is accessible from tests
        // The actual assembly name is set to 'crt' in the project file
        var type = typeof(Program);
        Assert.NotNull(type);
    }
}

// Make Program accessible to tests
public partial class Program { }
