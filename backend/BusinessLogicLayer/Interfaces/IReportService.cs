using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;

namespace BusinessLogicLayer.Interfaces;

public interface IReportService
{
    Task<Report> AddReportAsync(ReportDto reportDto);
    Task<(bool Success, string Message)> UpdateReportStatusAsync(int id, int status);
    Task<IEnumerable<ReportListItemDto>> GetReportsByReviewIdAsync(int reviewId);
}