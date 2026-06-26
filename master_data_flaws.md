# DANH SÁCH KHUYẾT ĐIỂM HỆ THỐNG - PHÂN HỆ MASTER DATA
*Tài liệu ghi nhận toàn bộ các lỗ hổng thiết kế, lỗi logic nghiệp vụ, hiệu năng và UX của module Master Data (Skills, Skill Categories, Majors, Job Categories) từ Database đến Frontend.*

---

## 1. Tầng Database (PostgreSQL)

### 1.1. Thiếu Ràng buộc Unique ở mức Database đối với Skills
* **Hiện trạng:** Bảng `skills` trong database không có ràng buộc `UNIQUE` trên cột `name` hay `code`. Logic kiểm tra trùng lặp tên kỹ năng hiện tại chỉ được thực hiện bằng mã nguồn C# ở tầng Service (`SkillUseCase.cs` gọi `NormalizedName` so khớp).
* **Hậu quả:** Nếu có 2 requests đồng thời (concurrent requests) gửi lên để tạo cùng một kỹ năng, DB vẫn cho phép insert, dẫn đến việc trùng lặp dữ liệu thực tế trong DB mà tầng logic không kiểm soát được.
* **Cách khắc phục:** Thêm Unique Index hoặc Unique Constraint cho cột `name` hoặc `code` ở mức schema PostgreSQL.

### 1.2. Bất đối xứng và Không nhất quán trong Thiết kế Xóa (Soft Delete vs Hard Delete)
* **Hiện trạng:** 
  * Thực thể `Majors` sử dụng cơ chế **Soft Delete** (được đánh dấu bằng trường `deleted_at`, có Global Query Filter trong EF Core và có giao diện cho phép khôi phục - Restore).
  * Thực thể `Skills` và `SkillCategories` lại sử dụng cơ chế **Hard Delete** (xóa vật lý trực tiếp khỏi DB bằng lệnh `DELETE`).
* **Hậu quả:** Sự không đồng bộ này gây khó khăn trong việc quản trị dữ liệu. Khi Admin lỡ tay xóa một kỹ năng quan trọng, dữ liệu sẽ biến mất vĩnh viễn và phá vỡ tính toàn vẹn tham chiếu của các bảng liên kết (nếu không cấu hình ON DELETE SET NULL/CASCADE đúng cách) hoặc gây lỗi Foreign Key.
* **Cách khắc phục:** Đồng bộ hóa cơ chế Soft Delete cho toàn bộ các thực thể Master Data hoặc chuyển `Skills` sang sử dụng trạng thái hoạt động (`IsActive` / `Status`) kết hợp soft delete.

### 1.3. Thiếu Index Hiệu năng trên các Trường Tìm kiếm
* **Hiện trạng:** Các cột `name` hoặc `normalized_name` trong bảng `skills` và `majors` là các trường thường xuyên bị tìm kiếm bằng mệnh đề `ILIKE` hoặc `LIKE` ở cả Frontend lẫn các chức năng gợi ý của Candidate/Recruiter, nhưng hiện tại chưa được đánh index phù hợp.
* **Hậu quả:** Khi thư viện kỹ năng hoặc chuyên ngành phình to lên hàng ngàn bản ghi, các câu lệnh tìm kiếm dạng `ILIKE '%...%'` sẽ thực hiện quét toàn bộ bảng (Table Scan), gây suy giảm hiệu năng nghiêm trọng cho database.
* **Cách khắc phục:** Cấu hình Index sử dụng extension `pg_trgm` (GIN index) trên các trường văn bản thường xuyên tìm kiếm để tối ưu hóa truy vấn.

---

## 2. Tầng Backend Service (ITHunterview.Service)

### 2.1. Lỗi Over-fetching dữ liệu và xử lý trên RAM (In-Memory Processing)
* **Hiện trạng:** Trong `SkillUseCase.GetPagedSkillsAsync`, để lấy thông tin tên danh mục kỹ năng (`CategoryName`), UseCase gọi `_skillCategoryRepository.GetAllCategoriesAsync()` để tải toàn bộ danh mục lên bộ nhớ RAM rồi map thủ công:
  ```csharp
  var categories = await _skillCategoryRepository.GetAllCategoriesAsync();
  var categoryMap = categories.ToDictionary(c => c.Id, c => c.Name);
  // map categoryMap vào SkillDto...
  ```
