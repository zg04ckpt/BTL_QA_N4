using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;

namespace BusinessLogicLayer.Interfaces;

public interface IAddressService
{
    Task<Address> AddAddressAsync(AddressDto addressDto);
    Task UpdateAddressAsync(int id, AddressDto addressDto);
}