# Chi tiết testcase backend (theo `truong-hop-test-backend-tree.json` và báo cáo đồ án)

Tài liệu map **mỗi dòng** cây testcase sang một bản ghi. **Input** và **Expected Output** dùng cấu trúc phân cấp (mỗi ô bảng: xuống dòng bằng `<br>`, thụt bằng `&nbsp;&nbsp;`):

**Test Objective (Mô tả):** viết câu đủ nghĩa — thường dạng **Khi [điều kiện / dữ liệu đầu vào] => [hành vi hệ thống / kết quả]**, tránh ghi tắt kiểu "QR+ML ok" mà nên: *Tạo Review khi đủ điều kiện QR và ML cho phép => Lưu Review vào DB và gửi thông báo Firebase tới chủ nhà hàng.*

**Input**

- Mỗi dòng phải **tự mô tả đủ** dữ liệu đầu vào: luôn có **`- Cơ sở:`** (seed / mock / host) và **`- Test:`** (gọi API, method, DTO, HTTP, query). Tránh rút gọn kiểu chỉ nói "giống TC-…" mà không liệt kê lại field khi testcase cần dùng độc lập trong báo cáo; nếu tham chiếu TC khác, vẫn nên lặp lại giá trị mẫu chính (email, id, body JSON).
- `- Cơ sở:` — nhóm con `+ Tên nhóm:` (vd `+ Review:`, `+ QRInformation:`, `+ ML mock:`, `+ DB seed:`), trong nhóm dùng `+) …` cho từng dòng chi tiết (vd `+) UserId = 1,`).
- `- Test:` — `+) Gọi: …` và `+) dto / tham số — …` (liệt kê field như `UserId = 1, RestaurantId = 3, …`).

**Expected Output**

- Với **message / thông báo lỗi**: có thể mô tả **ý nghĩa** bằng tiếng Việt, không bắt buộc trích nguyên chuỗi tiếng Anh trong code (trừ khi cần assert chính xác trong test).
- `- Trả về:` — `+) success / message / Token…` (khớp kiểu C# hoặc JSON).
- `- DB:` — `+) …` (bảng `reviews`, `ReviewPhoto`, …).
- `- Hành động phụ / tích hợp:` — Firebase, ML, ghi file…
- Controller thêm `- HTTP:` và `- Trả về body:` khi cần.

---

## 1. `UserService.cs`

### 1.1 `RegisterUserAsync`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-USV-REG-001 | Khi đăng ký với email & họ tên hợp lệ, email chưa tồn tại => Tạo tài khoản khách hàng (customer), tạo địa chỉ liên kết, trả về thông báo đăng ký thành công. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Bảng `users` / `addresses`: không có email `tranvanhung@example.vn`.<br>&nbsp;&nbsp;+ Repository: thật hoặc in-memory SQLite.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await RegisterUserAsync(userDto)`.<br>&nbsp;&nbsp;+) `userDto` — Email = `tranvanhung@example.vn`, Password = `MatKhau123!`, Name = `Trần Văn Hùng`, PhoneNumber = `0912345678`, Role = `customer`.<br>&nbsp;&nbsp;+) `userDto.Address` — City = `Hà Nội`, District = `Cầu Giấy`, Ward = `Dịch Vọng`, Detail = `Ngõ 12, đường Xuân Thủy`, Lat/Lon = số hợp lệ nếu DTO yêu cầu. | - Trả về:<br>&nbsp;&nbsp;+) `Success` = true.<br>&nbsp;&nbsp;+) `Message`: thông báo đăng ký thành công.<br>- DB:<br>&nbsp;&nbsp;+) Thêm 1 `User`: Role = `customer`, Status = 1, Email/Name/Phone/Password khớp request.<br>&nbsp;&nbsp;+) Thêm 1 `Address`; User.AddressId trỏ tới address mới; địa chỉ khớp seed.<br>- Hành động phụ: không. |
| TC-USV-REG-002 | Khi request gửi Role = admin và email hợp lệ, chưa trùng => Hệ thống ghi nhận quyền quản trị (admin) và xác nhận đăng ký thành công. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Bảng `users` / `addresses`: không có email `quantri@hethong.vn`.<br>&nbsp;&nbsp;+ Repository: thật hoặc in-memory SQLite.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await RegisterUserAsync(userDto)`.<br>&nbsp;&nbsp;+) `userDto` — Email = `quantri@hethong.vn`, Password = `MatKhauAdmin2024!`, Name = `Lê Thị Quản Trị`, PhoneNumber = `0908111222`, Role = `admin`.<br>&nbsp;&nbsp;+) `userDto.Address` — City = `Đà Nẵng`, District = `Hải Châu`, Ward = `Thạch Thang`, Detail = `Số 20 đường Lê Duẩn`, Lat = 16.0544, Lon = 108.2022. | - Trả về:<br>&nbsp;&nbsp;+) `Success` = true.<br>&nbsp;&nbsp;+) `Message`: thông báo đăng ký thành công.<br>- DB:<br>&nbsp;&nbsp;+) `User.Role` = `"admin"`. |
| TC-USV-REG-003 | Khi email đã gắn với tài khoản khác => Từ chối đăng ký, trả về thông báo trùng email, không tạo thêm user. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User Id = 100, Email = `nguyenthianh@gmail.com`, Name = `Người Cũ`, PhoneNumber = `0909000111`, Role = `customer`, Status = 1.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await RegisterUserAsync(userDto)`.<br>&nbsp;&nbsp;+) `userDto` — Email = `nguyenthianh@gmail.com`, Password = `MatKhau123!`, Name = `Nguyễn Thị Ánh`, PhoneNumber = `0912333444`, Role = `customer`.<br>&nbsp;&nbsp;+) `userDto.Address` — City = `Hà Nội`, District = `Đống Đa`, Ward = `Khâm Thiên`, Detail = `Ngõ 8 Khâm Thiên`, Lat = 21.0189, Lon = 105.8366. | - Trả về:<br>&nbsp;&nbsp;+) `Success` = false.<br>&nbsp;&nbsp;+) `Message`: thông báo email đã tồn tại.<br>- DB:<br>&nbsp;&nbsp;+) Số user không tăng; user Id=100 không đổi Name thành `Nguyễn Thị Ánh`. |
| TC-USV-REG-004 | Khi lưu hồ sơ không được xác nhận (repository CreateAsync trả null) => Trả về thông báo không tạo được tài khoản. | - Cơ sở:<br>&nbsp;&nbsp;+ Mock:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `UserRepository.CreateAsync` trả `null`.<br>&nbsp;&nbsp;+ DB seed: không trùng email đăng ký (hoặc isolated DbContext).<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await RegisterUserAsync(userDto)`.<br>&nbsp;&nbsp;+) `userDto` — Email = `phamminhduc@yahoo.com`, Password = `MatKhau123!`, Name = `Phạm Minh Đức`, PhoneNumber = `0933555666`, Role = `customer`.<br>&nbsp;&nbsp;+) `userDto.Address` — City = `Hải Phòng`, District = `Ngô Quyền`, Ward = `Máy Chai`, Detail = `Số 12 Lạch Tray`, Lat = 20.8449, Lon = 106.6881. | - Trả về:<br>&nbsp;&nbsp;+) `Success` = false.<br>&nbsp;&nbsp;+) `Message`: thông báo không tạo được tài khoản. |

### 1.2 `LoginAsync`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-USV-LOGIN-001 | Khi người dùng nhập đúng email & mật khẩu, tài khoản đang hoạt động => Cấp mã xác thực JWT, trả về thông báo đăng nhập thành công, không thay đổi dữ liệu user. | - Cơ sở:<br>&nbsp;&nbsp;+ User (seed):<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Email = `nguoidung@test.vn`, Password = `matkhau123`, Role = `customer`, Status = 1.<br>&nbsp;&nbsp;+ IConfiguration (`Jwt`):<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Key đủ dài; Subject, Issuer, Audience có giá trị.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await LoginAsync(loginDto)`.<br>&nbsp;&nbsp;+) `loginDto` — Email = `nguoidung@test.vn`, Password = `matkhau123`. | - Trả về (`LoginResult`):<br>&nbsp;&nbsp;+) `Message`: thông báo đăng nhập thành công.<br>&nbsp;&nbsp;+) `Token`: chuỗi JWT không null/rỗng (có dạng xxx.yyy.zzz).<br>- DB:<br>&nbsp;&nbsp;+) Không thêm/xóa user; không đổi password.<br>- Hành động phụ:<br>&nbsp;&nbsp;+) Có thể decode JWT kiểm tra claim Id, email, role. |
| TC-USV-LOGIN-002 | Khi email không khớp hồ sơ nào hoặc mật khẩu sai => Không cấp token, trả về thông báo sai thông tin đăng nhập. | - Cơ sở:<br>&nbsp;&nbsp;+ User seed giống TC-USV-LOGIN-001.<br>- Test:<br>&nbsp;&nbsp;+) (A) Email đúng, Password = `saimatkhau`; hoặc (B) Email = `khongco@mail.com`, Password bất kỳ.<br>&nbsp;&nbsp;+) `await LoginAsync(loginDto)`. | - Trả về:<br>&nbsp;&nbsp;+) `Token` = null.<br>&nbsp;&nbsp;+) `Message`: thông báo sai email hoặc mật khẩu. |
| TC-USV-LOGIN-003 | Khi mật khẩu đúng nhưng tài khoản bị khóa (Status ≠ 1) => Không cấp token, trả về thông báo cần liên hệ quản trị. | - Cơ sở:<br>&nbsp;&nbsp;+ User: Email = `bikhoa@example.vn`, Password = `dungmatkhau`, Status = 0.<br>- Test:<br>&nbsp;&nbsp;+) `loginDto` đúng email/password; `await LoginAsync(loginDto)`. | - Trả về:<br>&nbsp;&nbsp;+) `Token` = null.<br>&nbsp;&nbsp;+) `Message`: thông báo tài khoản bị khóa, cần liên hệ quản trị. |

