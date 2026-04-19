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

    [Fact]
    public async Task TC_ADD_002_AddAddressAsync_ShouldPersistOnlyRequiredCityField()
    {
        // Test Case ID: TC-ADD-002
        await BeginTransactionAsync();
        try
        {
            var service = new AddressService(new AddressRepository(DbContext));
            var created = await service.AddAddressAsync(new AddressDto { City = "Only City" });
            Assert.NotNull(created);
            Assert.Equal("Only City", created.City);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_ADD_004_AddAddressAsync_ShouldAcceptNullDistrictWithOtherFieldsProvided()
    {
        // Test Case ID: TC-ADD-004
        await BeginTransactionAsync();
        try
        {
            var service = new AddressService(new AddressRepository(DbContext));
            var created = await service.AddAddressAsync(new AddressDto { City = "HCM", District = null, Ward = "Ben Nghe", Detail = "123" });
            Assert.Null(created.District);
            Assert.Equal("Ben Nghe", created.Ward);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_ADD_005_AddAddressAsync_ShouldAcceptMissingCoordinates()
    {
        // Test Case ID: TC-ADD-005
        await BeginTransactionAsync();
        try
        {
            var service = new AddressService(new AddressRepository(DbContext));
            var created = await service.AddAddressAsync(new AddressDto { City = "Can Tho", District = "Ninh Kieu", Ward = "Tan An", Detail = "45" });
            Assert.Null(created.Lon);
            Assert.Null(created.Lat);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_ADD_006_AddAddressAsync_ShouldTrimNotAppliedAndPersistOriginalText()
    {
        // Test Case ID: TC-ADD-006
        await BeginTransactionAsync();
        try
        {
            var service = new AddressService(new AddressRepository(DbContext));
            var created = await service.AddAddressAsync(new AddressDto { City = "  Da Nang  " });
            Assert.Equal("  Da Nang  ", created.City);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_ADD_007_AddAddressAsync_ShouldAllowNegativeCoordinatesByCurrentLogic()
    {
        // Test Case ID: TC-ADD-007
        await BeginTransactionAsync();
        try
        {
            var service = new AddressService(new AddressRepository(DbContext));
            var created = await service.AddAddressAsync(new AddressDto { City = "Hue", Lon = -91, Lat = -181 });
            Assert.Equal(-91, created.Lon);
            Assert.Equal(-181, created.Lat);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_ADD_009_AddAddressAsync_ShouldAllowNullCityByCurrentRepositoryBehavior()
    {
        // Test Case ID: TC-ADD-009
        await BeginTransactionAsync();
        try
        {
            var service = new AddressService(new AddressRepository(DbContext));
            var ex = await Record.ExceptionAsync(() => service.AddAddressAsync(new AddressDto { City = null! }));
            Assert.Null(ex);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_ADD_010_AddAddressAsync_ShouldStoreUnicodeInput()
    {
        // Test Case ID: TC-ADD-010
        await BeginTransactionAsync();
        try
        {
            var service = new AddressService(new AddressRepository(DbContext));
            var created = await service.AddAddressAsync(new AddressDto { City = "Hà Nội", Detail = "Phố cổ" });
            Assert.Equal("Hà Nội", created.City);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_ADD_011_AddAddressAsync_ShouldHandleDuplicatePayloadInMultipleCalls()
    {
        // Test Case ID: TC-ADD-011
        await BeginTransactionAsync();
        try
        {
            var service = new AddressService(new AddressRepository(DbContext));
            var dto = new AddressDto { City = "Repeat City", District = "D1" };
            await service.AddAddressAsync(dto);
            await service.AddAddressAsync(dto);
            Assert.True(await DbContext.Addresses.CountAsync() >= 2);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_ADD_012_AddAddressAsync_ShouldAcceptScriptLikeDetailAsPlainText()
    {
        // Test Case ID: TC-ADD-012
        await BeginTransactionAsync();
        try
        {
            var service = new AddressService(new AddressRepository(DbContext));
            var payload = "<script>alert('x')</script>";
            var created = await service.AddAddressAsync(new AddressDto { City = "Sec", Detail = payload });
            Assert.Equal(payload, created.Detail);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_ADD_013_AddAddressAsync_ShouldPreserveWhitespaceOnlyCity()
    {
        // Test Case ID: TC-ADD-013
        await BeginTransactionAsync();
        try
        {
            var service = new AddressService(new AddressRepository(DbContext));
            var created = await service.AddAddressAsync(new AddressDto { City = "   " });
            Assert.Equal("   ", created.City);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_ADD_014_AddAddressAsync_ShouldAcceptEmptyDetail()
    {
        // Test Case ID: TC-ADD-014
        await BeginTransactionAsync();
        try
        {
            var service = new AddressService(new AddressRepository(DbContext));
            var created = await service.AddAddressAsync(new AddressDto { City = "HN", Detail = string.Empty });
            Assert.Equal(string.Empty, created.Detail);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_UPD_002_UpdateAddressAsync_ShouldUpdateAllProvidedFields()
    {
        // Test Case ID: TC-UPD-002
        await BeginTransactionAsync();
        try
        {
            var seed = new Address { City = "Old", District = "Old", Ward = "Old" };
            DbContext.Addresses.Add(seed);
            await DbContext.SaveChangesAsync();

            var service = new AddressService(new AddressRepository(DbContext));
            await service.UpdateAddressAsync(seed.Id, new AddressDto { City = "New", District = "D", Ward = "W", Detail = "Detail", Lon = 1, Lat = 2 });

            var updated = await DbContext.Addresses.FindAsync(seed.Id);
            Assert.Equal("New", updated!.City);
            Assert.Equal("Detail", updated.Detail);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_UPD_004_UpdateAddressAsync_ShouldAllowSettingNullableFieldsToNull()
    {
        // Test Case ID: TC-UPD-004
        await BeginTransactionAsync();
        try
        {
            var seed = new Address { City = "Old", District = "X", Ward = "Y", Detail = "Z", Lon = 10, Lat = 20 };
            DbContext.Addresses.Add(seed);
            await DbContext.SaveChangesAsync();

            var service = new AddressService(new AddressRepository(DbContext));
            await service.UpdateAddressAsync(seed.Id, new AddressDto { City = "Old", District = null, Ward = null, Detail = null, Lon = null, Lat = null });

            var updated = await DbContext.Addresses.FindAsync(seed.Id);
            Assert.Null(updated!.District);
            Assert.Null(updated.Ward);
            Assert.Null(updated.Detail);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_UPD_005_UpdateAddressAsync_ShouldNotCreateNewRecordWhenIdMissing()
    {
        // Test Case ID: TC-UPD-005
        await BeginTransactionAsync();
        try
        {
            var service = new AddressService(new AddressRepository(DbContext));
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateAddressAsync(123456, new AddressDto { City = "A" }));
            Assert.Equal(0, await DbContext.Addresses.CountAsync());
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_UPD_006_UpdateAddressAsync_ShouldPersistNegativeCoordinatesByCurrentLogic()
    {
        // Test Case ID: TC-UPD-006
        await BeginTransactionAsync();
        try
        {
            var seed = new Address { City = "A" };
            DbContext.Addresses.Add(seed);
            await DbContext.SaveChangesAsync();

            var service = new AddressService(new AddressRepository(DbContext));
            await service.UpdateAddressAsync(seed.Id, new AddressDto { City = "A", Lon = -1, Lat = -1 });
            var updated = await DbContext.Addresses.FindAsync(seed.Id);
            Assert.Equal(-1, updated!.Lon);
            Assert.Equal(-1, updated.Lat);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_UPD_007_UpdateAddressAsync_ShouldUpdateSingleFieldAndKeepOthers()
    {
        // Test Case ID: TC-UPD-007
        await BeginTransactionAsync();
        try
        {
            var seed = new Address { City = "A", District = "B", Ward = "C" };
            DbContext.Addresses.Add(seed);
            await DbContext.SaveChangesAsync();

            var service = new AddressService(new AddressRepository(DbContext));
            await service.UpdateAddressAsync(seed.Id, new AddressDto { City = "A2", District = "B", Ward = "C" });
            var updated = await DbContext.Addresses.FindAsync(seed.Id);
            Assert.Equal("A2", updated!.City);
            Assert.Equal("B", updated.District);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_UPD_008_UpdateAddressAsync_ShouldAcceptBoundaryCoordinates()
    {
        // Test Case ID: TC-UPD-008
        await BeginTransactionAsync();
        try
        {
            var seed = new Address { City = "A" };
            DbContext.Addresses.Add(seed);
            await DbContext.SaveChangesAsync();

            var service = new AddressService(new AddressRepository(DbContext));
            await service.UpdateAddressAsync(seed.Id, new AddressDto { City = "A", Lon = 180, Lat = 90 });
            var updated = await DbContext.Addresses.FindAsync(seed.Id);
            Assert.Equal(180, updated!.Lon);
            Assert.Equal(90, updated.Lat);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_UPD_009_UpdateAddressAsync_ShouldAllowEmptyCityByCurrentLogic()
    {
        // Test Case ID: TC-UPD-009
        await BeginTransactionAsync();
        try
        {
            var seed = new Address { City = "X" };
            DbContext.Addresses.Add(seed);
            await DbContext.SaveChangesAsync();
            var service = new AddressService(new AddressRepository(DbContext));
            await service.UpdateAddressAsync(seed.Id, new AddressDto { City = string.Empty });
            var updated = await DbContext.Addresses.FindAsync(seed.Id);
            Assert.Equal(string.Empty, updated!.City);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_UPD_010_UpdateAddressAsync_ShouldAllowWhitespaceCityByCurrentLogic()
    {
        // Test Case ID: TC-UPD-010
        await BeginTransactionAsync();
        try
        {
            var seed = new Address { City = "X" };
            DbContext.Addresses.Add(seed);
            await DbContext.SaveChangesAsync();
            var service = new AddressService(new AddressRepository(DbContext));
            await service.UpdateAddressAsync(seed.Id, new AddressDto { City = "   " });
            var updated = await DbContext.Addresses.FindAsync(seed.Id);
            Assert.Equal("   ", updated!.City);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_UPD_011_UpdateAddressAsync_ShouldAllowLongCityTextByCurrentLogic()
    {
        // Test Case ID: TC-UPD-011
        await BeginTransactionAsync();
        try
        {
            var seed = new Address { City = "X" };
            DbContext.Addresses.Add(seed);
            await DbContext.SaveChangesAsync();
            var longCity = new string('A', 1000);
            var service = new AddressService(new AddressRepository(DbContext));
            await service.UpdateAddressAsync(seed.Id, new AddressDto { City = longCity });
            var updated = await DbContext.Addresses.FindAsync(seed.Id);
            Assert.Equal(longCity, updated!.City);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_UPD_012_UpdateAddressAsync_ShouldPersistSpecialCharacters()
    {
        // Test Case ID: TC-UPD-012
        await BeginTransactionAsync();
        try
        {
            var seed = new Address { City = "X" };
            DbContext.Addresses.Add(seed);
            await DbContext.SaveChangesAsync();
            var payload = "<script>alert(1)</script>";
            var service = new AddressService(new AddressRepository(DbContext));
            await service.UpdateAddressAsync(seed.Id, new AddressDto { City = "X", Detail = payload });
            var updated = await DbContext.Addresses.FindAsync(seed.Id);
            Assert.Equal(payload, updated!.Detail);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }
}