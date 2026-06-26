# Danh Sách Các Khuyết Điểm Và Vấn Đề Tồn Đọng Trong Hệ Thống (ITHunterview)

Dưới đây là danh sách toàn bộ các khuyết điểm, lỗi bảo mật, lỗi thiết kế luồng dữ liệu và vấn đề trải nghiệm người dùng (UX) được phát hiện trong hệ thống. Tài liệu này được sử dụng để theo dõi, đánh giá mức độ ảnh hưởng và lên kế hoạch sửa đổi đồng bộ, tránh sửa cái này hỏng cái khác.

---

## 1. Các Khuyết Điểm Mức Độ Nghiêm Trọng (CRITICAL / HIGH)

### 1.1. Bỏ qua cơ chế bảo vệ quyền truy cập ở Frontend (`frontend_auth_guard_bypass`)
- **Tệp tin ảnh hưởng**: `frontend/src/store/auth.store.ts` và các route layouts.
- **Mô tả**: `AccessToken` và thông tin `User` được đọc và tin cậy trực tiếp từ `localStorage` ở Client mà không có bước kiểm tra tính hợp lệ (Signature check) hay gọi API xác thực lại (`/api/me`) ở lần tải đầu tiên.
- **Tác hại**: Người dùng có thể chỉnh sửa thủ công dữ liệu giả lập trong `localStorage` để đánh lừa Client-state rằng họ đã đăng nhập với vai trò Admin/Recruiter nhằm hiển thị giao diện ẩn. Mặc dù các API backend vẫn chặn, khuyết điểm này tạo ra trải nghiệm bảo mật giả và nguy cơ hiển thị thông tin tĩnh không được bảo vệ.

### 1.2. Bỏ qua xác thực Email đối với tài khoản Google (`google_auth_email_verification_bypass`)
- **Tệp tin ảnh hưởng**: `backend/ITHunterview.Service/UseCase/AuthUseCase.cs`
- **Mô tả**: Người dùng đăng nhập qua Google Auth được tự động kích hoạt tài khoản trực tiếp ngay cả khi tài khoản đăng ký bằng email thông thường trước đó đang ở trạng thái chờ kích hoạt email (`PENDING_VERIFICATION`).
- **Tác hại**: Bỏ qua quy trình xác thực email bắt buộc của hệ thống, tạo cơ hội cho việc đăng nhập trái phép nếu email chưa thực sự được xác thực quyền sở hữu ban đầu.

### 1.3. Thiếu giao diện cho hầu hết các trang xác thực (`frontend_unimplemented_auth_pages`)
- **Tệp tin ảnh hưởng**: `frontend/src/app/(auth)/*`
- **Mô tả**: Ngoài trang Đăng nhập (`login/page.tsx`), các trang quan trọng khác như Đăng ký tài khoản, Quên mật khẩu, Đặt lại mật khẩu, và Xác thực Email chưa hề được xây dựng giao diện thực tế (chỉ có tệp tin giữ chỗ `.gitkeep`).
- **Tác hại**: Người dùng không thể thực hiện quy trình đăng ký hay khôi phục mật khẩu thông thường.

### 1.4. Audit Log bị Rollback khi Transaction Nghiệp Vụ Lỗi (`audit_log_rollback_on_business_failure`)
- **Tệp tin ảnh hưởng**: `backend/ITHunterview.Service/Infrastructure/Persistence/AuditLogInterceptor.cs`
- **Mô tả**: Do `AuditLogInterceptor` ghi nhận log đột biến dữ liệu trực tiếp vào DbContext hiện hành của nghiệp vụ chính và chạy cùng Transaction, nên khi nghiệp vụ chính bị lỗi (ví dụ: validate DB thất bại, lỗi khóa ngoại, v.v.), toàn bộ Transaction bao gồm cả bản ghi Audit Log sẽ bị rollback.
- **Tác hại**: Hệ thống hoàn toàn không ghi nhận được các hành vi cố tình thực hiện thao tác sai hoặc các cuộc tấn công bảo mật cố ý làm lỗi dữ liệu nhằm phá hoại. Đây là một lỗ hổng kiểm toán nghiêm trọng.