### 1.3 `GetAllUsersAsync`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-USV-GAU-001 | Khi hệ thống có nhiều người dùng trong DB => Trả về danh sách đầy đủ bản ghi User (dữ liệu thô) cho luồng nội bộ. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User A: Id = 201, Email = `phamvanan@mail.vn`, Name = `Phạm Văn An`, PhoneNumber = `0911112222`, Role = `customer`, Status = 1.<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User B: Id = 202, Email = `hoangthibich@mail.vn`, Name = `Hoàng Thị Bích`, PhoneNumber = `0922223333`, Role = `customer`, Status = 1.<br>&nbsp;&nbsp;+ Repository / DbContext: SQLite in-memory hoặc DB test có transaction rollback.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await GetAllUsersAsync()`.<br>&nbsp;&nbsp;+) Không tham số. | - Trả về:<br>&nbsp;&nbsp;+) `IEnumerable<User>` có Count ≥ 2.<br>&nbsp;&nbsp;+) Mỗi phần tử có Id, Email khớp seed.<br>- DB: không đổi (chỉ đọc). |
| TC-USV-GAU-002 | Khi chưa có người dùng nào trong DB => Trả về danh sách rỗng. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Bảng `users` không có dòng (truncate / DbContext mới / transaction chỉ chứa bảng rỗng trước assert).<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await GetAllUsersAsync()`.<br>&nbsp;&nbsp;+) Không tham số. | - Trả về:<br>&nbsp;&nbsp;+) Enumerable rỗng (Count = 0). |

### 1.4 `GetAllUserSummariesForAdminAsync`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-USV-GAS-001 | Khi quản trị xem danh sách và mỗi hồ sơ có đủ thông tin kèm địa chỉ => Mỗi dòng tóm tắt hiển thị đầy đủ email, họ tên, SĐT, vai trò, trạng thái, ảnh đại diện và địa chỉ chi tiết. | - Cơ sở:<br>&nbsp;&nbsp;+ User: Name = `Đỗ Minh Châu`, Email = `dominhchau@gmail.com`, PhoneNumber = `0933444555`, Role = `customer`, Status = 1, AvtImage = `/images/chau.png`.<br>&nbsp;&nbsp;+ Address liên kết: City = `Hà Nội`, District = `Ba Đình`, Ward = `Điện Biên`, Detail = `Số 5 phố Điện Biên Phủ`, Lat/Lon số thực.<br>- Test:<br>&nbsp;&nbsp;+) `await GetAllUserSummariesForAdminAsync()`. | - Trả về:<br>&nbsp;&nbsp;+) Một `AdminUserSummaryDto` khớp user seed.<br>&nbsp;&nbsp;+) `Address` ≠ null; City/District/Ward/Detail/Lat/Lon khớp seed.<br>- DB: chỉ đọc. |
| TC-USV-GAS-002 | Khi trong danh sách có người dùng không có địa chỉ => Phần địa chỉ trên bản tóm tắt để trống (null), không coi là lỗi hệ thống. | - Cơ sở:<br>&nbsp;&nbsp;+ User: Name = `Vũ Thị Lan`, Email = `vuthilan@mail.vn`, AddressId = null.<br>- Test:<br>&nbsp;&nbsp;+) `await GetAllUserSummariesForAdminAsync()`. | - Trả về:<br>&nbsp;&nbsp;+) Dòng user Lan có `Address` = null.<br>&nbsp;&nbsp;+) Không exception. |

### 1.5 `GetUserByIdAsync`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-USV-GUI-001 | Khi tra cứu người dùng đã tồn tại và có địa chỉ => Trả về DTO chi tiết kèm thông tin địa chỉ đầy đủ. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User Id = 1, Email = `user1@test.vn`, Name = `Nguyễn Văn Một`, PhoneNumber = `0909123456`, Role = `customer`, Status = 1, AddressId trỏ tới Address Id = 301.<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Address Id = 301: City = `Huế`, District = `Thành phố Huế`, Ward = `Phú Hậu`, Detail = `Số 88 phố Huế`, Lat = 16.4637, Lon = 107.5909.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await GetUserByIdAsync(1)`. | - Trả về:<br>&nbsp;&nbsp;+) `UserDTO` ≠ null.<br>&nbsp;&nbsp;+) `Address.Detail` = `Số 88 phố Huế` (và các field địa chỉ khớp). |
| TC-USV-GUI-002 | Khi tra cứu người dùng tồn tại nhưng chưa có địa chỉ => Trả về thông tin tài khoản; phần địa chỉ để trống (null). | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User Id = 2, Email = `khongdiachi@example.vn`, Name = `Trần Thị Hai`, PhoneNumber = `0912000333`, Role = `customer`, Status = 1, AddressId = null.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await GetUserByIdAsync(2)`. | - Trả về:<br>&nbsp;&nbsp;+) `UserDTO.Address` = null. |
| TC-USV-GUI-003 | Khi mã người dùng không khớp hồ sơ nào => Không trả về dữ liệu người dùng (null). | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Không tồn tại `users.Id = 9999` (max Id trong seed nhỏ hơn hoặc không trùng).<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await GetUserByIdAsync(9999)`. | - Trả về:<br>&nbsp;&nbsp;+) `null`. |

