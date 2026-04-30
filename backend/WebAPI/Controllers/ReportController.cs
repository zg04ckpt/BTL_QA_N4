using BusinessLogicLayer.Interfaces;
using DataAccessLayer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportController : Controller
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpPost]
    public async Task<IActionResult> AddReport([FromBody] ReportDto reportDto)
    {
        var report = await _reportService.AddReportAsync(reportDto);
        return Ok(report);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateReportStatus(int id, [FromBody] int status)
    {
        var (success, message) = await _reportService.UpdateReportStatusAsync(id, status);
        if (!success) return NotFound(new { success = false, message });

        return Ok(new { success = true, message });
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllReports()
    {
        var reports = await _reportService.GetAllReportsAsync();
        return Ok(reports);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetReportsByUserId(int userId)
    {
        var reports = await _reportService.GetReportsByUserIdAsync(userId);
        return Ok(reports);
    }
}