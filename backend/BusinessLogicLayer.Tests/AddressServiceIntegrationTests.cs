using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BusinessLogicLayer.Tests;

public class AddressServiceIntegrationTests : TestDatabaseFixture
{
    [Fact]
    public async Task TC_ADD_001_AddAddressAsync_ShouldCreateAddressAndReturnCreatedEntity()
    {
        // Test Case ID: TC-ADD-001
        // Mục tiêu: Kiểm tra service tạo mới Address thành công khi DTO hợp lệ.
        await BeginTransactionAsync();

        try
        {
            var addressRepository = new AddressRepository(DbContext);
            var addressService = new AddressService(addressRepository);

            var inputAddressDto = new AddressDto
            {
                City = "Hanoi",
                District = "Ba Dinh",
                Ward = "Phuc Xa",
                Detail = "12 ABC Street",
                Lon = 105.8342,
                Lat = 21.0278
            };

            var createdAddress = await addressService.AddAddressAsync(inputAddressDto);

            Assert.NotNull(createdAddress);
            Assert.NotEqual(0, createdAddress.Id);
            Assert.Equal(inputAddressDto.City, createdAddress.City);
            Assert.Equal(inputAddressDto.District, createdAddress.District);
            Assert.Equal(inputAddressDto.Ward, createdAddress.Ward);
            Assert.Equal(inputAddressDto.Detail, createdAddress.Detail);
            Assert.Equal(inputAddressDto.Lon, createdAddress.Lon);
            Assert.Equal(inputAddressDto.Lat, createdAddress.Lat);

            var savedAddress = await DbContext.Addresses.FindAsync(createdAddress.Id);
            Assert.NotNull(savedAddress);
            Assert.Equal(inputAddressDto.City, savedAddress!.City);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_ADD_003_AddAddressAsync_ShouldAllowNullableFieldsToBeNull()
    {
        // Test Case ID: TC-ADD-003
        // Mục tiêu: Kiểm tra các trường nullable có thể lưu giá trị null trong DB thật.
        await BeginTransactionAsync();

        try
        {
            var addressRepository = new AddressRepository(DbContext);
            var addressService = new AddressService(addressRepository);

            var inputAddressDto = new AddressDto
            {
                City = "Hue",
                District = null,
                Ward = null,
                Detail = null,
                Lon = null,
                Lat = null
            };

            var createdAddress = await addressService.AddAddressAsync(inputAddressDto);

            Assert.NotNull(createdAddress);
            Assert.Equal("Hue", createdAddress.City);
            Assert.Null(createdAddress.District);
            Assert.Null(createdAddress.Ward);
            Assert.Null(createdAddress.Detail);
            Assert.Null(createdAddress.Lon);
            Assert.Null(createdAddress.Lat);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_ADD_008_AddAddressAsync_ShouldPersistBoundaryCoordinates90And180()
    {
        // Test Case ID: TC-ADD-008
        // Mục tiêu: Kiểm tra lưu thành công tọa độ biên lớn nhất trong DB thật.
        await BeginTransactionAsync();

        try
        {
            var addressRepository = new AddressRepository(DbContext);
            var addressService = new AddressService(addressRepository);

            var inputAddressDto = new AddressDto
            {
                City = "Danang",
                Lon = 180,
                Lat = 90
            };

            var createdAddress = await addressService.AddAddressAsync(inputAddressDto);

            Assert.NotNull(createdAddress);
            Assert.Equal(180, createdAddress.Lon);
            Assert.Equal(90, createdAddress.Lat);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_UPD_001_UpdateAddressAsync_ShouldUpdateExistingAddressSuccessfully()
    {
        // Test Case ID: TC-UPD-001
        // Mục tiêu: Kiểm tra cập nhật Address khi Id tồn tại và DTO hợp lệ.
        await BeginTransactionAsync();

        try
        {
            var seedAddress = new Address
            {
                City = "Old City",
                District = "Old District",
                Ward = "Old Ward",
                Detail = "Old Detail",
                Lon = 1.1,
                Lat = 2.2
            };

            DbContext.Addresses.Add(seedAddress);
            await DbContext.SaveChangesAsync();

            var addressRepository = new AddressRepository(DbContext);
            var addressService = new AddressService(addressRepository);

            var updateAddressDto = new AddressDto
            {
                City = "Hanoi",
                District = "Dong Da",
                Ward = "Lang Ha",
                Detail = "99 Tran Duy Hung",
                Lon = 105.8,
                Lat = 21.0
            };

            await addressService.UpdateAddressAsync(seedAddress.Id, updateAddressDto);

            var updatedAddress = await DbContext.Addresses.FindAsync(seedAddress.Id);
            Assert.NotNull(updatedAddress);
            Assert.Equal(updateAddressDto.City, updatedAddress!.City);
            Assert.Equal(updateAddressDto.District, updatedAddress.District);
            Assert.Equal(updateAddressDto.Ward, updatedAddress.Ward);
            Assert.Equal(updateAddressDto.Detail, updatedAddress.Detail);
            Assert.Equal(updateAddressDto.Lon, updatedAddress.Lon);
            Assert.Equal(updateAddressDto.Lat, updatedAddress.Lat);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_UPD_003_UpdateAddressAsync_ShouldThrowKeyNotFoundWhenIdNotExists()
    {
        // Test Case ID: TC-UPD-003
        // Mục tiêu: Kiểm tra service ném KeyNotFoundException khi không tìm thấy Id.
        await BeginTransactionAsync();

        try
        {
            var addressRepository = new AddressRepository(DbContext);
            var addressService = new AddressService(addressRepository);

            var updateAddressDto = new AddressDto
            {
                City = "Hanoi"
            };

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => addressService.UpdateAddressAsync(999999, updateAddressDto));

            Assert.Equal("Address not found", exception.Message);

            var addressCount = await DbContext.Addresses.CountAsync();
            Assert.Equal(0, addressCount);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }
}