### 1.6 `UpdateUserAsync`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-USV-UU-001 | Khi quản trị gửi yêu cầu chỉ đổi trạng thái (khóa/mở) cho người dùng tồn tại => Cập nhật trường Status trong DB và xác nhận thành công. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User Id = 10, Email = `user10@test.vn`, Name = `Người Mười`, PhoneNumber = `0900101010`, Role = `customer`, Status = 1.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await UpdateUserAsync(10, dto)`.<br>&nbsp;&nbsp;+) `UserUpdateDTO` — Status = 0; Name, Email, PhoneNumber, AvtImage, Address = null. | - Trả về:<br>&nbsp;&nbsp;+) Success = true; Message: thông báo cập nhật user thành công.<br>- DB:<br>&nbsp;&nbsp;+) User.Status = 0. |
| TC-USV-UU-002 | Khi chỉ gửi họ tên mới (các trường khác để trống) => Đổi tên hiển thị; giữ nguyên email và SĐT. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User Id = 11, Email = `buivan@mail.vn`, Name = `Bùi Văn Cũ`, PhoneNumber = `0909111222`, Role = `customer`, Status = 1.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await UpdateUserAsync(11, dto)`.<br>&nbsp;&nbsp;+) `UserUpdateDTO` — Name = `Bùi Văn Mới`; Email, PhoneNumber, Status, AvtImage, Address = null. | - Trả về: Success = true.<br>- DB: Name đổi; Email, PhoneNumber giữ nguyên. |
| TC-USV-UU-003 | Khi chỉ gửi SĐT mới => Cập nhật số điện thoại; không động vào các trường khác nếu không gửi. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User Id = 12, Email = `user12@test.vn`, PhoneNumber = `0909000111`, Name = `Lê Văn Mười Hai`, Status = 1.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await UpdateUserAsync(12, dto)`.<br>&nbsp;&nbsp;+) `UserUpdateDTO` — PhoneNumber = `0987654321`; Name, Email, Status, AvtImage, Address = null. | - Trả về: Success = true.<br>- DB: chỉ SĐT đổi. |
| TC-USV-UU-004 | Khi chỉ gửi đường dẫn ảnh đại diện mới => Thay ảnh hiển thị; họ tên và liên hệ giữ nguyên. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User Id = 13, Email = `user13@test.vn`, Name = `Ảnh Cũ`, PhoneNumber = `0909131313`, AvtImage = `/images/old.png`, Status = 1.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await UpdateUserAsync(13, dto)`.<br>&nbsp;&nbsp;+) `UserUpdateDTO` — AvtImage = `/images/avatar_moi.png`; các field khác null. | - Trả về: Success = true.<br>- DB: chỉ AvtImage đổi. |
| TC-USV-UU-005 | Khi cập nhật địa chỉ chỉ gửi một phần trường (merge) => Chỉ ghép các phần được gửi; giữ nguyên phần địa chỉ chưa gửi. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User Id = 14, AddressId trỏ Address Id = 50: City = `Hà Nội`, District = `Hoàn Kiếm`, Ward = `Hàng Bài`, Detail = `Số 1 phố Hàng Bài`, Lon = 105.8500, Lat = 21.0280.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await UpdateUserAsync(14, dto)`.<br>&nbsp;&nbsp;+) `UserUpdateDTO.Address` — chỉ District = `Ba Đình`; City, Ward, Detail, Lon, Lat = null (merge theo service). | - Trả về: Success = true.<br>- DB: District = `Ba Đình`; Ward vẫn `Hàng Bài`. |
| TC-USV-UU-006 | Khi người dùng gửi tọa độ mới (kinh/vĩ độ) => Lưu lon/lat mới trên bản ghi địa chỉ. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User Id = 15 + Address: Lon = 105.80, Lat = 21.00 (ghi trong assert “trước cập nhật”).<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await UpdateUserAsync(15, dto)`.<br>&nbsp;&nbsp;+) `UserUpdateDTO` (hoặc `UserUpdateDTO.Address` tùy model project) — Lon = 105.85, Lat = 21.03; các field khác null nếu chỉ đổi tọa độ. | - Trả về: Success = true.<br>- DB: Lon/Lat khớp 105.85 / 21.03. |
| TC-USV-UU-007 | Khi hồ sơ chưa có địa chỉ nhưng request gửi đủ thông tin địa chỉ => Tạo mới một bản ghi địa chỉ và liên kết với user. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User Id = 16, Email = `user16@test.vn`, AddressId = null, Status = 1.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await UpdateUserAsync(16, dto)`.<br>&nbsp;&nbsp;+) `UserUpdateDTO.Address` — City = `TP.HCM`, District = `Quận 1`, Ward = `Bến Nghé`, Detail = `Số 45 Nguyễn Huệ`, Lon = 106.7044, Lat = 10.7769. | - Trả về: Success = true.<br>- DB: User.AddressId khác null; thêm 1 dòng `addresses`. |
| TC-USV-UU-008 | Khi cập nhật với mã người dùng không tồn tại => Từ chối với thông báo không tìm thấy user. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Không có User Id = 777.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await UpdateUserAsync(777, dto)`.<br>&nbsp;&nbsp;+) `UserUpdateDTO` — Name = `Tên Giả`, PhoneNumber = `0999888777` (tối thiểu hợp lệ). | - Trả về:<br>&nbsp;&nbsp;+) Success = false.<br>&nbsp;&nbsp;+) Message: thông báo không tìm thấy user. |
| TC-USV-UU-009 | Khi lớp lưu trữ không xác nhận được thao tác cập nhật (trả null) => Trả về thông báo cập nhật thất bại. | - Cơ sở:<br>&nbsp;&nbsp;+ Mock:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `UserRepository.UpdateAsync` trả `null`.<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User Id = 17, Email = `mockfail@test.vn`, Name = `Trước Mock`, Status = 1.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await UpdateUserAsync(17, dto)`.<br>&nbsp;&nbsp;+) `UserUpdateDTO` — Name = `Sau Mock`. | - Trả về:<br>&nbsp;&nbsp;+) Success = false.<br>&nbsp;&nbsp;+) Message: thông báo cập nhật thất bại. |

### 1.7 `DeleteUserAsync`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-USV-DU-001 | Khi xóa người dùng với mã tồn tại => Xóa bản ghi khỏi DB và xác nhận thành công. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User Id = 5, Email = `canxoa@test.vn`, Name = `Người Bị Xóa`, Role = `customer`, Status = 1.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await DeleteUserAsync(5)`. | - Trả về:<br>&nbsp;&nbsp;+) Success = true.<br>&nbsp;&nbsp;+) Message: thông báo xóa user thành công.<br>- DB:<br>&nbsp;&nbsp;+) User Id=5 không còn (xóa cứng theo repo hiện tại). |
| TC-USV-DU-002 | Khi yêu cầu xóa với mã không tồn tại => Không xóa dữ liệu; trả về không tìm thấy user. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Không có User Id = 888 trong bảng `users`.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await DeleteUserAsync(888)`. | - Trả về:<br>&nbsp;&nbsp;+) Success = false.<br>&nbsp;&nbsp;+) Message: thông báo không tìm thấy user. |
| TC-USV-DU-003 | Khi lớp lưu trữ báo xóa không thành công (false) => Trả về thông báo xóa thất bại. | - Cơ sở:<br>&nbsp;&nbsp;+ Mock:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `UserRepository.DeleteAsync` trả `false`.<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User Id = 3, Email = `deletefail@test.vn`, Name = `Xóa Thất Bại`, Status = 1.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await DeleteUserAsync(3)`. | - Trả về:<br>&nbsp;&nbsp;+) Success = false.<br>&nbsp;&nbsp;+) Message: thông báo xóa thất bại. |

---

## 2. `UserController.cs`

