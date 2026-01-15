// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CodeReviewTool.Shared.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReportingService.Core.Interfaces;
using ReportingService.Core.Messages;

namespace ReportingService.Infrastructure.BackgroundServices;

public class ReportGenerationService : BackgroundService
{
    private readonly IMessageSubscriber _messageSubscriber;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IReportGenerator _reportGenerator;
    private readonly IReportRepository _reportRepository;
    private readonly ILogger<ReportGenerationService> _logger;

    public ReportGenerationService(
        IMessageSubscriber messageSubscriber,
        IMessagePublisher messagePublisher,
        IReportGenerator reportGenerator,
        IReportRepository reportRepository,
        ILogger<ReportGenerationService> logger)
    {
        _messageSubscriber = messageSubscriber;
        _messagePublisher = messagePublisher;
        _reportGenerator = reportGenerator;
        _reportRepository = reportRepository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Report Generation Service started");

        await _messageSubscriber.SubscribeAsync<AnalysisCompletedMessage>(HandleAnalysisCompletedAsync, stoppingToken);
        await _messageSubscriber.SubscribeAsync<AnalyticsReportRequestedMessage>(HandleAnalyticsReportRequestedAsync, stoppingToken);
        await _messageSubscriber.SubscribeAsync<PolicyEvaluatedMessage>(HandlePolicyEvaluatedAsync, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task HandleAnalysisCompletedAsync(AnalysisCompletedMessage message)
    {
        try
        {
            _logger.LogInformation("Processing analysis completed message: {AnalysisId}", message.AnalysisId);

            var report = await _reportGenerator.GenerateHtmlReportAsync(message.AnalysisData);
            await _reportRepository.SaveAsync(report);

            var successMessage = new ReportGeneratedMessage
            {
                ReportId = report.Id,
                ReportType = report.Type,
                Format = report.Format,
                StorageLocation = report.StorageLocation
            };

            await _messagePublisher.PublishAsync(successMessage);
            _logger.LogInformation("Report generated successfully: {ReportId}", report.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate report for analysis: {AnalysisId}", message.AnalysisId);

            var failureMessage = new ReportGenerationFailedMessage
            {
                ReportId = Guid.NewGuid().ToString(),
                ReportType = "Analysis",
                ErrorMessage = ex.Message
            };

            await _messagePublisher.PublishAsync(failureMessage);
        }
    }

    private async Task HandleAnalyticsReportRequestedAsync(AnalyticsReportRequestedMessage message)
    {
        try
        {
            _logger.LogInformation("Processing analytics report request: {RequestId}", message.RequestId);

            var data = new
            {
                message.RequestId,
                message.StartDate,
                message.EndDate
            };

            var report = message.Format.ToLower() switch
            {
                "csv" => await _reportGenerator.GenerateCsvReportAsync(data),
                "pdf" => await _reportGenerator.GeneratePdfReportAsync(data),
                _ => await _reportGenerator.GenerateHtmlReportAsync(data)
            };

            await _reportRepository.SaveAsync(report);

            var successMessage = new ReportGeneratedMessage
            {
                ReportId = report.Id,
                ReportType = "Analytics",
                Format = report.Format,
                StorageLocation = report.StorageLocation
            };

            await _messagePublisher.PublishAsync(successMessage);
            _logger.LogInformation("Analytics report generated successfully: {ReportId}", report.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate analytics report: {RequestId}", message.RequestId);

            var failureMessage = new ReportGenerationFailedMessage
            {
                ReportId = Guid.NewGuid().ToString(),
                ReportType = "Analytics",
                ErrorMessage = ex.Message
            };

            await _messagePublisher.PublishAsync(failureMessage);
        }
    }

    private async Task HandlePolicyEvaluatedAsync(PolicyEvaluatedMessage message)
    {
        try
        {
            _logger.LogInformation("Processing policy evaluation: {PolicyId}", message.PolicyId);

            var data = new
            {
                message.PolicyId,
                message.RepositoryId,
                message.Passed,
                message.Violations
            };

            var report = await _reportGenerator.GenerateHtmlReportAsync(data);
            report.Type = "Compliance";
            await _reportRepository.SaveAsync(report);

            var successMessage = new ReportGeneratedMessage
            {
                ReportId = report.Id,
                ReportType = "Compliance",
                Format = report.Format,
                StorageLocation = report.StorageLocation
            };

            await _messagePublisher.PublishAsync(successMessage);
            _logger.LogInformation("Compliance report generated successfully: {ReportId}", report.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate compliance report: {PolicyId}", message.PolicyId);

            var failureMessage = new ReportGenerationFailedMessage
            {
                ReportId = Guid.NewGuid().ToString(),
                ReportType = "Compliance",
                ErrorMessage = ex.Message
            };

            await _messagePublisher.PublishAsync(failureMessage);
        }
    }
}
