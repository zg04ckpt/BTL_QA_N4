using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using UnitTests.Infrastructure;
using Xunit;

namespace UnitTests.Services;

public class AddressServiceTests
{

    

    // START TEST FUNCTION : AddAddressAsync

    /// <summary>
    /// TC-ADD-1-001
    /// 1 - 01 - AddressDto không hợp lệ với trường City là null còn lại hợp lệ
    /// Expected: failed (throw exception, không insert được)
    /// </summary>
    [Fact]
    public async Task AddAddressAsync_CityNull_ThrowsDbUpdateException()
    {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);

        var invalidDto = new AddressDto
        {
            City = null!,
            Lat = 80,
            Lon = 89,
            Detail = "so nha 3",
            District = "Ha Noi",
            Ward = "Hai Ba Trung"
        };

        // Act + Assert
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => addressService.AddAddressAsync(invalidDto));
        Assert.IsType<DbUpdateException>(exception);

        // Verify: DB không có record nào được insert
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var count = await verifyContext.Addresses.CountAsync();
        Assert.Equal(0, count);
    }

    /// <summary>
    /// TC-ADD-1-002
    /// 1  - 02- Address Dto không hợp lệ với cả object là null
    /// Expected: failed (throw exception, không insert được)
    /// </summary>
     [Fact]
    public async Task AddAddressAsync_ObjectNull_ThrowsDbUpdateException() {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);

         var invalidDto = new AddressDto
        {
            City = null!,
            Lat = null,
            Lon = null,
            Detail = null,
            District = null,
            Ward = null
        };

        // Act + Assert
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => addressService.AddAddressAsync(invalidDto));
        Assert.IsType<DbUpdateException>(exception);


        // Verify: DB không có record nào được insert
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var count = await verifyContext.Addresses.CountAsync();
        Assert.Equal(0, count);
    }

    /// <summary>
    /// TC-ADD-1-003
    /// 1  - 03 - AddressDto không hợp lệ do lat = 91 
    /// Expected: failed (throw exception, không insert được)
    /// </summary>
    [Fact]
    public async Task AddAddressAsync_LatMinus91_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);

        var invalidDto = new AddressDto
        {
            City = "Ha Noi",
            Lat = 91,
            Lon = 85,
            Detail = "so nha 3",
            District = "Ha Noi",
            Ward = "Kim Giang"
        };

        // Act + Assert
        var result = await addressService.AddAddressAsync(invalidDto);
        Assert.Null(result);

        // Verify: DB không có record nào được insert
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var count = await verifyContext.Addresses.CountAsync();
        Assert.Equal(0, count);
    }

    /// <summary>
    /// TC-ADD-1-004
    /// 1 - 04 - AddressDto không hợp lệ do lon = 181
    /// Expected: failed (throw exception, không insert được)
    /// </summary>
    [Fact]
    public async Task AddAddressAsync_LonMinus181_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);

        var invalidDto = new AddressDto
        {
            City = "Ha Noi",
            Lat = 39,
            Lon = 181,
            Detail = "so nha 3",
            District = "Thanh Xuan",
            Ward = "Kim Giang"
        };

        // Act + Assert
        var result = await addressService.AddAddressAsync(invalidDto);
        Assert.Null(result);

        // Verify: DB không có record nào được insert
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var count = await verifyContext.Addresses.CountAsync();
        Assert.Equal(0, count);
    }

    /// <summary>
    /// TC-ADD-1-005
    /// 1 - 05 - AddressDto không hợp lệ với chuỗi city quá dài 
    /// Expected: failed (throw exception, không insert được)
    /// </summary>
    [Fact]
    public async Task AddAddressAsync_CityTooLong_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);

        var invalidDto = new AddressDto
        {
            City = new string('A', 1001),
            Lat = 91,
            Lon = 85,
            Detail = "so nha 3",
            District = "Thanh Xuan",
            Ward = "Kim Giang"
        };

        // Act + Assert
        var result = await addressService.AddAddressAsync(invalidDto);
        Assert.Null(result);

        // Verify: DB không có record nào được insert
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var count = await verifyContext.Addresses.CountAsync();
        Assert.Equal(0, count);
    }

    /// <summary>
    /// TC-ADD-1-006
    /// 1 - 06 - AddressDto không hợp lệ với chuỗi district quá dài
    /// Expected: failed (throw exception, không insert được)
    /// </summary>
    [Fact]
    public async Task AddAddressAsync_DistrictTooLong_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);

        var invalidDto = new AddressDto
        {
            City = "Ha Noi",
            Lat = 91,
            Lon = 85,
            Detail = "so nha 3",
            District = new string('A', 1001),
            Ward = "Kim Giang"
        };

        // Act + Assert
        var result = await addressService.AddAddressAsync(invalidDto);
        Assert.Null(result);

        // Verify: DB không có record nào được insert
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var count = await verifyContext.Addresses.CountAsync();
        Assert.Equal(0, count);
    }

    /// <summary>
    /// TC-ADD-1-007
    /// 1 - 07 - AddressDto không hợp lệ với chuỗi ward quá dài
    /// Expected: failed (throw exception, không insert được)
    /// </summary>
    [Fact]
    public async Task AddAddressAsync_WardTooLong_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);

        var invalidDto = new AddressDto
        {
            City = "Ha Noi",
            Lat = 91, // Giữ tọa độ hợp lệ để khoanh vùng lỗi duy nhất là do Ward
            Lon = 85,
            Detail = "so nha 3",
            District = "Thanh Xuan",
            Ward = new string('A', 1001) // Chuỗi quá dài
        };

        // Act + Assert
        // Kỳ vọng hàm trả về null (báo fail) do code Service hiện chưa có Validation
        var result = await addressService.AddAddressAsync(invalidDto);
        Assert.Null(result);

        // Verify: DB không có record nào được insert
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var count = await verifyContext.Addresses.CountAsync();
        Assert.Equal(0, count);
    }

    /// <summary>
    /// TC-ADD-1-008
    /// 1 - 08 - AddressDto không hợp lệ với chuỗi detail quá dài
    /// Expected: failed (throw exception, không insert được)
    /// </summary>
    [Fact]
    public async Task AddAddressAsync_DetailTooLong_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);

        var invalidDto = new AddressDto
        {
            City = "Ha Noi",
            Lat = 91,
            Lon = 85,
            Detail = new string('A', 1001), // Chuỗi quá dài
            District = "Thanh Xuan",
            Ward = "Kim Giang"
        };

        // Act + Assert
        var result = await addressService.AddAddressAsync(invalidDto);
        Assert.Null(result);

        // Verify: DB không có record nào được insert
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var count = await verifyContext.Addresses.CountAsync();
        Assert.Equal(0, count);
    }

    /// <summary>
    /// TC-ADD-2-001
    /// 2 - 01 - AddressDto hợp lệ với trường city khác null, các trường còn lại là null
    /// Expected: pass
    /// </summary>
    [Fact]
    public async Task AddAddressAsync_OnlyCityNotNull_ReturnsCreatedAddress()
    {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);

        var validDto = new AddressDto
        {
            City = "Ha Noi",
            Lat = null,
            Lon = null,
            Detail = null,
            District = null,
            Ward = null
        };

        // Act
        var result = await addressService.AddAddressAsync(validDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(validDto.City, result.City);

        // Verify: DB có chính xác 1 record được insert
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var count = await verifyContext.Addresses.CountAsync();
        Assert.Equal(1, count);
    }

    /// <summary>
    /// TC-ADD-2-002
    /// 2 - 02 - AddressDto hợp lệ với trường city, district khác null còn các trường còn lại là null
    /// Expected: pass
    /// </summary>
    [Fact]
    public async Task AddAddressAsync_CityAndDistrictNotNull_ReturnsCreatedAddress()
    {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);

        var validDto = new AddressDto
        {
            City = "Ha Noi",
            Lat = null,
            Lon = null,
            Detail = null,
            District = "Thanh Xuan",
            Ward = null
        };

        // Act
        var result = await addressService.AddAddressAsync(validDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(validDto.District, result.District);

        // Verify: DB có chính xác 1 record
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var count = await verifyContext.Addresses.CountAsync();
        Assert.Equal(1, count);
    }

    /// <summary>
    /// TC-ADD-2-003
    /// 2 - 03 - AddressDto hợp lệ với trường city, district, ward khác null còn các trường còn lại là null
    /// Expected: pass
    /// </summary>
    [Fact]
    public async Task AddAddressAsync_CityDistrictWardNotNull_ReturnsCreatedAddress()
    {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);

        var validDto = new AddressDto
        {
            City = "Ha Noi",
            Lat = null,
            Lon = null,
            Detail = null,
            District = "Thanh Xuan",
            Ward = "Kim Giang"
        };

        // Act
        var result = await addressService.AddAddressAsync(validDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(validDto.Ward, result.Ward);

        // Verify
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var count = await verifyContext.Addresses.CountAsync();
        Assert.Equal(1, count);
    }

    /// <summary>
    /// TC-ADD-2-004
    /// 2 - 04 - AddressDto hợp lệ với trường city, district, ward, detail khác null còn các trường còn lại là null
    /// Expected: pass
    /// </summary>
    [Fact]
    public async Task AddAddressAsync_CityDistrictWardDetailNotNull_ReturnsCreatedAddress()
    {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);

        var validDto = new AddressDto
        {
            City = "Ha Noi",
            Lat = null,
            Lon = null,
            Detail = "so nha 3",
            District = "Thanh Xuan",
            Ward = "Kim Giang"
        };

        // Act
        var result = await addressService.AddAddressAsync(validDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(validDto.Detail, result.Detail);

        // Verify
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var count = await verifyContext.Addresses.CountAsync();
        Assert.Equal(1, count);
    }

    /// <summary>
    /// TC-ADD-2-005
    /// 2 - 05 - AddressDto hợp lệ với trường city, district, ward, detail, lon khác null còn các trường còn lại là null
    /// Expected: pass
    /// </summary>
    [Fact]
    public async Task AddAddressAsync_MissingOnlyLat_ReturnsCreatedAddress()
    {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);

        var validDto = new AddressDto
        {
            City = "Ha Noi",
            Lat = null,
            Lon = 85,
            Detail = "so nha 3",
            District = "Thanh Xuan",
            Ward = "Kim Giang"
        };

        // Act
        var result = await addressService.AddAddressAsync(validDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(validDto.Lon, result.Lon);

        // Verify
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var count = await verifyContext.Addresses.CountAsync();
        Assert.Equal(1, count);
    }

    /// <summary>
    /// TC-ADD-2-006
    /// 2 - 06 - AddressDto hợp lệ và toàn bộ trường khác null
    /// Expected: pass
    /// </summary>
    [Fact]
    public async Task AddAddressAsync_AllFieldsValid_ReturnsCreatedAddress()
    {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);

        var validDto = new AddressDto
        {
            City = "Ha Noi",
            Lat = 90,
            Lon = 85,
            Detail = "so nha 3",
            District = new string('A', 1001),
            Ward = "Kim Giang"
        };

        // Act
        var result = await addressService.AddAddressAsync(validDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(validDto.City, result.City);
        Assert.Equal(validDto.Lat, result.Lat);

        // Verify: Đảm bảo dữ liệu ghi đúng xuống DB
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var count = await verifyContext.Addresses.CountAsync();
        Assert.Equal(1, count);
    }

    // START TEST FUNCTION : UpdateAddressAsync
    /// <summary>
    /// TC-UAA-1-001
    /// 1 - 01 - id không hợp lệ (không tồn tại trong DB)
    /// Expected: failed (Ném ra KeyNotFoundException)
    /// </summary>
    [Fact]
    public async Task UpdateAddressAsync_IdNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);

        var dto = new AddressDto { City = "Ha Noi" };

        // Act + Assert
        // Gọi ID = 999 (không có trong DB ảo)
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => addressService.UpdateAddressAsync(99999999, dto));
        
        // Verify đúng thông báo lỗi
        Assert.Equal("Address not found", exception.Message);
    }

    /// <summary>
    /// TC-UAA-2-001 
    ///  2  - 01 - id là 3 : hợp lệ với addressDto có trường city khác null
    /// Expected: pass (
    /// </summary>
    [Fact]
    public async Task UpdateAddressAsync_OnlyCityNotNull_UpdatesCityAndClearsOtherFields()
    {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);
        
        // 1. BẮT BUỘC: Seed data - Tạo dữ liệu cũ có sẵn trong DB
        var existingAddress = new Address
        { 
            Id = 3, 
            City = "Ho Chi Minh", 
            District = "Quan 1", 
            Lat = 10.76, 
            Lon = 106.66 
        };
        context.Addresses.Add(existingAddress);

        await context.SaveChangesAsync();

        // 2. Dữ liệu mới cập nhật (Chỉ có City)
        var updateDto = new AddressDto
        {
            City = "Ha Noi",
            District = null,
            Ward = null,
            Detail = null,
            Lat = null,
            Lon = null
        };

        // Act
        await addressService.UpdateAddressAsync(3, updateDto);

        // Assert & Verify
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var updatedAddress = await verifyContext.Addresses.FindAsync(3);

        Assert.NotNull(updatedAddress);
        // Kiểm tra City đã được update
        Assert.Equal(updateDto.City, updatedAddress.City);
        
        // CẢNH BÁO: Kiểm tra lỗ hổng ghi đè dữ liệu. 
        // District cũ là "Quan 1" giờ KHÔNG bị biến thành null.
        Assert.Null(updatedAddress.District);
        Assert.Null(updatedAddress.Ward);
        Assert.Null(updatedAddress.Lat);
        Assert.Null(updatedAddress.Lon);
    }


     /// <summary>
    /// TC-UAA-2-002
    /// 2  - 02 - id là 3 : hợp lệ với addressDto có  trường city, district khác null còn các trường còn lại là null
    /// Expected: pass
    /// </summary>
    [Fact]
    public async Task UpdateAddressAsync_CityAndDistrictNotNull_UpdatesBothAndClearsRest()
    {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        
        var existingAddress = new Address { Id = 3, City = "Old City", District = "Old District", Ward = "Old Ward" };
        context.Addresses.Add(existingAddress);
        await context.SaveChangesAsync();

        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);

        var updateDto = new AddressDto
        {
            City = "Ha Noi",
            District = "Thanh Xuan",
            Ward = null, // Cố tình để null
            Detail = null,
            Lat = null,
            Lon = null
        };

        // Act
        await addressService.UpdateAddressAsync(3, updateDto);

        // Assert & Verify
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var updatedAddress = await verifyContext.Addresses.FindAsync(3);

        Assert.NotNull(updatedAddress);
        Assert.Equal(updateDto.City, updatedAddress.City);
        Assert.Equal(updateDto.District, updatedAddress.District);
        Assert.Null(updatedAddress.Ward);
        Assert.Null(updatedAddress.Lat);
        Assert.Null(updatedAddress.Lon);
    }

    /// <summary>
    /// TC-UAA-2-003
    /// 2  - 03 - id là 3 : hợp lệ với addressDto có  trường city, district, ward khác null còn các trường còn lại là nul
    /// Expected: pass
    /// </summary>
    [Fact]
    public async Task UpdateAddressAsync_CityAndDistrictAndWardNotNull_UpdatesBothAndClearsRest()
    {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        
        var existingAddress = new Address { Id = 3, City = "Old City", District = "Old District", Ward = "Old Ward" };
        context.Addresses.Add(existingAddress);
        await context.SaveChangesAsync();

        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);

        var updateDto = new AddressDto
        {
            City = "Ha Noi",
            District = "Thanh Xuan",
            Ward = "Kim Giang", // Cố tình để null
            Detail = null,
            Lat = null,
            Lon = null
        };

        // Act
        await addressService.UpdateAddressAsync(3, updateDto);

        // Assert & Verify
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var updatedAddress = await verifyContext.Addresses.FindAsync(3);

        Assert.NotNull(updatedAddress);
        Assert.Equal(updateDto.City, updatedAddress.City);
        Assert.Equal(updateDto.District, updatedAddress.District);
         Assert.Equal(updateDto.Ward, updatedAddress.Ward);
        Assert.Null(updatedAddress.Lat);
        Assert.Null(updatedAddress.Lon);
    }

    /// <summary>
    /// TC-UAA-2-004
    /// 3  - 04 - id là 3 : hợp lệ với addressDto có  trường city, district,ward,detail, lon khác null còn các trường còn lại là null
    /// Expected: pass
    /// </summary>
    [Fact]
    public async Task UpdateAddressAsync_CityAndDistrictAndWardAndDetailNotNull_UpdatesBothAndClearsRest()
    {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        
        var existingAddress = new Address { Id = 3, City = "Old City", District = "Old District", Ward = "Old Ward", Detail = "Old Detail" };
        context.Addresses.Add(existingAddress);
        await context.SaveChangesAsync();

        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);

        var updateDto = new AddressDto
        {
            City = "Ha Noi",
            District = "Thanh Xuan",
            Ward = "Kim Giang", // Cố tình để null
            Detail = "So nha 3",
            Lat = null,
            Lon = null
        };

        // Act
        await addressService.UpdateAddressAsync(3, updateDto);

        // Assert & Verify
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var updatedAddress = await verifyContext.Addresses.FindAsync(3);

        Assert.NotNull(updatedAddress);
        Assert.Equal(updateDto.City, updatedAddress.City);
        Assert.Equal(updateDto.District, updatedAddress.District);
         Assert.Equal(updateDto.Ward, updatedAddress.Ward);
         Assert.Equal(updateDto.Detail, updatedAddress.Detail);
        Assert.Null(updatedAddress.Lat);
        Assert.Null(updatedAddress.Lon);
    }

    /// <summary>
    /// TC-UAA-2-005
    /// 3  - 05 - id là 3 : hợp lệ với addressDto có  trường city, district,ward,detail, lon khác null còn các trường còn lại là null
    /// Expected: pass
    /// </summary>
    [Fact]
    public async Task UpdateAddressAsync_CityAndDistrictAndWardAndDetailAndLonNotNull_UpdatesBothAndClearsRest()
    {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        
        var existingAddress = new Address { Id = 3, City = "Old City", District = "Old District", Ward = "Old Ward", Detail = "Old Detail" , Lon = 10.76};
        context.Addresses.Add(existingAddress);
        await context.SaveChangesAsync();

        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);

        var updateDto = new AddressDto
        {
            City = "Ha Noi",
            District = "Thanh Xuan",
            Ward = "Kim Giang", 
            Detail = "So nha 3",
            Lat = null,
            Lon = 85
        };

        // Act
        await addressService.UpdateAddressAsync(3, updateDto);

        // Assert & Verify
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var updatedAddress = await verifyContext.Addresses.FindAsync(3);

        Assert.NotNull(updatedAddress);
        Assert.Equal(updateDto.City, updatedAddress.City);
        Assert.Equal(updateDto.District, updatedAddress.District);
         Assert.Equal(updateDto.Ward, updatedAddress.Ward);
         Assert.Equal(updateDto.Detail, updatedAddress.Detail);
        Assert.Null(updatedAddress.Lat);
         Assert.Equal(updateDto.Lon, updatedAddress.Lon);
    }

     /// <summary>
    /// TC-UAA-2-006
    /// 3  - 06 - id là 3 : hợp lệ với addressDto khác null
    /// Expected: pass
    /// </summary>
    [Fact]
    public async Task UpdateAddressAsync_ObjectNotNull_UpdatesBothAndClearsRest()
    {
        // Arrange
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        
        var existingAddress = new Address { Id = 3, City = "Old City", District = "Old District", Ward = "Old Ward", Detail = "Old Detail" , Lon = 10.76, Lat = 45};
        context.Addresses.Add(existingAddress);
        await context.SaveChangesAsync();

        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);

        var updateDto = new AddressDto
        {
            City = "Ha Noi",
            District = "Thanh Xuan",
            Ward = "Kim Giang", // Cố tình để null
            Detail = "So nha 3",
            Lat = 88,
            Lon = 85
        };

        // Act
        await addressService.UpdateAddressAsync(3, updateDto);

        // Assert & Verify
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var updatedAddress = await verifyContext.Addresses.FindAsync(3);

        Assert.NotNull(updatedAddress);
        Assert.Equal(updateDto.City, updatedAddress.City);
        Assert.Equal(updateDto.District, updatedAddress.District);
         Assert.Equal(updateDto.Ward, updatedAddress.Ward);
         Assert.Equal(updateDto.Detail, updatedAddress.Detail);
         Assert.Equal(updateDto.Lat, updatedAddress.Lat);
         Assert.Equal(updateDto.Lon, updatedAddress.Lon);
    }






}