### 2.1 `Register`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-UC-REG-001 | Khi client gửi body đăng ký không hợp lệ (thiếu email hoặc sai định dạng) => API trả 400 và ModelState phản ánh lỗi từng trường. | - Cơ sở:<br>&nbsp;&nbsp;+ Host test:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `WebApplicationFactory` / test server với cùng pipeline validation như production.<br>&nbsp;&nbsp;+ Header:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `Content-Type: application/json`.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `POST /api/User/register`.<br>&nbsp;&nbsp;+) Body JSON (ví dụ): `{ "name": "Test", "password": "MatKhau123!" }` — **thiếu** key `email`; hoặc `"email": "khong-phai-email"`. | - HTTP:<br>&nbsp;&nbsp;+) Status = 400 BadRequest.<br>- Trả về body:<br>&nbsp;&nbsp;+) ModelState có lỗi theo field (vd Email). |
| TC-UC-REG-002 | Khi gửi đủ thông tin người dùng + địa chỉ hợp lệ và email chưa trùng => Đăng ký thành công qua HTTP 200 với thông báo xác nhận từ backend. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Chưa có user email `dangky@ok.vn`.<br>&nbsp;&nbsp;+ Header:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `Content-Type: application/json`.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `POST /api/User/register`.<br>&nbsp;&nbsp;+) Body JSON (khớp DTO API): Email = `dangky@ok.vn`, Password = `MatKhau123!`, Name = `Đăng Ký OK`, PhoneNumber = `0909123456`, Role = `customer`, và object Address đầy đủ City/District/Ward/Detail/Lat/Lon (giá trị mẫu giống TC-USV-REG-001). | - HTTP:<br>&nbsp;&nbsp;+) Status = 200.<br>- Trả về body (JSON):<br>&nbsp;&nbsp;+) Key `Message`: thông báo đăng ký thành công. |
| TC-UC-REG-003 | Khi đăng ký với email đã có trong hệ thống => API từ chối (400) và trả message trùng email. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User đã tồn tại với Email = `trung@mail.vn`.<br>&nbsp;&nbsp;+ Header:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `Content-Type: application/json`.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `POST /api/User/register`.<br>&nbsp;&nbsp;+) Body: Email = `trung@mail.vn`, Password = `MatKhau123!`, Name = `Người Mới`, PhoneNumber = `0911222333`, Role = `customer` + Address đầy đủ (khác Id nhưng trùng email). | - HTTP: 400.<br>- Body: `Message` — thông báo email đã tồn tại. |
| TC-UC-REG-004 | Khi đặc tả yêu cầu chặn trùng số điện thoại (gap so với code hiện tại chỉ check email) => Kỳ vọng 400 + message trùng SĐT; ghi nhận kết quả thực tế khi chạy test. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User A: Email = `user_a@mail.vn`, PhoneNumber = `0911222333`, Name = `Người Cũ SĐT`.<br>&nbsp;&nbsp;+ Header:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `Content-Type: application/json`.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `POST /api/User/register`.<br>&nbsp;&nbsp;+) Body: Email = `user_b@mail.vn` (khác A), PhoneNumber = `0911222333` (trùng A), Password/Name/Role/Address hợp lệ đầy đủ. | - Đặc tả kỳ vọng: HTTP 400 + message trùng SĐT.<br>- Ghi chú: code hiện chỉ check trùng email — khi chạy có thể 200. |

### 2.2 `Login`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-UC-LOGIN-001 | Khi client không gửi body đăng nhập (null) => API trả 400 với message dữ liệu đăng nhập không hợp lệ. | - Cơ sở:<br>&nbsp;&nbsp;+ Host test:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) API integration giống TC-UC-REG-001.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `POST /api/User/login`.<br>&nbsp;&nbsp;+) Body: `null` / không gửi JSON (theo cách client test mô phỏng); `Content-Type` có thể `application/json` hoặc không — ghi nhận baseline. | - HTTP: 400.<br>- Body: `Message` — thông báo dữ liệu đăng nhập không hợp lệ. |
| TC-UC-LOGIN-002 | Khi email và mật khẩu đúng và tài khoản đang hoạt động => HTTP 200 và body chứa JWT (theo controller hiện tại có thể không kèm Message). | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User: Email = `nguoidung@test.vn`, Password = `matkhau123` (hash đúng quy ước service), Role = `customer`, Status = 1.<br>&nbsp;&nbsp;+ Header:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `Content-Type: application/json`.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `POST /api/User/login`.<br>&nbsp;&nbsp;+) Body JSON: `{ "email": "nguoidung@test.vn", "password": "matkhau123" }` (khớp tên property DTO thực tế trong project). | - HTTP: 200.<br>- Trả về body:<br>&nbsp;&nbsp;+) Có key `Token` (JWT).<br>&nbsp;&nbsp;+) Không có key `Message` trong Ok (theo controller hiện tại). |
| TC-UC-LOGIN-003 | Khi mật khẩu không khớp (hoặc email không tồn tại) => HTTP 401 và message sai thông tin đăng nhập. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User giống TC-UC-LOGIN-002 (Email = `nguoidung@test.vn`).<br>&nbsp;&nbsp;+ Header:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `Content-Type: application/json`.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `POST /api/User/login`.<br>&nbsp;&nbsp;+) Body: Email = `nguoidung@test.vn`, Password = `saimatkhau`. | - HTTP: 401.<br>- Body: `Message` — thông báo sai email hoặc mật khẩu. |
| TC-UC-LOGIN-004 | Khi tài khoản bị khóa (Status khác hoạt động) dù password đúng => HTTP 401 và message yêu cầu liên hệ quản trị. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User: Email = `bikhoa@example.vn`, Password = `dungmatkhau` (đúng hash), Status = 0.<br>&nbsp;&nbsp;+ Header:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `Content-Type: application/json`.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `POST /api/User/login`.<br>&nbsp;&nbsp;+) Body: Email = `bikhoa@example.vn`, Password = `dungmatkhau`. | - HTTP: 401.<br>- Body: message thông báo tài khoản bị khóa (tương đương service). |

### 2.3 `GetAllUsers`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-UC-GAU-001 | Khi gọi API lấy danh sách người dùng cho quản trị => HTTP 200 và JSON mảng bản tóm tắt (có thể rỗng hoặc N phần tử). | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed (một trong hai kịch bản):<br>&nbsp;&nbsp;&nbsp;&nbsp;+) (A) Không user; hoặc (B) ≥1 user với đủ field tóm tắt (Email, Name, Phone…).<br>&nbsp;&nbsp;+ Auth (nếu endpoint yêu cầu):<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Header `Authorization: Bearer <JWT admin>` khi test protected.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `GET /api/User/GetAllUsers`.<br>&nbsp;&nbsp;+) Không body; query string rỗng (trừ khi route có tham số). | - HTTP: 200.<br>- Body: JSON array `AdminUserSummaryDto` (có thể `[]`). |

### 2.4 `GetUserById`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-UC-GUI-001 | Khi tra cứu mã người dùng tồn tại => HTTP 200 và body là UserDTO đầy đủ. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User Id = 42, Email = `user42@test.vn`, Name = `Người 42`, PhoneNumber = `0909424242`, Role = `customer`, Status = 1 + Address đầy đủ nếu FK yêu cầu.<br>&nbsp;&nbsp;+ Auth (nếu có):<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Bearer token khi bắt buộc.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `GET /api/User/GetUserById?id=42`. | - HTTP: 200.<br>- Body: JSON UserDTO đầy đủ. |
| TC-UC-GUI-002 | Khi tra cứu mã không tồn tại => HTTP 204 No Content, không body. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Không có User Id = 99999.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `GET /api/User/GetUserById?id=99999`. | - HTTP: 204 NoContent.<br>- Body: rỗng. |

### 2.5 `UpdateUser`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-UC-UU-001 | Khi body cập nhật không vượt qua validation => HTTP 400 với ModelState. | - Cơ sở:<br>&nbsp;&nbsp;+ Host test + model binding giống production.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `PUT /api/User/UpdateUser/{id}` với `id` số hợp lệ (vd 10).<br>&nbsp;&nbsp;+) Body JSON: thiếu field bắt buộc / sai kiểu / email không hợp lệ (theo attribute validation của `UserUpdateDTO`).<br>&nbsp;&nbsp;+) `Content-Type: application/json`. | - HTTP: 400 ModelState. |
| TC-UC-UU-002 | Khi cập nhật với id tồn tại và DTO hợp lệ => HTTP 200 và message cập nhật thành công. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User Id = 20 tồn tại, có thể kèm Address.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `PUT /api/User/UpdateUser/20`.<br>&nbsp;&nbsp;+) Body JSON `UserUpdateDTO` hợp lệ (vd Name = `Tên Sau Cập Nhật`, các field khác null hoặc giá trị hợp lệ).<br>&nbsp;&nbsp;+) `Content-Type: application/json`. | - HTTP: 200.<br>- Body: `Message` — thông báo cập nhật user thành công. |
| TC-UC-UU-003 | Khi id không tồn tại hoặc lớp service báo cập nhật thất bại => HTTP 400 và message not found / failed to update. | - Cơ sở:<br>&nbsp;&nbsp;+ (A) DB không có User Id = 777; hoặc (B) mock service `UpdateUserAsync` trả failed.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `PUT /api/User/UpdateUser/777` (hoặc id mock fail).<br>&nbsp;&nbsp;+) Body JSON `UserUpdateDTO` hợp lệ giống TC-UC-UU-002. | - HTTP: 400.<br>- Body: thông báo không tìm thấy user hoặc cập nhật thất bại. |
| TC-UC-UU-004 | Khi đặc tả mở rộng chặn trùng email khi đổi hồ sơ => Kỳ vọng 400 + message trùng email (baseline theo API triển khai). | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User A: Id = 30, Email = `trungemail@mail.vn`.<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User B: Id = 31, Email = `khac@mail.vn`.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `PUT /api/User/UpdateUser/31`.<br>&nbsp;&nbsp;+) Body: đổi Email của B thành `trungemail@mail.vn` (trùng A), các field khác hợp lệ. | - Đặc tả: 400 + message trùng email. |
| TC-UC-UU-005 | Khi đặc tả mở rộng chặn trùng SĐT => Kỳ vọng 400 + message trùng SĐT. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User A: PhoneNumber = `0911000222`.<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User B: Email khác A, PhoneNumber khác A.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `PUT /api/User/UpdateUser/{id_B}`.<br>&nbsp;&nbsp;+) Body: PhoneNumber = `0911000222` (trùng A), các field khác hợp lệ. | - Đặc tả: 400 + message trùng SĐT. |

