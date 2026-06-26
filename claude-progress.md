# Nhật ký Tiến độ — ITHunterview

## Trạng thái Đã Xác minh Mới nhất

**Ngày**: 2026-06-26  
**Trạng thái baseline**:
- Backend: `dotnet build ITHunterview_V1.slnx` → ✅ PASS (2026-06-26)
- Frontend: `npm run build` → ✅ PASS (2026-06-26)

---

## Đã DONE (6 tính năng)

- **feat-001** Authentication Backend — JWT, Brute-force, GoogleAuth
- **feat-002** Master Data — Skills & Majors CRUD với soft-delete, cascade warning
- **feat-003** Admin User Governance — Accounts, ban/activate, auth guard AppShell
- **feat-004** Audit Logs — AuditLogInterceptor, immutability trigger
- **feat-005** Subscription Config Admin — JSONB features_config
- **feat-006** Coin Pay-as-you-go — Row-level Lock FOR UPDATE

## Lỗi chưa fix (HIGH)

| Flaw | File | Mô tả ngắn |
|---|---|---|
| `google_auth_email_verification_bypass` | AuthUseCase.cs | GoogleAuth bypass email verify |
| `google_auth_missing_profile_check` | AuthUseCase.cs | Không auto-tạo profile nếu thiếu |
| `frontend_login_violates_flow_constraint` | login/page.tsx | Gọi thẳng authService thay vì hook |
| `frontend_unimplemented_auth_pages` | (auth)/* | Register/Forgot/Reset/Verify chưa có |
| `entity_relationship_navigation_property_missing` | JobPostings, InterviewSessions | Thiếu nav properties cho EF Include |
| `entity_sql_data_type_mismatch_nullable` | JobPostings.cs | recruiter_id non-nullable vs DB NULL |

---

## Bước tiếp theo (Phiên kế tiếp BẮT ĐẦU TỪ ĐÂY)

### Tính năng: `feat-007` — Auth Pages Frontend

**Phạm vi:**
1. Sửa `(auth)/login/page.tsx` — refactor qua `useLogin` hook, sửa links, thêm loading
2. Tạo `(auth)/register/page.tsx` — Candidate + Recruiter register
3. Tạo `(auth)/forgot-password/page.tsx`
4. Tạo `(auth)/reset-password/page.tsx`
5. Tạo `(auth)/verify-email/page.tsx`

**Đọc trước:**
- `frontend/kinh-mantra.md` + `.agents/Frontend/DESIGN.md`
- `frontend/src/store/auth.store.ts`
- `frontend/src/hooks/useAuth.ts` (nếu tồn tại, nếu không → tạo mới)

**Xác minh:** `cd frontend && npm run build` → không lỗi

---

## Lịch sử Phiên

### Phiên Dịch Phân hệ User Governance sang Tiếng Anh (2026-06-26)
- Chuyển đổi toàn bộ thông báo xác thực (Validation Messages) trong `CreateStaffDto.cs` và `UpdateUserStatusDto.cs` sang tiếng Anh.
- Chuyển đổi các thông báo lỗi và chuỗi ghi log chặn truy cập (`[ACCESS BLOCKED]`) trong `UserStatusCheckMiddleware.cs` sang tiếng Anh.
- Chuyển đổi thông báo model validation lỗi trong `UserGovernanceController.cs` sang tiếng Anh.
- Cập nhật dependency_map trong `system_registry.json`.
- Biên dịch Backend (`dotnet build`) và Frontend (`npm run build`) thành công 100%.

### Phiên Khôi phục User Governance (2026-06-26T01:50Z)
- Thực hiện rà soát và khôi phục lại toàn bộ code bị lỗi/mất do thao tác sai của người dùng.
- Khôi phục lại: loại bỏ UpdateUserRole (Backend/Frontend), thêm RateLimiter cho CreateStaff, cập nhật Regex mật khẩu, sửa lỗi claim Role trùng lặp trong JWT, và cơ chế Authorization Guard trong AppShell.
- Chạy xác minh `dotnet build` and `npm run build` thành công.

### Phiên Khắc phục Lỗi Logic Nghiệp vụ User Governance (2026-06-26)
- Loại bỏ API UpdateUserRole vi phạm quy tắc nghiệp vụ ở cả Backend và Frontend.
- Đồng bộ hóa logic và UI trạng thái PENDING_VERIFICATION cho tài khoản nhà tuyển dụng qua API UpdateUserStatus.
- Chạy build thành công toàn bộ Backend và Frontend.

### Phiên Harness Setup (2026-06-26)
- Tạo AGENTS.md (root), feature_list.json, claude-progress.md, init.ps1, action_history.json
- Cập nhật frontend/AGENTS.md, system_registry.json

### Tiến độ hiện tại

- **Đã hoàn thành Phase 4 - Bảo mật (Security Issues) & Bản dịch Tiếng Anh** thuộc User Governance:
  - Cập nhật Regex bảo mật cho Email và Password trong `CreateStaffDto.cs`.
  - Thêm Rate Limiter (5 requests/minute) cho endpoint `CreateStaff` trong `Program.cs` và `UserGovernanceController.cs`.
  - Củng cố UseCase với Defense-in-depth: kiểm tra `actorRole == "admin"` trực tiếp trong `UserGovernanceUseCase.cs` cho tác vụ cập nhật User Status.
  - Cập nhật AppShell frontend để thực hiện redirect tức thì khi không có quyền, thay vì render Access Denied component (ngăn rò rỉ dữ liệu UI).
  - Chuyển dịch toàn bộ thông báo lỗi xác thực và nhật ký chặn truy cập sang tiếng Anh nhằm đồng bộ với FE.
  - Cả Backend và Frontend đều đã pass build verification.
  - Cập nhật ngôn ngữ sang tiếng Anh cho các thông báo lỗi liên quan tới quản lý nhân viên / tài khoản.
  - Đã ghi nhận Technical Debt `missing_localization_configuration` vào `system_registry.json` để thực hiện cấu hình i18n/Localization bài bản trong tương lai.
- **Đã hoàn thành Phase 5 - Frontend UI/UX Issues** thuộc User Governance:
  - FE-1: Xoá modal custom inline trong `[userId]/page.tsx`, thay bằng component `UpdateStatusModal` chuẩn.
  - FE-2: Fix logic render Toast icon (dùng `XCircle` cho lỗi, `AlertTriangle` cho cảnh báo, thay vì hardcode `CheckCircle`).
  - FE-3: Fix cú pháp `DialogTrigger` trong `create-staff-modal.tsx` (sử dụng `@base-ui` chuẩn với `render={children}`).
  - FE-4: Thêm tính năng phân trang thông minh (smart pagination truncation) trong `page.tsx` cho danh sách accounts.
  - Xác minh thành công Frontend `npm run build`.
- **Tồn đọng (Pending)**: SEC-4 (Force Change Password) đã được ghi nhận trong audit và để lại cho phase cải tiến sau theo yêu cầu.

### Phiên Rà soát Phân hệ Master Data (2026-06-26)
- Thực hiện rà soát sâu toàn bộ mã nguồn của Master Data (Chuyên ngành & Kỹ năng) từ Database, Backend Service, Web API cho đến Frontend UI.
- Ghi nhận 14 khuyết điểm quan trọng liên quan đến tính nhất quán dữ liệu, lỗi logic Force Deactivate (do lệch ngôn ngữ thông báo), điểm nghẽn hiệu năng khi tải danh mục vào RAM, và sự thiếu hụt CRUD danh mục.
- Lưu trữ toàn bộ phân tích chi tiết vào tệp `master_data_flaws.md` ở thư mục gốc của dự án.
- Đăng ký các mã lỗi kỹ thuật này vào danh sách `identified_flaws` trong `system_registry.json`.

## Bước tiếp theo

1. Tiến hành nâng cấp Frontend của Master Data (phần Majors) để hỗ trợ cấu trúc cây phân cấp (tree list/nesting UI), hiển thị cấp độ của ngành, cho phép tạo ngành con (ParentId) và hiển thị cảnh báo chặn xoá (Restrict Delete) thích hợp.
2. Hoặc bắt đầu triển khai tính năng `feat-007` — Auth Pages Frontend nếu có yêu cầu.

### Phiên Nâng cấp Phân cấp Cành cây Majors Backend (2026-06-26)
- Đổi cấu trúc bảng `Majors` thành cấu trúc phân cấp cây bằng cách thêm cột `ParentId` (tự tham chiếu).
- Cập nhật database schema (`schema.sql` và migration logic qua EF Core).
- Cài đặt giới hạn độ sâu (Max Depth = 3) khi tạo/cập nhật ngành học, ngăn chặn vòng lặp tuần hoàn.
- Cài đặt cơ chế Xoá ràng buộc (Restrict Delete): Không cho phép xoá ngành cha nếu có ngành con đang hoạt động (Active).
- Cập nhật `IMajorRepository`, `MajorRepository`, `IMajorUseCase`, `MajorUseCase`, và `MasterDataController` tương ứng để trả về cấu trúc phân cấp cây và validate nghiệp vụ.
- Đã chạy xác minh `dotnet build` thành công 100%.

*Agent: cập nhật section "Bước tiếp theo" mỗi khi kết thúc phiên.*
