# Hướng dẫn nhanh Unit Tests

Tài liệu này giúp thành viên mới pull code và bắt đầu viết/chạy test ngay.

## 1) Yêu cầu môi trường

- .NET SDK 8.x
- Kiểm tra:

```powershell
dotnet --version
```

Nếu không phải 8.x, hãy cài .NET 8 SDK trước khi chạy test.

## 2) Restore packages

Chạy ở thư mục gốc repo:

```powershell
dotnet restore
```

## 3) Chạy test hiện có

### Chạy riêng project UnitTests

```powershell
dotnet test backend/UnitTests/UnitTests.csproj
```

### Chạy toàn bộ solution

```powershell
dotnet test backend/DoAn_Backend.sln
```

## 4) Cấu trúc và vị trí quan trọng

- Project test: `backend/UnitTests`
- Helper SQLite memory DB: `backend/UnitTests/Infrastructure/SqliteMemoryDb.cs`
- Mẫu test service:
  - `backend/UnitTests/Services/UserServiceTests.cs`

## 5) Quy ước khi viết test

- Đặt tên hàm theo mẫu: `<MethodName>_<ScenarioName>`
  - Ví dụ: `LoginAsync_LoginSuccess`
- Mỗi hàm test cần có comment ID testcase
  - Ví dụ: `TC-USR-003`
- Dữ liệu test ưu tiên dùng tiếng Việt rõ nghĩa
- Viết comment ngắn gọn theo từng bước Arrange/Act/Assert

## 6) Lưu ý về database test

- Đang dùng SQLite in-memory để test nhanh và có transaction/rollback thật.
- Khi cần rollback:
  - Mở transaction trong test
  - Bọc logic test trong `try`
  - `RollbackAsync()` trong `finally`

## 7) Chạy một test cụ thể

```powershell
dotnet test backend/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~UserServiceTests.LoginAsync_LoginSuccess"
```