### 2.6 `DeleteUser`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-UC-DU-001 | Khi xóa người dùng với id tồn tại => HTTP 200 và message xóa thành công. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User Id = 55, Email = `delete55@test.vn`, Status = 1.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `DELETE /api/User/DeleteUser/55`.<br>&nbsp;&nbsp;+) Không body. | - HTTP: 200.<br>- Body: `Message` — thông báo xóa user thành công. |
| TC-UC-DU-002 | Khi id không tồn tại hoặc xóa thất bại => HTTP 404 và message not found / failed to delete. | - Cơ sở:<br>&nbsp;&nbsp;+ (A) Không có User Id = 66666; hoặc (B) mock service delete fail.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `DELETE /api/User/DeleteUser/66666` (hoặc id mock fail). | - HTTP: 404.<br>- Body: thông báo không tìm thấy user hoặc xóa thất bại. |

---

## 3. `ReviewService.cs`

### 3.1 `AddReviewAsync`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-RSV-ADD-001 | Khi khách đã quét QR hợp lệ trong vòng 30 ngày, model kiểm duyệt nội dung cho phép (class 0), và dữ liệu FK đầy đủ => Lưu đánh giá + ảnh vào DB, đồng thời gửi thông báo Firebase tới topic chủ nhà hàng (`admin_{RestaurantId}`). | - Cơ sở:<br>&nbsp;&nbsp;+ Review (cấu hình):<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `RequireQrScan` = true.<br>&nbsp;&nbsp;+ QRInformation:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) UserId = 1,<br>&nbsp;&nbsp;&nbsp;&nbsp;+) RestaurantId = 3,<br>&nbsp;&nbsp;&nbsp;&nbsp;+) CreateTime = Unix ms / giá trị sao cho thời điểm quét **trong vòng &lt; 30 ngày** so với thời điểm gọi request (UTC trong code).<br>&nbsp;&nbsp;+ ML mock:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) POST `/predict` trả JSON `{ "predicted_class_id": 0 }`.<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User Id=1, Restaurant Id=3 tồn tại (thỏa FK).<br>&nbsp;&nbsp;+ Firebase: mock hoặc spy để assert topic.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await AddReviewAsync(reviewDto)`.<br>&nbsp;&nbsp;+) `reviewDto` — UserId = 1, RestaurantId = 3, Content = `"Món phở rất ngon, phục vụ chu đáo."`, Score = 5, CreateDate = Unix ms hiện tại (hoặc cố định trong test), PhotoUrls = `["/uploads/review1.jpg"]`. *(Không set ReportsCount khi thêm — field thường chỉ đọc sau khi có báo cáo.)* | - Trả về:<br>&nbsp;&nbsp;+) success (tuple `Success`) = true.<br>&nbsp;&nbsp;+) message: thông báo thêm review thành công.<br>- DB:<br>&nbsp;&nbsp;+) Bảng `reviews`: thêm 1 dòng; UserId, RestaurantId, Content, Score khớp dto.<br>&nbsp;&nbsp;+) `ReviewPhoto`: số dòng = số phần tử `PhotoUrls`; mỗi `ImageUrl` khớp URL đã gửi; cùng `ReviewId` mới.<br>- Hành động phụ / tích hợp:<br>&nbsp;&nbsp;+) Firebase: gửi thông báo tới topic `admin_3` (theo `admin_{RestaurantId}` trong code); title/body chứa thông tin đánh giá mới (assert mock nếu có). |
| TC-RSV-ADD-002 | Khi cấu hình bắt buộc quét QR nhưng chưa có bản ghi quét cho cặp User–Nhà hàng => Ném exception với message bắt buộc quét QR trước khi viết review. | - Cơ sở:<br>&nbsp;&nbsp;+ Review (cấu hình):<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `RequireQrScan` = true.<br>&nbsp;&nbsp;+ QRInformation (repo / DB):<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Không có dòng nào khớp UserId + RestaurantId của dto.<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User Id = 1, Restaurant Id = 3 tồn tại (FK).<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await AddReviewAsync(reviewDto)`.<br>&nbsp;&nbsp;+) `reviewDto` — UserId = 1, RestaurantId = 3, Content = `"Chưa quét QR."`, Score = 4, CreateDate = Unix ms, PhotoUrls = `[]` hoặc null. | - Trả về:<br>&nbsp;&nbsp;+) Throw Exception; message chứa thông báo rằng cần quét QR trước khi viết review. |
| TC-RSV-ADD-003 | Khi đã quét QR nhưng thời điểm quét quá 30 ngày so với thời điểm gọi => Chặn và báo QR hết hạn, yêu cầu quét lại. | - Cơ sở:<br>&nbsp;&nbsp;+ Review (cấu hình):<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `RequireQrScan` = true.<br>&nbsp;&nbsp;+ QRInformation:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) UserId = 1, RestaurantId = 3,<br>&nbsp;&nbsp;&nbsp;&nbsp;+) CreateTime = Unix ms sao cho **&gt; 30 ngày** trước `DateTime.UtcNow` tại thời điểm test (cố định bằng clock fake hoặc offset).<br>&nbsp;&nbsp;+ DB seed: User 1, Restaurant 3 tồn tại.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await AddReviewAsync(reviewDto)`.<br>&nbsp;&nbsp;+) `reviewDto` — UserId = 1, RestaurantId = 3, Content = `"QR đã hết hạn."`, Score = 5, CreateDate = Unix ms hiện tại. | - Trả về:<br>&nbsp;&nbsp;+) Exception; message chứa thông báo rằng lần quét QR đã hết hạn, cần quét lại. |
| TC-RSV-ADD-004 | Khi model ML trả lớp khác 0 (nội dung không được phép) => Không lưu review; trả Success false và message kiểm duyệt tiếng Việt. | - Cơ sở:<br>&nbsp;&nbsp;+ Review / QR: giống điều kiện thành công (RequireQrScan off **hoặc** QR hợp lệ trong 30 ngày).<br>&nbsp;&nbsp;+ ML mock:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) POST `/predict` trả `{ "predicted_class_id": 1 }` (hoặc ≠ 0).<br>&nbsp;&nbsp;+ DB seed: User + Restaurant thỏa FK.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await AddReviewAsync(reviewDto)`.<br>&nbsp;&nbsp;+) `reviewDto` — Content = `"Nội dung tiêu cực giả định"`, Score = 2, UserId/RestaurantId/CreateDate/PhotoUrls hợp lệ. | - Trả về:<br>&nbsp;&nbsp;+) Success = false.<br>&nbsp;&nbsp;+) Message = chuỗi moderation tiếng Việt trong `ReviewService`.<br>- DB: không thêm review. |
| TC-RSV-ADD-005 | Khi nội dung review trống hoặc chỉ khoảng trắng => Bỏ qua gọi ML; nếu các điều kiện khác thỏa thì vẫn lưu và thông báo như luồng bình thường. | - Cơ sở:<br>&nbsp;&nbsp;+ Review (cấu hình):<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `RequireQrScan` = false **hoặc** có QRInformation hợp lệ như TC-RSV-ADD-001.<br>&nbsp;&nbsp;+ ML mock:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Spy: không được gọi khi Content trim rỗng (assert).<br>&nbsp;&nbsp;+ DB seed + Firebase mock: như luồng lưu thành công.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await AddReviewAsync(reviewDto)`.<br>&nbsp;&nbsp;+) `reviewDto` — Content = `null` hoặc `"   "`, Score = 4, UserId = 1, RestaurantId = 3, CreateDate = Unix ms, PhotoUrls tùy (vd `[]`). | - Trả về:<br>&nbsp;&nbsp;+) Success = true nếu save + Firebase ok.<br>- Hành động phụ: không gọi ML khi không có ký tự có nghĩa (theo `ModerateCommentWithMlAsync`). |
| TC-RSV-ADD-006 | Khi dịch vụ ML không phản hồi hoặc trả lỗi HTTP => Không chặn vì lý do ML; tiếp tục lưu nếu DB và Firebase thành công. | - Cơ sở:<br>&nbsp;&nbsp;+ ML mock:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) HttpClient trả HTTP 503 hoặc `TaskCanceledException` (timeout).<br>&nbsp;&nbsp;+ QR + DB seed + Firebase mock: đủ để lưu như TC-RSV-ADD-001 (RequireQrScan/QR tùy cấu hình test).<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await AddReviewAsync(reviewDto)`.<br>&nbsp;&nbsp;+) `reviewDto` — Content = `"Có chữ để ML được gọi nhưng lỗi mạng."`, Score = 5, các Id/FK hợp lệ. | - Trả về: tiếp tục lưu; Success = true nếu DB + Firebase ok. |
| TC-RSV-ADD-007 | Khi lưu DB hoặc Firebase ném exception sau khi đã qua các bước kiểm tra => Trả Success false và message lỗi thêm review kèm chi tiết. | - Cơ sở:<br>&nbsp;&nbsp;+ ML mock:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `predicted_class_id` = 0.<br>&nbsp;&nbsp;+ Mock:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `ReviewRepository.AddAsync` throw **hoặc** Firebase service throw sau khi ML pass.<br>&nbsp;&nbsp;+ QR/DB seed: đủ điều kiện tới bước lưu.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await AddReviewAsync(reviewDto)` với dto đầy đủ giống TC-RSV-ADD-001 (UserId, RestaurantId, Content có chữ, Score, PhotoUrls). | - Trả về:<br>&nbsp;&nbsp;+) Success = false.<br>&nbsp;&nbsp;+) Message báo lỗi khi thêm review (có thể kèm chi tiết). |
| TC-RSV-ADD-008 | Khi gửi nhiều URL ảnh kèm đủ điều kiện QR/ML/Firebase giống luồng thành công => Mỗi URL tạo một dòng ReviewPhoto đúng ReviewId mới. | - Cơ sở:<br>&nbsp;&nbsp;+ Review (cấu hình): `RequireQrScan` = true.<br>&nbsp;&nbsp;+ QRInformation: UserId = 1, RestaurantId = 3, CreateTime trong 30 ngày.<br>&nbsp;&nbsp;+ ML mock: `predicted_class_id` = 0.<br>&nbsp;&nbsp;+ DB seed: User 1, Restaurant 3; Firebase mock.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await AddReviewAsync(reviewDto)`.<br>&nbsp;&nbsp;+) `reviewDto` — PhotoUrls = `["/img/a.jpg","/img/b.jpg"]`, Content = `"Hai ảnh minh họa."`, Score = 5, UserId = 1, RestaurantId = 3, CreateDate = Unix ms. | - DB:<br>&nbsp;&nbsp;+) Số `ReviewPhoto` = 2; ImageUrl khớp từng URL. |

### 3.2 `DeleteReviewAsync`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-RSV-DEL-001 | Khi xóa review với id tồn tại => Xóa khỏi DB và trả message xóa thành công. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Review Id = 12, UserId = 2, RestaurantId = 4, Content = `"Sẽ xóa"`, Score = 3.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await DeleteReviewAsync(12)`. | - Trả về: Success = true; Message: thông báo xóa review thành công.<br>- DB: không còn review 12. |
| TC-RSV-DEL-002 | Khi id review không tồn tại => Success false và message không tìm thấy review. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Không có Review Id = 999 trong bảng `reviews`.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await DeleteReviewAsync(999)`. | - Trả về: Success = false; Message: thông báo không tìm thấy review. |
| TC-RSV-DEL-003 | Khi lớp repository/lưu trữ ném lỗi không phải not-found => Success false và message lỗi hệ thống khi xóa. | - Cơ sở:<br>&nbsp;&nbsp;+ Mock:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `ReviewRepository.DeleteAsync` (hoặc tương đương) throw `InvalidOperationException("DB timeout")`.<br>&nbsp;&nbsp;+ DB seed: Review Id = 12 tồn tại (nếu code gọi delete sau khi load).<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await DeleteReviewAsync(12)`. | - Trả về: Success = false; Message: thông báo lỗi hệ thống khi xóa review. |