* **Hậu quả:** Đây là lỗi thiết kế phản mẫu (anti-pattern) kinh điển. Nếu bảng danh mục kỹ năng có hàng ngàn bản ghi, việc tải toàn bộ vào RAM cho mỗi request phân trang sẽ gây nghẽn bộ nhớ và làm chậm API đáng kể.
* **Cách khắc phục:** Sử dụng câu lệnh LINQ `Join` hoặc `.Include()` ở tầng EF Core để database thực hiện JOIN và chỉ trả về các cột cần thiết (Projection DTO) thông qua câu lệnh SQL tối ưu.

### 2.2. Câu truy vấn kiểm tra ràng buộc (Constraint Checks) kém hiệu quả
* **Hiện trạng:** Để kiểm tra một kỹ năng có đang được sử dụng trước khi thay đổi trạng thái hoặc xóa, hệ thống thực hiện hai lệnh truy vấn đếm riêng lẻ tới database:
  ```csharp
  var userCount = await _candidateRepository.CountBySkillIdAsync(id);
  var jobCount = await _jobRepository.CountBySkillIdAsync(id);
  ```
* **Hậu quả:** Gây ra 2 lượt kết nối mạng (round-trip) tới database không cần thiết cho mỗi lần Admin thay đổi trạng thái kỹ năng, làm tăng tải mạng và trễ API.
* **Cách khắc phục:** Gộp chung logic kiểm tra này vào một câu truy vấn duy nhất trả về một cấu trúc DTO chứa cả hai số lượng hoặc kiểm tra sự tồn tại (dùng `.Any()`) thay vì đếm toàn bộ (`.Count()`) để DB dừng quét ngay khi tìm thấy bản ghi đầu tiên.

### 2.3. Hardcode Ngôn ngữ Thông báo Lỗi
* **Hiện trạng:** Trong `SkillUseCase.UpdateSkillStatusAsync`, thông báo lỗi trả về khi kỹ năng đang được sử dụng được hardcode bằng tiếng Anh:
  ```csharp
  throw new BadRequestException($"Skill is currently used by {userCount} candidates and {jobCount} jobs. Are you sure you want to deactivate it?");
  ```
  Nhưng Frontend lại sử dụng tiếng Việt để bắt lỗi logic.
* **Hậu quả:** Sự bất đồng bộ này trực tiếp phá hỏng logic xử lý lỗi ở Frontend (xem mục 4.1).
* **Cách khắc phục:** Triển khai localization `.resx` ở Backend hoặc trả về mã lỗi có cấu trúc (ví dụ: `ErrorCode: "SKILL_IN_USE"`, kèm metadata chứa số lượng candidates/jobs) để Frontend tự xử lý hiển thị ngôn ngữ.

---

## 3. Tầng API Controller (ITHunterview.WebAPI)

### 3.1. Thiết kế API vi phạm Nguyên tắc Đơn nhiệm (Single Responsibility Principle)
* **Hiện trạng:** Tệp `MasterDataController.cs` gộp cả các endpoint quản lý của `Skills`, `Majors`, và cả danh sách `SkillCategories` vào chung một Controller.
* **Hậu quả:** Tệp tin trở nên cồng kềnh, khó quản lý phân quyền chi tiết cho từng loại master data trong tương lai (ví dụ: chỉ cho phép staff quản lý Skills nhưng không cho quản lý Majors).
* **Cách khắc phục:** Tách `MasterDataController` thành các controller độc lập như `SkillsController`, `MajorsController` và `CategoriesController`.

### 3.2. Thiếu hoàn toàn API CRUD cho Danh mục (Categories)
* **Hiện trạng:** Mặc dù cả hai thực thể `skill_categories` và `job_categories` là xương sống phân loại của hệ thống, Backend chỉ cung cấp API để lấy danh sách (Read), hoàn toàn thiếu các API để tạo mới, chỉnh sửa hoặc xóa danh mục từ phía Admin.
* **Hậu quả:** Admin không thể mở rộng hoặc chỉnh sửa danh mục kỹ năng/ngành nghề khi có nhu cầu phát sinh thực tế, phải can thiệp thủ công vào DB.
* **Cách khắc phục:** Bổ sung đầy đủ các endpoint CRUD cho `SkillCategory` và `JobCategory`.

### 3.3. Thiếu Rate Limiting cho các Endpoint Quản trị
* **Hiện trạng:** Các tác vụ thay đổi dữ liệu master data (POST, PUT, DELETE, PATCH) của Admin/Staff không áp dụng bất kỳ cơ chế giới hạn lượt gọi (Rate Limiting) nào.
* **Hậu quả:** Hệ thống có nguy cơ bị spam tạo rác dữ liệu master data thông qua các script tự động nếu tài khoản admin bị lộ lọt token hoặc bị tấn công CSRF.
* **Cách khắc phục:** Áp dụng Rate Limiting tối thiểu cho các phương thức Mutate dữ liệu master data.

