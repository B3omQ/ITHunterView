# BẢN ĐĂNG KÝ HỆ THỐNG THAY ĐỔI (CHANGES REGISTRY)
*Tài liệu này ghi lại toàn bộ thay đổi cấu trúc mã nguồn Backend nhằm truy vết ảnh hưởng đồng bộ khi nâng cấp.*

---

## 1. Phân Hệ: Administrative User Governance Backend
*Ngày cập nhật: 20-06-2026*

### Các Tệp Thay Đổi & Sự Ảnh Hưởng Liên Đới:

#### A. Domain Layer (Thực thể và Ràng buộc Dữ liệu)
1. **[RecruiterProfiles.cs](file:///d:/Desktop/FU/do%20an/Capstone Project/backend/ITHunterview.Domain/Entities/RecruiterProfiles.cs)**
   - *Thay đổi:* Thêm Navigation Property `Company` và khóa ngoại `company_id`.
   - *Ảnh hưởng:* Thay đổi cấu trúc cơ sở dữ liệu (Database Schema). Yêu cầu chạy migration. Ảnh hưởng đến các truy vấn liên quan đến hồ sơ nhà tuyển dụng (cần Eager/Lazy loading nếu muốn lấy thông tin công ty).

#### B. Persistence Layer (Truy cập cơ sở dữ liệu)
2. **[ITHunterviewContext.cs](file:///d:/Desktop/FU/do%20an/Capstone Project/backend/ITHunterview.Service/Infrastructure/Persistence/ITHunterviewContext.cs)**
   - *Thay đổi:* Cấu hình quan hệ Một-Nhiều/Một-Một Fluent API giữa `RecruiterProfiles` và `Companies`.
   - *Ảnh hưởng:* Thay đổi cách EF Core khởi tạo mô hình quan hệ cơ sở dữ liệu.
3. **[IUserRepository.cs](file:///d:/Desktop/FU/do%20an/Capstone%20Project/backend/ITHunterview.Service/Interface/Persistence/IUserRepository.cs)**
   - *Thay đổi:* Loại bỏ hoàn toàn các khai báo phương thức liên quan đến log hoạt động (`AddActivityLogAsync`, `GetPagedActivityLogsAsync`, `PurgeActivityLogsAsync`) để bàn giao trách nhiệm cho `IAuditLogRepository`. Chỉ giữ lại các phương thức quản trị thông tin User.
   - *Ảnh hưởng:* Đồng bộ hóa cấu trúc UserRepository và các UseCase sử dụng.
4. **[UserRepository.cs](file:///d:/Desktop/FU/do%20an/Capstone%20Project/backend/ITHunterview.Service/Infrastructure/Persistence/UserRepository.cs)**
   - *Thay đổi:* Xóa các phương thức truy vấn và tương tác với bảng log hoạt động (`AddActivityLogAsync`, `GetPagedActivityLogsAsync`, `PurgeActivityLogsAsync`).
   - *Ảnh hưởng:* Làm tinh gọn và chuyên biệt hóa UserRepository, tránh việc truy vấn chéo thực thể không thuộc phạm vi trách nhiệm.

#### C. Service & Business Layer (Logic nghiệp vụ)
5. **[IUserGovernanceUseCase.cs](file:///d:/Desktop/FU/do%20an/Capstone%20Project/backend/ITHunterview.Service/Interface/UseCase/IUserGovernanceUseCase.cs)**
   - *Thay đổi:* Loại bỏ khai báo phương thức lấy lịch sử log phân trang (`GetPagedActivityLogsAsync`).
   - *Ảnh hưởng:* Ràng buộc hợp đồng nghiệp vụ với Controller.
6. **[UserGovernanceUseCase.cs](file:///d:/Desktop/FU/do%20an/Capstone%20Project/backend/ITHunterview.Service/UseCase/UserGovernanceUseCase.cs)**
   - *Thay đổi:* Loại bỏ phương thức `GetPagedActivityLogsAsync`. Điều chỉnh phương thức private `LogActivityAsync` gọi trực tiếp `IAuditLogRepository` để lưu log hoạt động thay vì qua `_userRepository`.
   - *Ảnh hưởng:* Giảm sự phụ thuộc chéo và đảm bảo dữ liệu log hoạt động quản trị được lưu trữ tập trung qua lớp Audit Log.
7. **[ServiceCollectionExtensions.cs](file:///d:/Desktop/FU/do%20an/Capstone%20Project/backend/ITHunterview.Service/Config/ServiceCollectionExtensions.cs)**
   - *Thay đổi:* Đăng ký Scoped cho `IUserGovernanceUseCase`.
   - *Ảnh hưởng:* Đăng ký phụ thuộc hệ thống. Nếu thiếu sẽ gây lỗi Runtime DI khi gọi Controller.
8. **[UserActivityLogDto.cs [DELETE]](file:///d:/Desktop/FU/do%20an/Capstone%20Project/backend/ITHunterview.Service/DTOs/UserGovernance/UserActivityLogDto.cs)**
   - *Thay đổi:* Xóa bỏ hoàn toàn tệp tin DTO do không còn sử dụng.
   - *Ảnh hưởng:* Rút gọn danh sách các DTO dư thừa trong phân hệ quản trị người dùng.

#### D. Web API Layer (Đầu cuối API)
9. **[UserGovernanceController.cs](file:///d:/Desktop/FU/do%20an/Capstone%20Project/backend/ITHunterview.WebAPI/Controllers/UserGovernanceController.cs)**
   - *Thay đổi:* Xóa bỏ endpoint `GET api/user-governance/activity-logs`.
   - *Ảnh hưởng:* Client/Frontend khi gọi lấy log hoạt động bắt buộc phải chuyển sang endpoint thống nhất `GET api/v1/audit-logs`.

---

## 2. Hướng dẫn Đồng bộ khi chỉnh sửa (Sync Guideline)
- **Khi sửa cơ chế Refresh Token/Session:** Bắt buộc kiểm tra `UserGovernanceUseCase.cs` tại phương thức `UpdateUserStatusAsync` và `UpdateUserRoleAsync` để đảm bảo việc thu hồi token vẫn hoạt động đồng bộ.
- **Khi sửa thực thể `User`, `CandidateProfiles`, `RecruiterProfiles`, hoặc `Companies`:** Kiểm tra lập tức hàm mapper trong `UserGovernanceUseCase.cs` (`GetPagedUsersAsync`, `GetUserDetailAsync`) để cập nhật các thuộc tính tương ứng tránh lỗi biên dịch hoặc lỗi runtime trả về null.
- **Khi thay đổi cấu hình bảo mật/JWT Claims:** Xác minh lại các hàm lấy claim `userId`, `email`, `role` tại `UserGovernanceController.cs`.

---

## 3. Phân Hệ: Audit Log Optimization (Tối ưu hóa Nhật ký Giám sát)
*Ngày cập nhật: 23-06-2026*

### Các Tệp Thay Đổi & Sự Ảnh Hưởng Liên Đới:

#### A. Interface & Infrastructure (Hạ tầng lưu trữ Log bất đồng bộ)
1. **[IAuditLogQueue.cs](file:///d:/Desktop/FU/do%20an/Capstone%20Project/backend/ITHunterview.Service/Interface/Infrastructure/IAuditLogQueue.cs)**
   - *Thay đổi:* Bổ sung các phương thức `WaitToReadAsync` và `TryDequeue` vào interface.
   - *Ảnh hưởng:* Định nghĩa API để hỗ trợ việc đọc gom lô (batching) bất đồng bộ hiệu quả.
2. **[AuditLogQueue.cs](file:///d:/Desktop/FU/do%20an/Capstone%20Project/backend/ITHunterview.Service/Infrastructure/Infrastructure/AuditLogQueue.cs)**
   - *Thay đổi:* Chuyển từ `Channel.CreateUnbounded` sang `Channel.CreateBounded(10000)` để giới hạn dung lượng hàng đợi, tránh tràn bộ nhớ RAM (OOM) khi hệ thống chịu tải cao; cài đặt các phương thức gom lô.
   - *Ảnh hưởng:* Tăng tính ổn định hệ thống. Các tác nhân gửi log nếu vượt quá 10,000 sẽ chờ thay vì tiếp tục phình to bộ nhớ.

#### B. Middleware (Bộ lọc yêu cầu người dùng)
3. **[UserStatusCheckMiddleware.cs](file:///d:/Desktop/FU/do%20an/Capstone%20Project/backend/ITHunterview.WebAPI/Middlewares/UserStatusCheckMiddleware.cs)**
   - *Thay đổi:* Thay thế `IAuditLogRepository` (truy xuất DB đồng bộ) bằng `IAuditLogQueue` (đưa vào hàng đợi bộ nhớ bất đồng bộ).
   - *Ảnh hưởng:* Khắc phục lỗi cạn kiệt Connection Pool của Database khi có đợt truy cập lớn bị từ chối (bảo vệ hệ thống khỏi các đợt quét bot/DDoS).

#### C. Background Services (Tiến trình nền ghi Log)
4. **[AuditLogProcessor.cs](file:///d:/Desktop/FU/do%20an/Capstone%20Project/backend/ITHunterview.WebAPI/BackgroundServices/AuditLogProcessor.cs)**
   - *Thay đổi:* Tái cấu trúc tiến trình nền để đọc log theo lô (batch size 50 hoặc flush sau tối đa 2 giây) sử dụng `AddRangeAsync` ghi vào cơ sở dữ liệu thay vì ghi đơn dòng.
   - *Ảnh hưởng:* Giảm số lượng truy vấn I/O đến cơ sở dữ liệu, tối ưu hóa băng thông Database.

#### D. Database Interceptor (Bẫy thay đổi dữ liệu EF Core)
5. **[AuditLogInterceptor.cs](file:///d:/Desktop/FU/do%20an/Capstone%20Project/backend/ITHunterview.Service/Infrastructure/Persistence/AuditLogInterceptor.cs)**
   - *Thay đổi:* Override bổ sung 3 phương thức đồng bộ `SavingChanges`, `SavedChanges`, `SaveChangesFailed` bên cạnh các phương thức bất đồng bộ. Cập nhật hàm `GetSnapshotDiff` để đính kèm Khóa chính (Primary Key) của thực thể bị tác động vào Snapshot.
   - *Ảnh hưởng:* Tránh việc bypass ghi log khi lập trình viên vô tình gọi `SaveChanges()` đồng bộ thay vì `SaveChangesAsync()`. Lưu trữ bối cảnh đầy đủ của thực thể bị thay đổi.

---

## 4. Phân Hệ: User Governance Security & Authorization Update
*Ngày cập nhật: 24-06-2026*

### Các Tệp Thay Đổi & Sự Ảnh Hưởng Liên Đới:

#### A. Web API Layer (Đầu cuối API)
1. **[UserGovernanceController.cs](file:///d:/Desktop/FU/do%20an/Capstone%20Project/backend/ITHunterview.WebAPI/Controllers/UserGovernanceController.cs)**
   - *Thay đổi:* Đổi Policy từ `[Authorize(Policy = "StaffOrAdmin")]` thành `[Authorize(Policy = "AdminOnly")]`.
   - *Ảnh hưởng:* Chặn nhân viên (Staff) truy cập toàn bộ các API quản trị tài khoản người dùng, chuyển quyền hạn độc quyền sang Admin.

#### B. Service & Business Layer (Logic nghiệp vụ)
2. **[UserGovernanceUseCase.cs](file:///d:/Desktop/FU/do%20an/Capstone%20Project/backend/ITHunterview.Service/UseCase/UserGovernanceUseCase.cs)**
   - *Thay đổi:*
     - Cập nhật logic `UpdateUserStatusAsync` để kiểm tra vai trò đích của người dùng cần thay đổi trạng thái. Nếu tài khoản đích là Admin, chặn ngay lập tức và ghi nhận log thất bại.
     - Vô hiệu hóa hoàn toàn phương thức `UpdateUserRoleAsync`, trả lỗi trực tiếp để hủy bỏ chức năng phân quyền tự do.
   - *Ảnh hưởng:* Bảo vệ tài khoản Admin khỏi việc bị vô hiệu hóa (lockout), đồng thời đóng băng cấu trúc vai trò của các tài khoản đã được phân lớp từ khi đăng ký.