### 3.3 `UpdateReviewAsync`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-RSV-UPD-001 | Khi review tồn tại, ML cho phép, và DTO có nội dung/điểm/ảnh mới => Cập nhật DB và trả message cập nhật thành công. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Review Id = 5, UserId = 2, RestaurantId = 4, Content cũ = `"Cũ"`, Score = 3.<br>&nbsp;&nbsp;+ ML mock:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `predicted_class_id` = 0 cho nội dung mới.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await UpdateReviewAsync(5, dto)`.<br>&nbsp;&nbsp;+) `dto` — Content = `"Đã chỉnh sửa, nội dung lành mạnh."`, Score = 5, PhotoUrls = `["/new1.jpg"]`. | - Trả về: Success = true; Message: thông báo cập nhật review thành công.<br>- DB: khớp dto. |
| TC-RSV-UPD-002 | Khi DTO gửi PhotoUrls = null => Ghi nhận hành vi repo (xóa ảnh hay giữ ảnh cũ — baseline theo code). | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Review Id = 8 tồn tại; đã có 1 `ReviewPhoto` ImageUrl = `/old.jpg`.<br>&nbsp;&nbsp;+ ML mock: class 0 nếu có kiểm duyệt lại nội dung.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await UpdateReviewAsync(8, dto)`.<br>&nbsp;&nbsp;+) `dto` — Content = `"Chỉ đổi chữ, không gửi ảnh."`, Score = 4, **PhotoUrls = null**. | - Trả về: ghi nhận hành vi repo (ảnh clear hay giữ — baseline quan sát). |
| TC-RSV-UPD-003 | Khi cập nhật với id không tồn tại => Success false và message không tìm thấy review. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Không có Review Id = 99999.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await UpdateReviewAsync(99999, dto)`.<br>&nbsp;&nbsp;+) `dto` — Content = `"Bất kỳ"`, Score = 5 (hợp lệ). | - Trả về: Success = false; Message: thông báo không tìm thấy review. |
| TC-RSV-UPD-004 | Khi nội dung sau chỉnh sửa bị ML chặn (class ≠ 0) => Không cập nhật thành công; message giống luồng thêm review. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Review Id = 6 tồn tại.<br>&nbsp;&nbsp;+ ML mock:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `predicted_class_id` ≠ 0 cho Content mới.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await UpdateReviewAsync(6, dto)`.<br>&nbsp;&nbsp;+) `dto` — Content = `"Nội dung vi phạm sau chỉnh sửa."`, Score = 1. | - Trả về: Success = false, message như TC-RSV-ADD-004. |
| TC-RSV-UPD-005 | Khi lớp lưu trữ ném exception trong lúc cập nhật => Trả message lỗi khi cập nhật review. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Review Id = 7 tồn tại.<br>&nbsp;&nbsp;+ Mock:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `ReviewRepository.UpdateAsync` throw `Exception("Update failed")`.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await UpdateReviewAsync(7, dto)` với dto hợp lệ, ML mock class 0. | - Trả về: Message thông báo lỗi hệ thống khi cập nhật review. |