---

## 4. Tầng Frontend (Next.js & UI Components)

### 4.1. Lỗi Nghiêm trọng: Vô hiệu hóa tính năng Cảnh báo hủy kích hoạt kỹ năng (Force Deactivate)
* **Hiện trạng:** Tại tệp `master-data/page.tsx` (dòng 195), hàm `handleSkillStatusToggle` bắt lỗi và hiển thị hộp thoại xác nhận (Force Status) bằng cách so khớp chuỗi tiếng Việt thô:
  ```typescript
  const apiMessage = error.message || "";
  if (apiMessage.includes("đang được sử dụng")) {
    setPendingSkillToggle({ id, currentStatus, hasConfirmWarning: true, warningMessage: apiMessage });
  }
  ```
  Tuy nhiên, API Backend lại trả về chuỗi tiếng Anh (`"Skill is currently used..."`).
* **Hậu quả:** Điều kiện `apiMessage.includes("đang được sử dụng")` không bao giờ thỏa mãn (luôn trả về `false`). Admin khi hủy kích hoạt một kỹ năng đã được dùng sẽ bị báo lỗi đỏ thô thiển và hoàn toàn không thể chọn "Vẫn hủy kích hoạt" (Force Deactivate) được nữa. Chức năng này bị liệt hoàn toàn.
* **Cách khắc phục:** Sửa điều kiện so khớp thành chuỗi tiếng Anh tương ứng hoặc tốt nhất là dựa vào mã lỗi trả về (`error.code === 'SKILL_IN_USE'`).

### 4.2. Lỗi UI/UX: Giữ nguyên vị trí cuộn trang khi chuyển Tab (Tab Scroll Bug)
* **Hiện trạng:** Trang Master Data quản lý song song hai danh sách Skills và Majors bằng Tabs. Khi Admin cuộn trang xuống sâu ở danh sách Skills rồi chuyển sang tab Majors, vị trí cuộn trang của trình duyệt bị giữ nguyên ở tọa độ Y đó.
* **Hậu quả:** Nếu danh sách Majors ngắn hơn, giao diện sẽ hiển thị một khoảng trắng trống rỗng ở phía dưới, khiến Admin hoang mang tưởng hệ thống bị lỗi dữ liệu cho đến khi họ tự cuộn chuột ngược lên đỉnh.
* **Cách khắc phục:** Bổ sung sự kiện bắt thay đổi tab để cuộn view của khung chứa về đỉnh (`window.scrollTo(0, 0)` hoặc phần tử cha chứa bảng) khi chuyển đổi tab.

### 4.3. Trộn lẫn Ngôn ngữ hiển thị (Anh - Việt)
* **Hiện trạng:** Trong giao diện quản lý Majors, nút khôi phục chuyên ngành đã xóa bị hardcode nhãn hiển thị là `"Restore"` (tiếng Anh), trong khi các nút thao tác khác kế bên lại hiển thị là `"Sửa"`, `"Xóa"`, `"Thêm mới"`.
* **Hậu quả:** Gây trải nghiệm người dùng thiếu chuyên nghiệp và không đồng nhất.
* **Cách khắc phục:** Việt hóa nhãn hiển thị thành `"Khôi phục"` hoặc áp dụng hệ thống Localization i18n cho toàn bộ giao diện quản trị.

### 4.4. Thiếu Optimistic Updates cho Thao tác khôi phục ngành (Restore Major)
* **Hiện trạng:** Khi thực hiện khôi phục một chuyên ngành đã xóa (Restore), giao diện không áp dụng cơ chế cập nhật tạm thời (Optimistic Updates) mà phải đợi API phản hồi thành công mới cập nhật danh sách. Trong khi đó, tính năng bật/tắt trạng thái kỹ năng lại có áp dụng.
* **Hậu quả:** Trải nghiệm thao tác khôi phục bị giật cục, nút bấm đơ từ 1-2 giây tùy thuộc vào tốc độ mạng, làm giảm cảm giác mượt mà của ứng dụng cao cấp.
* **Cách khắc phục:** Cấu hình TanStack Query trong hook `useRestoreMajor` để thực hiện optimistic update tương tự như logic của toggle status.
