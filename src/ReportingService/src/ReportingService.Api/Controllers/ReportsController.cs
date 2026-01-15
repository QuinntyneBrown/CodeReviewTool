// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using ReportingService.Core.Interfaces;

namespace ReportingService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportRepository _reportRepository;
    private readonly IReportGenerator _reportGenerator;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportRepository reportRepository,
        IReportGenerator reportGenerator,
        ILogger<ReportsController> logger)
    {
        _reportRepository = reportRepository;
        _reportGenerator = reportGenerator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var reports = await _reportRepository.GetAllAsync();
            return Ok(reports);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all reports");
            return StatusCode(500, new { error = "Failed to retrieve reports" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var report = await _reportRepository.GetByIdAsync(id);
            if (report == null)
            {
                return NotFound(new { error = "Report not found" });
            }

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report {ReportId}", id);
            return StatusCode(500, new { error = "Failed to retrieve report" });
        }
    }

    [HttpGet("type/{type}")]
    public async Task<IActionResult> GetByType(string type)
    {
        try
        {
            var reports = await _reportRepository.GetByTypeAsync(type);
            return Ok(reports);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reports of type {Type}", type);
            return StatusCode(500, new { error = "Failed to retrieve reports" });
        }
    }

    [HttpPost("generate/{format}")]
    public async Task<IActionResult> GenerateReport(string format, [FromBody] object data)
    {
        try
        {
            var report = format.ToLower() switch
            {
                "html" => await _reportGenerator.GenerateHtmlReportAsync(data),
                "pdf" => await _reportGenerator.GeneratePdfReportAsync(data),
                "sarif" => await _reportGenerator.GenerateSarifReportAsync(data),
                "junit" => await _reportGenerator.GenerateJUnitXmlReportAsync(data),
                "csv" => await _reportGenerator.GenerateCsvReportAsync(data),
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };

            await _reportRepository.SaveAsync(report);
            _logger.LogInformation("Report generated: {ReportId} in format {Format}", report.Id, format);

            return CreatedAtAction(nameof(GetById), new { id = report.Id }, report);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report in format {Format}", format);
            return StatusCode(500, new { error = "Failed to generate report" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _reportRepository.DeleteAsync(id);
            _logger.LogInformation("Report deleted: {ReportId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting report {ReportId}", id);
            return StatusCode(500, new { error = "Failed to delete report" });
        }
    }
}