### 3.4 `GetAllReviewsAsync`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-RSV-GAR-001 | Khi trong DB có ít nhất một review kèm quan hệ user/nhà hàng/báo cáo/ảnh => Danh sách DTO đầy đủ các trường tổng hợp (user, tên nhà, reports, ảnh). | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User Id = 10, Restaurant Id = 20 (tên `Nhà Hàng Phở`).<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Review Id = 100: UserId = 10, RestaurantId = 20, Content = `"Hay"`, Score = 5, ReportsCount = 1.<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `ReviewPhoto`: ImageUrl = `/r100.jpg` gắn Review 100.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await GetAllReviewsAsync()`. | - Trả về: mỗi DTO có User, RestaurantName, ReportsCount, PhotoUrls. |
| TC-RSV-GAR-002 | Khi bảng reviews rỗng => Trả về danh sách rỗng. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Bảng `reviews` rỗng (hoặc DbContext mới không insert review).<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await GetAllReviewsAsync()`. | - Trả về: rỗng. |

### 3.5 `GetReviewsByUserIdAsync`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-RSV-GUR-001 | Khi một user có đúng hai đánh giá trong DB => Trả đủ 2 bản ghi, mọi item cùng UserId. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Review A: Id = 41, UserId = 7, RestaurantId = 1, Content = `"Một"`, Score = 5.<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Review B: Id = 42, UserId = 7, RestaurantId = 2, Content = `"Hai"`, Score = 4.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await GetReviewsByUserIdAsync(7)`. | - Trả về: Count = 2; mọi item UserId = 7. |
| TC-RSV-GUR-002 | Khi user không có review nào => Trả danh sách rỗng. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) User Id = 99 tồn tại nhưng không có dòng `reviews` nào với UserId = 99.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await GetReviewsByUserIdAsync(99)`. | - Trả về: rỗng. |

### 3.6 `GetReviewsByRestaurantIdAsync`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-RSV-GRR-001 | Khi nhà hàng có ít nhất một đánh giá => Mọi phần tử trả về cùng RestaurantId đã seed. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Restaurant Id = 4 (tên `Quán Bún`).<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Review Id = 51, UserId = 3, RestaurantId = 4, Content = `"Ngon"`, Score = 5.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await GetReviewsByRestaurantIdAsync(4)`. | - Trả về: mọi item RestaurantId = 4. |
| TC-RSV-GRR-002 | Khi nhà hàng không có review => Trả danh sách rỗng. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Restaurant Id = 60 tồn tại; không có `reviews.RestaurantId = 60`.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await GetReviewsByRestaurantIdAsync(60)`. | - Trả về: rỗng. |

### 3.7 `GetReviewsWithHighReportsAsync`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-RSV-GHR-001 | Khi có review với số báo cáo khác nhau và truyền ngưỡng reportCount => Chỉ trả các review vượt ngưỡng theo rule repository. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Review R1: ReportsCount = 1.<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Review R2: ReportsCount = 5.<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Review R3: ReportsCount = 10.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await GetReviewsWithHighReportsAsync(reportCount: 3)` (hoặc tên tham số đúng chữ ký service). | - Trả về: chỉ review thỏa rule repo (&gt; 3). |
| TC-RSV-GHR-002 | Khi đặt ngưỡng rất cao (không review nào đạt) => Trả danh sách rỗng. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Giống TC-RSV-GHR-001 (ReportsCount tối đa = 10).<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `await GetReviewsWithHighReportsAsync(reportCount: 999)`. | - Trả về: rỗng. |

---

## 4. `ReviewController.cs`

### 4.1 `AddReview`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-RC-ADD-001 | Khi client không gửi body đánh giá => HTTP 400 và message dữ liệu không được null. | - Cơ sở:<br>&nbsp;&nbsp;+ Host test: WebApplicationFactory / API integration.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `POST /api/reviews` (đường dẫn đúng route project).<br>&nbsp;&nbsp;+) Body: `null` hoặc không gửi JSON; header `Content-Type: application/json` tùy client test. | - HTTP: 400.<br>- Body: message thông báo dữ liệu review không được null. |
| TC-RC-ADD-002 | Khi service thêm review thành công (QR/ML/Firebase như TC-RSV-ADD-001) => HTTP 200 và JSON message thêm review thành công. | - Cơ sở:<br>&nbsp;&nbsp;+ DB + QR + ML + Firebase: giống **Input** của TC-RSV-ADD-001 (seed User 1, Restaurant 3, QR trong 30 ngày, ML class 0).<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `POST /api/reviews`.<br>&nbsp;&nbsp;+) Body JSON khớp `ReviewDto`: UserId = 1, RestaurantId = 3, Content = `"Món phở rất ngon, phục vụ chu đáo."`, Score = 5, CreateDate, PhotoUrls = `["/uploads/review1.jpg"]`.<br>&nbsp;&nbsp;+) `Content-Type: application/json`. | - HTTP: 200.<br>- Body: JSON có key message — thông báo thêm review thành công. |
| TC-RC-ADD-003 | Khi service trả Success false (vd ML chặn hoặc mock) => HTTP 500 và body chứa message lỗi từ service. | - Cơ sở:<br>&nbsp;&nbsp;+ ML mock: `predicted_class_id` ≠ 0 **hoặc** mock `AddReviewAsync` trả `(false, "…")`.<br>&nbsp;&nbsp;+ DB seed User/Restaurant + JSON body hợp lệ.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `POST /api/reviews` với body đầy đủ field (UserId, RestaurantId, Content vi phạm nếu test ML). | - HTTP: 500.<br>- Body: JSON message — nội dung lỗi từ service (vd kiểm duyệt). |
| TC-RC-ADD-004 | Khi service ném exception quy tắc QR (chưa quét / hết hạn) => HTTP 400 và body message exception/QR. | - Cơ sở:<br>&nbsp;&nbsp;+ (A) Giống TC-RSV-ADD-002: RequireQrScan, không có QRInformation; hoặc (B) Giống TC-RSV-ADD-003: QR quá 30 ngày.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `POST /api/reviews` với JSON dto khớp từng kịch bản (UserId/RestaurantId/Content/Score…). | - HTTP: 400.<br>- Body: JSON message — mô tả lỗi nghiệp vụ (QR / điều kiện quét mã). |

### 4.2 `DeleteReview`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-RC-DEL-001 | Khi xóa review với id tồn tại qua API => HTTP 200 và body message xóa thành công. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed: Review Id = 12 tồn tại.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `DELETE /api/reviews/12` (route đúng project).<br>&nbsp;&nbsp;+) Không body. | - HTTP: 200; body message xóa thành công. |
| TC-RC-DEL-002 | Khi id không tồn tại => HTTP 404 và message không tìm thấy review. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed: không có Review Id = 88888.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `DELETE /api/reviews/88888`. | - HTTP: 404; body thông báo không tìm thấy review. |

### 4.3 `UpdateReview`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-RC-UPD-001 | Khi PUT id và DTO hợp lệ, service cập nhật thành công => HTTP 200 và message updated. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed: Review Id = 5 tồn tại; ML mock class 0.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `PUT /api/reviews/5`.<br>&nbsp;&nbsp;+) Body JSON: Content, Score, PhotoUrls như TC-RSV-UPD-001.<br>&nbsp;&nbsp;+) `Content-Type: application/json`. | - HTTP: 200; body thông báo cập nhật review thành công. |
| TC-RC-UPD-002 | Khi id sai hoặc cập nhật thất bại => HTTP 404 theo controller hiện tại. | - Cơ sở:<br>&nbsp;&nbsp;+ Không có Review Id = 99999 **hoặc** mock service fail.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `PUT /api/reviews/99999`.<br>&nbsp;&nbsp;+) Body JSON DTO hợp lệ (Content = `"X"`, Score = 3). | - HTTP: 404. |

### 4.4 `GetAllReviews`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-RC-GAR-001 | Khi gọi GET toàn bộ reviews => HTTP 200 và mảng JSON. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed: có thể rỗng hoặc có review như TC-RSV-GAR-001.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `GET /api/reviews`.<br>&nbsp;&nbsp;+) Không body; auth header nếu endpoint yêu cầu. | - HTTP: 200; JSON array. |

### 4.5 `GetReviewsByUserId`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-RC-GUR-001 | Khi user có review => HTTP 200 và mảng kết quả. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed: UserId = 7 có ≥1 review (vd TC-RSV-GUR-001).<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `GET /api/reviews/by-user/7` (route đúng project). | - HTTP: 200; array. |
| TC-RC-GUR-002 | Khi user không có review => HTTP 404 và chuỗi thông báo (theo controller). | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed: User Id = 99 không có review (TC-RSV-GUR-002).<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `GET /api/reviews/by-user/99`. | - HTTP: 404; chuỗi thông báo (vd không có dữ liệu — theo controller). |