### 1.5. Ảnh hưởng hiệu năng nghiêm trọng do Serialize đồng bộ (`audit_log_sync_serialization_overhead`)
- **Tệp tin ảnh hưởng**: `backend/ITHunterview.Service/Infrastructure/Persistence/AuditLogInterceptor.cs`
- **Mô tả**: Việc tính toán Snapshot Diff và serialize sang chuỗi JSON được thực hiện đồng bộ trực tiếp trong luồng `SavingChanges/SavingChangesAsync` của DbContext nghiệp vụ.
- **Tác hại**: Gây nghẽn CPU và làm tăng đáng kể Response Time (độ trễ phản hồi) của mọi API có hành động thay đổi dữ liệu, đặc biệt khi thực hiện bulk mutations (tạo/sửa hàng loạt bản ghi). Lẽ ra việc này phải chạy bất đồng bộ (Outbox pattern).

---

## 2. Các Khuyết Điểm Mức Độ Trung Bình (MEDIUM)

### 2.1. Đăng nhập vi phạm ràng buộc luồng dữ liệu (`frontend_login_violates_flow_constraint`)
- **Tệp tin ảnh hưởng**: `frontend/src/app/(auth)/login/page.tsx`
- **Mô tả**: Trang `LoginPage` đang gọi trực tiếp `authService` và quản lý các trạng thái `loading`, `error` cục bộ bằng `useState` thay vì sử dụng hook `useLogin` đã được định nghĩa trong `hooks/useAuth.ts`.
- **Tác hại**: Vi phạm quy tắc luồng dữ liệu chuẩn đã đặt ra ở `kinh-mantra.md` (`page.tsx` -> `hook` -> `service`). Khó quản lý trạng thái đồng bộ nếu cần cập nhật lại thông tin user ở các component khác.

### 2.2. Sai đường dẫn liên kết xác thực (`frontend_auth_broken_links`)
- **Tệp tin ảnh hưởng**: `frontend/src/app/(auth)/login/page.tsx`
- **Mô tả**: Các đường dẫn chuyển hướng sang Đăng ký và Quên mật khẩu đang để cứng là `/auth/register` và `/auth/forgot-password`.
- **Tác hại**: Trong Next.js App Router, do sử dụng Route Groups `(auth)`, các đường dẫn đúng phải là `/register` và `/forgot-password`. Việc dùng sai đường dẫn này sẽ dẫn người dùng đến trang lỗi 404.

### 2.3. Thiếu cơ chế xử lý lỗi khi mất kết nối mạng (`offline_ux_missing`)
- **Tệp tin ảnh hưởng**: Toàn bộ hệ thống hooks của frontend (`frontend/src/hooks/*`)
- **Mô tả**: Các hành động Mutation (Thêm mới, cập nhật, xóa) chưa có cơ chế tự động thử lại khi mất mạng tạm thời hoặc khi server không phản hồi. Giao diện chỉ hiển thị thông báo lỗi thô sơ qua toast.
- **Tác hại**: Trải nghiệm người dùng kém khi kết nối mạng không ổn định.

### 2.4. Google Auth thiếu bước kiểm tra hồ sơ người dùng (`google_auth_missing_profile_check`)
- **Tệp tin ảnh hưởng**: `backend/ITHunterview.Service/UseCase/AuthUseCase.cs`
- **Mô tả**: Trong phương thức `GoogleAuthAsync`, hệ thống không kiểm tra xem tài khoản đã có hồ sơ tương ứng chưa (Ví dụ: dữ liệu cũ bị lỗi thiếu bản ghi `CandidateProfiles` hoặc `RecruiterProfiles`).
- **Tác hại**: Gây ra lỗi logic không mong muốn (NullReferenceException) ở các API nghiệp vụ tiếp theo khi truy vấn hồ sơ liên kết.

