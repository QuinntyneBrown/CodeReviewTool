// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

var builder = DistributedApplication.CreateBuilder(args);

var gitAnalysis = builder.AddProject<Projects.GitAnalysis_Api>("gitanalysis-api");
var realtimeNotification = builder.AddProject<Projects.RealtimeNotification_Api>("realtimenotification-api");
var repositoryService = builder.AddProject<Projects.RepositoryService_Api>("repositoryservice-api");
var reportingService = builder.AddProject<Projects.ReportingService_Api>("reportingservice-api");

var apiGateway = builder.AddProject<Projects.ApiGateway_ApiGateway>("apigateway")
    .WithReference(gitAnalysis)
    .WithReference(realtimeNotification)
    .WithReference(repositoryService)
    .WithReference(reportingService);

builder.AddNpmApp("code-review-tool", "../Ui")
    .WithReference(apiGateway)
    .WithHttpEndpoint(env: "PORT", port: 4200)
    .WithExternalHttpEndpoints();

builder.Build().Run();