### 4.6 `GetReviewsByRestaurantId`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-RC-GRR-001 | Khi nhà hàng có review => HTTP 200 và mảng. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed: RestaurantId = 4 có review (TC-RSV-GRR-001).<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `GET /api/reviews/by-restaurant/4`. | - HTTP: 200; array. |
| TC-RC-GRR-002 | Khi nhà hàng không có review => HTTP 404 và chuỗi message. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed: RestaurantId = 60 không có review.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `GET /api/reviews/by-restaurant/60`. | - HTTP: 404; chuỗi thông báo (theo controller). |

### 4.7 `GetReviewsWithHighReports`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-RC-GHR-001 | Khi gọi endpoint high-reports không query => HTTP 200 và danh sách theo mặc định service. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed: có thể rỗng hoặc có review nhiều báo cáo.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `GET /api/reviews/high-reports` (không query `reportCount`). | - HTTP: 200; array. |
| TC-RC-GHR-002 | Khi truyền `?reportCount=2` với dữ liệu seed phù hợp => HTTP 200 và các phần tử thỏa ngưỡng. | - Cơ sở:<br>&nbsp;&nbsp;+ DB seed: nhiều review với ReportsCount 0,1,3,5 (giống TC-RSV-GHR-001).<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `GET /api/reviews/high-reports?reportCount=2`. | - HTTP: 200; phần tử đúng rule. |

---

## 5. `NotificationsController.cs`

### 5.1 `TestFirebase`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-NC-TFB-001 | Khi gọi test-firebase với topic không rỗng và FCM mock thành công => HTTP 200 và body có messageId + topic. | - Cơ sở:<br>&nbsp;&nbsp;+ Firebase / FCM:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) Mock trả `messageId` + echo `topic`.<br>&nbsp;&nbsp;+ Host: controller `NotificationsController` trong integration test.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `GET /api/Notifications/test-firebase?topic=nha_hang_pho_thin` (đường dẫn đúng prefix project).<br>&nbsp;&nbsp;+) Không body. | - HTTP: 200.<br>- Body: `messageId`, `topic`. |
| TC-NC-TFB-002 | Khi tham số topic thiếu hoặc chỉ khoảng trắng => HTTP 400 và message yêu cầu topic. | - Cơ sở:<br>&nbsp;&nbsp;+ — (không cần seed DB).<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `GET .../test-firebase` không có query `topic`; hoặc `?topic=`; hoặc `?topic=%20`. | - HTTP: 400; message yêu cầu nhập topic. |
| TC-NC-TFB-003 | Khi Firebase/FCM mock ném lỗi dù topic hợp lệ => HTTP 500 và body chứa thông tin lỗi. | - Cơ sở:<br>&nbsp;&nbsp;+ Mock Firebase:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `SendAsync` throw exception message `FCM lỗi`.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `GET .../test-firebase?topic=nha_hang_pho_thin`. | - HTTP: 500.<br>- Body: JSON chứa thông tin lỗi gửi qua FCM (vd key `error`). |

### 5.2 `SendNotification`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-NC-SND-001 | Khi POST đủ Topic, Title, Body và FCM thành công => HTTP 200 và MessageId. | - Cơ sở:<br>&nbsp;&nbsp;+ Mock FCM trả MessageId = `"msg_001"`.<br>&nbsp;&nbsp;+ Header:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `Content-Type: application/json`.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `POST /api/Notifications/send-notification` (route đúng project).<br>&nbsp;&nbsp;+) Body JSON: `Topic` = `nha_hang_pho_thin`, `Title` = `Thông báo thử`, `Body` = `Nội dung tin nhắn mẫu.` | - HTTP: 200; `MessageId`. |
| TC-NC-SND-002 | Khi thiếu trường Topic trong payload => HTTP 400 và message payload không hợp lệ. | - Cơ sở:<br>&nbsp;&nbsp;+ —<br>- Test:<br>&nbsp;&nbsp;+) POST cùng endpoint TC-NC-SND-001.<br>&nbsp;&nbsp;+) Body JSON: có `Title`, có `Body`, **không có** key `Topic` (hoặc `Topic` = null / `""`).<br>&nbsp;&nbsp;+) `Content-Type: application/json`. | - HTTP: 400; message thông báo payload không hợp lệ. |
| TC-NC-SND-003 | Khi thiếu Title => Giống TC-NC-SND-002 (400, payload không hợp lệ). | - Cơ sở:<br>&nbsp;&nbsp;+ —<br>- Test:<br>&nbsp;&nbsp;+) POST cùng endpoint TC-NC-SND-001.<br>&nbsp;&nbsp;+) Body JSON: có `Topic`, có `Body`, **không có** key `Title` (hoặc `Title` = null). | - Giống TC-NC-SND-002. |
| TC-NC-SND-004 | Khi thiếu Body => Giống TC-NC-SND-002. | - Cơ sở:<br>&nbsp;&nbsp;+ —<br>- Test:<br>&nbsp;&nbsp;+) POST cùng endpoint TC-NC-SND-001.<br>&nbsp;&nbsp;+) Body JSON: có `Topic`, có `Title`, **không có** key `Body`. | - Giống TC-NC-SND-002. |
| TC-NC-SND-005 | Khi payload đủ nhưng Firebase ném exception => HTTP 500 và object Error. | - Cơ sở:<br>&nbsp;&nbsp;+ Mock Firebase throw message `Gửi lỗi`.<br>- Test:<br>&nbsp;&nbsp;+) POST body giống TC-NC-SND-001 (đủ Topic, Title, Body). | - HTTP: 500; body JSON chứa thông tin lỗi gửi tin. |

---

## 6. `PhotoController.cs`

### 6.1 `UploadImage`

| Test Case ID | Test Objective | Input | Expected Output |
|--------------|----------------|-------|-----------------|
| TC-PC-UPL-001 | Khi upload file ảnh hợp lệ và thư mục `wwwroot/images` ghi được => HTTP 200 và JSON đường dẫn `/images/<guid>.jpg`; file tồn tại trên đĩa (integration). | - Cơ sở:<br>&nbsp;&nbsp;+ Thư mục `wwwroot/images` ghi được.<br>- Test:<br>&nbsp;&nbsp;+) POST multipart, file `mon_an.jpg`. | - HTTP: 200.<br>- Body: JSON có key đường dẫn ảnh (dạng `/images/<tên-file>.jpg`).<br>- Hành động phụ: file tồn trên đĩa (integration). |
| TC-PC-UPL-002 | Khi ghi file lên đĩa thất bại (path chỉ đọc hoặc mock lỗi) => HTTP 500 và message lỗi máy chủ nội bộ. | - Cơ sở:<br>&nbsp;&nbsp;+ Môi trường:<br>&nbsp;&nbsp;&nbsp;&nbsp;+) `wwwroot/images` chỉ đọc **hoặc** mock `FileStream` throw.<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `POST /api/Photo/upload` (route đúng project).<br>&nbsp;&nbsp;+) `multipart/form-data`: field file tên `mon_an.jpg`, nội dung byte giả lập PNG/JPEG hợp lệ. | - HTTP: 500; message lỗi máy chủ nội bộ. |
| TC-PC-UPL-003 | Khi request không có IFormFile hoặc file null => Ghi nhận hành vi/hoặc exception — baseline status và body theo code hiện tại. | - Cơ sở:<br>&nbsp;&nbsp;+ —<br>- Test:<br>&nbsp;&nbsp;+) Gọi: `POST /api/Photo/upload` với `multipart/form-data` **không** chứa part file; hoặc part đúng tên nhưng `length = 0` / null — ghi nhận response thực tế. | - Hiện trạng: có thể exception — ghi nhận status/body baseline. |

---

## Phụ lục: Ánh xạ ID ↔ dòng cây testcase

- **UserService:** TC-USV-*  
- **UserController:** TC-UC-*  
- **ReviewService / ReviewController:** TC-RSV-*, TC-RC-*  
- **Notifications / Photo:** TC-NC-*, TC-PC-*  