### 2.5. Thu thập IP Client không chính xác khi chạy qua Reverse Proxy (`governance_proxy_ip_spoofing`)
- **Tệp tin ảnh hưởng**: `backend/ITHunterview.WebAPI/Controllers/UserGovernanceController.cs`
- **Mô tả**: Việc lấy IP Client bằng `HttpContext.Connection.RemoteIpAddress?.ToString()` khi ứng dụng chạy sau một Reverse Proxy (như Nginx, Cloudflare) sẽ chỉ trả về IP của Proxy thay vì IP thực tế của người dùng.
- **Tác hại**: Làm giảm giá trị bảo mật và độ tin cậy của Audit Trail (Nhật ký hoạt động hệ thống), không thể truy vết được nguồn gốc thực tế của các thao tác quản trị.

### 2.6. Hoàn toàn thiếu hụt các Unit Tests kiểm thử tự động (`governance_missing_unit_tests`)
- **Tệp tin ảnh hưởng**: Toàn bộ dự án test `ITHunterview.Service.Tests` và `ITHunterview.Domain.Tests`.
- **Mô tả**: Các dự án Unit Test chỉ là thư mục chứa file `README.md` rỗng, không hề được liên kết trong tập tin giải pháp `.slnx` và không có bất kỳ kịch bản kiểm thử tự động nào cho các quy tắc bảo mật cốt lõi (chặn tự khóa, chặn staff lạm quyền).
- **Tác hại**: Dễ xảy ra lỗi logic (regression) khi mã nguồn UseCase được nâng cấp hoặc tối ưu hóa trong tương lai mà không được kiểm chứng tính đúng đắn tự động.

### 2.7. Rủi ro rò rỉ dữ liệu nhạy cảm do hardcode danh sách lọc (`audit_log_sensitive_fields_hardcoded`)
- **Tệp tin ảnh hưởng**: `backend/ITHunterview.Service/Infrastructure/Persistence/AuditLogInterceptor.cs`
- **Mô tả**: Các thuộc tính nhạy cảm bị loại trừ khỏi log (như `PasswordHash`, `Token`, v.v.) đang được định nghĩa bằng một mảng hardcode tĩnh cố định (`SensitiveFields`).
- **Tác hại**: Khi dự án mở rộng có các thực thể mới với trường dữ liệu nhạy cảm khác (ví dụ: `OtpCode`, `CreditCardNumber`, `PassportNumber`) mà lập trình viên quên cập nhật vào danh sách này, các thông tin nhạy cảm thô sẽ bị ghi rõ dưới dạng clear-text trong database log, vi phạm nghiêm trọng quy chuẩn an toàn thông tin.

### 2.8. Phụ thuộc cứng vào cú pháp PostgreSQL trong Repository (`audit_log_postgres_dependency`)
- **Tệp tin ảnh hưởng**: `backend/ITHunterview.Service/Infrastructure/Persistence/AuditLogRepository.cs`
- **Mô tả**: Phương thức dọn dẹp log `PurgeActivityLogsAsync` thực hiện bypass trigger chặn đột biến log của DB bằng cách chạy lệnh SQL thuần `SET LOCAL app.allow_audit_log_delete = 'true';` đặc trưng của PostgreSQL.
- **Tác hại**: Gây phụ thuộc chặt vào PostgreSQL. Khi chạy test tự động trên SQLite hoặc InMemory Database, hệ thống sẽ crash ngay lập tức, ngăn cản việc viết Unit Test/Integration Test cho luồng dọn dẹp log.

