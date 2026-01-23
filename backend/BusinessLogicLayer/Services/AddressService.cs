using BusinessLogicLayer.Interfaces;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;

namespace BusinessLogicLayer.Services;

public class AddressService : IAddressService
{
    private readonly AddressRepository _addressRepository;

    public AddressService(AddressRepository addressRepository)
    {
        _addressRepository = addressRepository;
    }

    public async Task<Address> AddAddressAsync(AddressDto addressDto)
    {
        var address = new Address
        {
            City = addressDto.City,
            District = addressDto.District,
            Ward = addressDto.Ward,
            Detail = addressDto.Detail,
            Lon = addressDto.Lon,
            Lat = addressDto.Lat
            
        };

        var createdAddress = await _addressRepository.AddAddressAsync(address);
        return createdAddress;
    }

    public async Task UpdateAddressAsync(int id, AddressDto addressDto)
    {
        var address = await _addressRepository.GetAddressByIdAsync(id);
        if (address == null)
            throw new KeyNotFoundException("Address not found");
        address.City = addressDto.City;
        address.District = addressDto.District;
        address.Ward = addressDto.Ward;
        address.Detail = addressDto.Detail;
        address.Lon = addressDto.Lon;
        address.Lat = addressDto.Lat;

        await _addressRepository.UpdateAddressAsync(address);
    }
}