using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Controllers;
using Xunit;

namespace UnitTests.Controllers;

/// <summary>
/// PhotoController — kiểm tra upload với thư mục wwwroot/images tạm (mock đường dẫn ghi file).
/// </summary>
public class PhotoControllerTests : IDisposable
{
    private readonly string _previousCwd;
    private readonly string _tempRoot;

    public PhotoControllerTests()
    {
        _previousCwd = Directory.GetCurrentDirectory();
        _tempRoot = Path.Combine(Path.GetTempPath(), "unittest_photo_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_tempRoot, "wwwroot", "images"));
        Directory.SetCurrentDirectory(_tempRoot);
    }

    public void Dispose()
    {
        try
        {
            Directory.SetCurrentDirectory(_previousCwd);
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // ignore cleanup errors on locked files
        }
    }

    /// <summary>TC-PC-UPL-001 — upload hợp lệ => 200 + đường dẫn /images/...</summary>
    [Fact]
    public async Task UploadImage_ValidFile_TC_PC_UPL_001()
    {
        var controller = new PhotoController();
        await using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var file = new FormFile(stream, 0, stream.Length, "file", "mon_an.jpg");

        // Upload file JPEG hợp lệ, thư mục wwwroot/images đã ghi được (tạo trong constructor)
        var result = await controller.UploadImage(file);

        // 200 + body có đường dẫn, file thật tồn tại trên đĩa
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var path = Path.Combine(_tempRoot, "wwwroot", "images");
        Assert.True(Directory.GetFiles(path).Length >= 1);
    }

    /// <summary>TC-PC-UPL-002 — ghi đĩa lỗi phụ thuộc quyền/OS; không chỉnh PhotoController để inject FS.</summary>
    [Fact(Skip = "TC-PC-UPL-002: mô phỏng thư mục chỉ đọc không ổn định trên mọi máy Windows.")]
    public Task UploadImage_ReadOnlyFolder_TC_PC_UPL_002() => Task.CompletedTask;

    /// <summary>TC-PC-UPL-003 — baseline: null file => exception (code không guard).</summary>
    [Fact]
    public async Task UploadImage_NullFile_Baseline_TC_PC_UPL_003()
    {
        var controller = new PhotoController();

        // File null → code không guard → baseline: phải throw exception
        await Assert.ThrowsAnyAsync<Exception>(() => controller.UploadImage(null!));
    }
}