### 2.9. Tải Payload Diff Quá Lớn Khi Lấy Danh Sách Logs (`audit_log_frontend_overfetching`)
- **Tệp tin ảnh hưởng**: `frontend/src/services/audit-log.service.ts`, `frontend/src/app/(admin)/admin/audit-logs/page.tsx`
- **Mô tả**: API `GET /api/v1/audit-logs` trả về trực tiếp toàn bộ chuỗi JSON `SnapshotDiff` (thường có dung lượng rất lớn) của tất cả các bản ghi logs trong trang danh sách.
- **Tác hại**: Lãng phí băng thông mạng cực kỳ lớn. SnapshotDiff chỉ cần thiết khi admin thực sự nhấn xem chi tiết. Cần áp dụng cơ chế Lazy Loading (chỉ load diff khi bấm xem chi tiết từng log).

### 2.10. Thiếu bộ lọc nâng cao thân thiện và dropdown động trên giao diện (`audit_log_frontend_ux_limitations`)
- **Tệp tin ảnh hưởng**: `frontend/src/app/(admin)/admin/audit-logs/page.tsx`
- **Mô tả**: Trình hiển thị so sánh diff giả định dữ liệu snapshot là phẳng (flat key-value), không hỗ trợ nested objects (JSONB). Bộ lọc ngày sử dụng HTML input thô sơ không có các preset thông minh. Lọc theo Actor (UserId) yêu cầu nhập chuỗi Guid thủ công thay vì sử dụng dropdown autocomplete.
- **Tác hại**: Giảm sút nghiêm trọng trải nghiệm quản trị vận hành của nhân viên hệ thống.

---

## 3. Các Khuyết Điểm Mức Độ Thấp (LOW)

### 3.1. Lưu trữ JWT Secret & Google ClientId dạng văn bản thuần (`plain_text_client_id`)
- **Tệp tin ảnh hưởng**: `backend/ITHunterview.WebAPI/appsettings.json`
- **Mô tả**: Khóa bí mật JWT (`JwtSettings:Secret`) và Google ClientId được lưu trực tiếp dạng chuỗi text trong tệp cấu hình thay vì sử dụng biến môi trường (Environment Variables) hoặc công cụ quản lý khóa (User Secrets/Key Vault).
- **Tác hại**: Dễ bị lộ khóa nếu vô tình push tệp appsettings.json lên các kho lưu trữ mã nguồn mở công khai.

### 3.2. Cứng hóa ngôn ngữ trong các thông báo lỗi Validation (`hardcoded_validation_messages`)
- **Tệp tin ảnh hưởng**: `frontend/src/app/(admin)/admin/master-data/page.tsx`
- **Mô tả**: Các thông báo lỗi kiểm tra dữ liệu đầu vào (Ví dụ: "Tên kỹ năng không được để trống") đang viết cứng bằng Tiếng Việt trực tiếp trong code.
- **Tác hại**: Khó khăn trong việc mở rộng đa ngôn ngữ (i18n) cho trang web sau này.

### 3.3. Dùng lớp CSS không tồn tại ở trang Đăng nhập (`frontend_login_missing_styling_class`)
- **Tệp tin ảnh hưởng**: `frontend/src/app/(auth)/login/page.tsx`
- **Mô tả**: Nút đăng nhập đang áp dụng lớp class `btn-primary-white` không có thực trong `globals.css` (chỉ có lớp `btn-primary`).
- **Tác hại**: Làm nút đăng nhập mất kiểu dáng chuẩn và có thể hiển thị không đồng bộ với các phần tử giao diện khác.

### 3.4. Khai báo biến màu sắc nhưng không sử dụng ở Dashboard (`frontend_dashboard_unused_color_variable`)
- **Tệp tin ảnh hưởng**: `frontend/src/app/(admin)/admin/dashboard/page.tsx`
- **Mô tả**: Khai báo biến màu sắc `s.color` (`from-indigo-500 to-violet-600`) trong danh sách các thẻ thống kê nhưng không áp dụng vào thẻ JSX chứa biểu tượng.
- **Tác hại**: Làm mất đi hiệu ứng trực quan màu sắc của biểu tượng thống kê trên trang Dashboard của Admin.

