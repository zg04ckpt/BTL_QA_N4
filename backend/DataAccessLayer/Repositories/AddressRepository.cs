using DataAccessLayer.Context;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace DataAccessLayer.Repositories;

public class AddressRepository
{
    private readonly ApplicationDbContext _context;

    public AddressRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Address> AddAddressAsync(Address address)
    {
        await _context.Addresses.AddAsync(address); 
        await _context.SaveChangesAsync();
        return address; 
    }

    public async Task UpdateAddressAsync(Address address)
    {
        _context.Addresses.Update(address);
        await _context.SaveChangesAsync();
    }

    public async Task<Address?> GetAddressByIdAsync(int id)
    {
        return await _context.Addresses.FindAsync(id);
    }
}