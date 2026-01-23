using BusinessLogicLayer.Interfaces;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QRInformationController : Controller
{
    private readonly IQRInformationService _qrInformationService;

    public QRInformationController(IQRInformationService qrInformationService)
    {
        _qrInformationService = qrInformationService;
    }

    [HttpPost]
    public async Task<IActionResult> AddQRInformation([FromBody] QRInformationDto qrInformation)
    {
        await _qrInformationService.AddQRInformationAsync(qrInformation);
        return Ok("QR Information added successfully.");
    }

    [HttpGet]
    public async Task<IActionResult> GetQRInformation([FromQuery] int? userId, [FromQuery] int? restaurantId)
    {
        var qrInformations = await _qrInformationService.GetQRInformationAsync(userId, restaurantId);
        return Ok(qrInformations);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQRInformation(int id)
    {
        await _qrInformationService.DeleteQRInformationAsync(id);
        return Ok("QR Information deleted successfully.");
    }
}