### 3.5. Giao diện Master Data giữ nguyên vị trí cuộn khi chuyển tab (`master_data_tab_scroll_bug`)
- **Tệp tin ảnh hưởng**: `frontend/src/app/(admin)/admin/master-data/page.tsx`
- **Mô tả**: Khi cuộn trang xuống cuối để xem phân trang ở tab Skills, nếu chuyển sang tab Majors, vị trí cuộn của trang không tự động reset lên đầu. Do danh sách Majors ngắn, trang trông như trống hoặc bị lỗi giao diện cho đến khi tự cuộn lên.
- **Tác hại**: Gây bối rối cho người dùng và giảm sút trải nghiệm giao diện.

### 3.6. Hệ thống không ghi log hành động đăng nhập (`governance_missing_login_logs`)
- **Tệp tin ảnh hưởng**: `backend/ITHunterview.Service/UseCase/AuthUseCase.cs`
- **Mô tả**: Hệ thống không lưu lại lịch sử hoạt động đối với các thao tác đăng nhập hoặc xác thực thành công/thất bại của người dùng vào bảng `user_activity_logs`.
- **Tác hại**: Làm giảm tính toàn vẹn của Audit Trail, khó khăn trong việc phát hiện các cuộc tấn công brute force hoặc rà quét đăng nhập.

### 3.7. Dữ liệu seed môi trường phát triển quá ít (`dev_seed_data_insufficient`)
- **Tệp tin ảnh hưởng**: `backend/ITHunterview.Service/Infrastructure/Persistence/DataSeeder.cs`
- **Mô tả**: Số lượng Majors được seed quá ít (8 chuyên ngành), dưới ngưỡng phân trang mặc định (10); đồng thời hoàn toàn thiếu dữ liệu seed cho `UserActivityLogs`.
- **Tác hại**: Gây khó khăn trong việc kiểm thử chức năng phân trang ở các tab này trực quan trên UI.

### 3.8. Thanh phân trang của bảng tài khoản bị khuất khỏi khung nhìn (`accounts_pagination_visibility_ux`)
- **Tệp tin ảnh hưởng**: `frontend/src/app/(admin)/admin/accounts/page.tsx`
- **Mô tả**: Danh sách tài khoản dài làm thanh phân trang bị đẩy hoàn toàn xuống dưới và ẩn khỏi khung nhìn mặc định, yêu cầu người dùng phải cuộn chuột xuống cuối cùng.
- **Tác hại**: Giảm tính tiện dụng của tính năng phân trang, người dùng khó phát hiện có trang kế tiếp.

### 3.9. Trải nghiệm tải trang khi đăng nhập thiếu loading indicator (`login_loading_indicator_missing`)
- **Tệp tin ảnh hưởng**: `frontend/src/app/(auth)/login/page.tsx`
- **Mô tả**: Thời gian API phản hồi đăng nhập mất hơn 2 giây nhưng không có vòng xoay loading hoặc màn hình overlay che phủ, chỉ có văn bản nút đổi sang "Signing in...".
- **Tác hại**: Người dùng có thể tưởng trang web bị treo và click nút Sign In nhiều lần, gây ra các request trùng lặp.

### 3.10. Thiếu tệp cấu hình môi trường và thông báo lỗi mạng không rõ ràng (`frontend_missing_env_and_vague_error`)
- **Tệp tin ảnh hưởng**: `frontend/src/services/api-client.ts` và toàn bộ thư mục gốc frontend
- **Mô tả**: Dự án chưa có tệp `.env.example` làm mẫu cấu hình `NEXT_PUBLIC_API_URL`. Port backend đang bị thiết lập cứng trong file cấu hình Axios (bị lệch giữa 50504 và 51148). Khi xảy ra lỗi kết nối (`AxiosError: Network Error`), giao diện chưa hiển thị thông báo lỗi thân thiện để giải thích backend đang không khả dụng.
- **Tác hại**: Gây bối rối cho môi trường dev mới (như lỗi hiện tại), lỗi kết nối mạng chỉ ném ra lỗi chung chung gây cản trở trải nghiệm người dùng và quá trình debug.

