using BusinessLogicLayer.Interfaces;
using DataAccessLayer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AddressController : Controller
{
    private readonly IAddressService _addressService;

    public AddressController(IAddressService addressService)
    {
        _addressService = addressService;
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddAddress([FromBody] AddressDto addressDto)
    {
        await _addressService.AddAddressAsync(addressDto);
        return Ok("Address added successfully");
    }

    [HttpPut("update/{id}")]
    public async Task<IActionResult> UpdateAddress(int id, [FromBody] AddressDto addressDto)
    {
        await _addressService.UpdateAddressAsync(id, addressDto);
        return Ok("Address updated successfully");
    }
}