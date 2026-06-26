# BẢN ĐĂNG KÝ HỆ THỐNG THAY ĐỔI FRONTEND (FRONTEND CHANGES REGISTRY)
*Tài liệu này ghi lại toàn bộ thay đổi cấu trúc mã nguồn Frontend nhằm truy vết ảnh hưởng đồng bộ khi nâng cấp.*

---

## 1. Phân Hệ: Platform Safety & Audit Logs Frontend
*Ngày cập nhật: 20-06-2026*

### Các Tệp Thay Đổi & Sự Ảnh Hưởng Liên Đới:

#### A. Routing & Navigation
1. **[constants.ts](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/lib/constants.ts)**
   - *Thay đổi:* Bổ sung định nghĩa route `AUDIT_LOGS` cho cả phân hệ `STAFF` và `ADMIN`. Thêm cấu hình mục menu `Platform Safety` trong danh sách menu của Admin.
   - *Ảnh hưởng:* Định vị URL liên kết cho toàn bộ ứng dụng. Bất kỳ sự thay đổi nào về route ở phía Backend hay Frontend Middleware cần cập nhật đồng bộ ở đây.
2. **[Sidebar.tsx](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/components/layout/Sidebar.tsx)**
   - *Thay đổi:* Thêm mục menu `"Platform Safety"` (dẫn tới route admin audit logs) và `"Audit Logs"` (cho staff) với các icon tương ứng (`Shield`, `ClipboardList`).
   - *Ảnh hưởng:* Thay đổi cấu trúc hiển thị thanh điều hướng bên trái (Sidebar). Cần đảm bảo icon sử dụng (`lucide-react`) được cấu hình hiển thị đúng động trong component Sidebar.

#### B. API Services Layer (Giao tiếp HTTP)
3. **[audit-log.service.ts](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/services/audit-log.service.ts)**
   - *Thay đổi:* Tạo mới dịch vụ gọi API Backend bao gồm lấy danh sách log có phân trang, bộ lọc (`getAuditLogs`) và xóa log (`purgeAuditLogs`).
   - *Ảnh hưởng:* Kết nối trực tiếp với backend `UserGovernanceController`. Thay đổi cấu trúc URL endpoint `/api/v1/audit-logs` hoặc tham số lọc ở backend sẽ gây lỗi gọi API ở frontend.

#### C. React Query Hooks Layer (Quản lý State Server)
4. **[useAuditLogs.ts](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/hooks/useAuditLogs.ts)**
   - *Thay đổi:* Tạo các hook custom `useAuditLogs` (truy vấn danh sách logs) và `usePurgeAuditLogs` (mutation để xóa log).
   - *Ảnh hưởng:* Quản lý vòng đời cache dữ liệu log thông qua react-query. Việc thay đổi key cache (`['audit-logs', params]`) sẽ ảnh hưởng đến việc invalidate queries khi thực hiện purge dữ liệu.

#### D. UI Components & Pages (Giao diện)
5. **[page.tsx](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/app/(admin)/admin/audit-logs/page.tsx)**
   - *Thay đổi:* Tái cấu trúc hoàn toàn trang giao diện Audit Logs sử dụng shadcn/ui components (`Button`, `Input`, `Label`), loại bỏ shadow/glassmorphism, chuyển các phần logic modal và so sánh Diff sang các component con độc lập.
   - *Ảnh hưởng:* Đầu mối điều phối trạng thái (bộ lọc, phân trang, fetch dữ liệu) của trang Audit Logs.
6. **[snapshot-diff.tsx](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/app/(admin)/admin/audit-logs/components/snapshot-diff.tsx)**
   - *Thay đổi:* Tạo mới component độc lập render cấu trúc Diff JSON của database state dưới dạng bảng hoặc pre-formatted text an toàn.
   - *Ảnh hưởng:* Chỉ hiển thị Diff trong modal chi tiết, bất kỳ thay đổi định dạng diff nào ở backend cần được parse/render tương thích ở đây.
7. **[log-details-modal.tsx](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/app/(admin)/admin/audit-logs/components/log-details-modal.tsx)**
   - *Thay đổi:* Tạo mới component modal hiển thị chi tiết log kiểm toán, loại bỏ shadow/glassmorphism, dùng overlay đơn giản và các badges có độ tương phản dịu nhẹ.
   - *Ảnh hưởng:* Dùng để xem chi tiết log khi người dùng click vào nút Xem chi tiết trên bảng logs.
8. **[purge-modal.tsx](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/app/(admin)/admin/audit-logs/components/purge-modal.tsx)**
   - *Thay đổi:* Tạo mới component modal xác nhận dọn dẹp logs cũ hơn N ngày với cảnh báo nguy hiểm và input xác thực số ngày.
   - *Ảnh hưởng:* Dùng để xác nhận hành động xoá logs định kỳ của quản trị viên.

---

## 2. Hướng dẫn Đồng bộ khi chỉnh sửa (Sync Guideline)
- **Khi thay đổi API Response ở Backend:** Cập nhật ngay kiểu dữ liệu `AuditLogDto` tại `types/audit-log.types.ts` để đồng bộ kiểu dữ liệu với Frontend service.
- **Khi thay đổi cấu trúc Sidebar/Icon:** Đảm bảo các icon sử dụng trong `constants.ts` (ví dụ `Shield`) đã được định nghĩa và map chính xác trong helper render icon của `Sidebar.tsx`.

---

## 3. Phân Hệ: Audit Log Usability & Synchronization (Đồng bộ bộ lọc và hiển thị Nhật ký)
*Ngày cập nhật: 23-06-2026*

### Các Tệp Thay Đổi & Sự Ảnh Hưởng Liên Đới:

#### A. UI Components & Pages (Giao diện Admin & Staff)
1. **[page.tsx (Admin)](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/app/%28admin%29/admin/audit-logs/page.tsx)**
   - *Thay đổi:*
     - Loại bỏ ký tự `'Z'` khỏi chuỗi Date ISO để ép DatePicker parse ngày theo múi giờ local của máy trạm thay vì ép buộc UTC trực tiếp (khắc phục lệch 7 tiếng ở GMT+7 gây lùi ngày).
     - Thay đổi giá trị tùy chọn trong Dropdown lọc danh mục từ `'ACCESS'` thành `'AUTH'` cho đồng bộ với định nghĩa thực tế trong DB ở backend, triệt tiêu lỗi HTTP 400 Bad Request.
     - Hàm `getCategoryBadgeColor` bổ sung xử lý màu sắc badge hiển thị cho các danh mục `'AUTH'` và `'SYSTEM'`.
   - *Ảnh hưởng:* Đảm bảo tính nhất quán của bộ lọc ngày và danh mục hoạt động chính xác không gây crash hay lỗi API 400.
2. **[page.tsx (Staff)](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/app/%28staff%29/staff/audit-logs/page.tsx)**
   - *Thay đổi:* Hàm `getCategoryBadgeColor` bổ sung xử lý hiển thị badge màu sắc cho các danh mục `'AUTH'` và `'SYSTEM'` thay cho `'ACCESS'`.
   - *Ảnh hưởng:* Đảm bảo giao diện quản lý log của Staff hiển thị badge đẹp mắt, đồng bộ thẩm mỹ với trang Admin.

---

## 4. Phân Hệ: User Governance UI Cleanup & Protection
*Ngày cập nhật: 24-06-2026*

### Các Tệp Thay Đổi & Sự Ảnh Hưởng Liên Đới:

#### A. UI Components & Pages (Giao diện)
1. **[page.tsx (Accounts)](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/app/(admin)/admin/accounts/page.tsx)**
   - *Thay đổi:*
     - Ẩn nút thay đổi "Trạng thái" đối với các tài khoản có vai trò `admin`.
     - Loại bỏ hoàn toàn nút "Quyền" và mã nguồn liên kết đến Modal/Dialog cập nhật vai trò (`UPDATE ROLE DIALOG`).
     - Dọn dẹp các hook (`useUpdateUserRole`), các state liên kết (`isRoleModalOpen`, `roleTargetUser`, `newRole`, `roleError`) và handler (`handleRoleSubmit`) không sử dụng.
   - *Ảnh hưởng:* Loại bỏ hoàn toàn tính năng phân quyền ở giao diện, ngăn ngừa lỗi thao tác vô hiệu hóa tài khoản Admin và làm gọn mã nguồn trang quản trị.
2. **[page.tsx (Account Detail)](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/app/(admin)/admin/accounts/[userId]/page.tsx)**
   - *Thay đổi:*
     - Ẩn khối chức năng thay đổi "Trạng thái" (Quick Actions) đối với các tài khoản có vai trò `admin` (kiểm tra qua `roleName` và `roleId`).
     - Loại bỏ hoàn toàn nút "Phân quyền tài khoản", Dialog cập nhật vai trò (`UPDATE ROLE DIALOG`) cùng toàn bộ states, logic và hook liên kết (`useUpdateUserRole`).
   - *Ảnh hưởng:* Đảm bảo đồng bộ hóa bảo mật, chặn đứng hoàn toàn khả năng sửa đổi thông tin quyền hạn và trạng thái của tài khoản Admin từ cấp độ chi tiết hồ sơ.

---

## 5. Phân Hệ: Localization & English Translation of Admin Portal
*Ngày cập nhật: 24-06-2026*

### Các Tệp Thay Đổi & Sự Ảnh Hưởng Liên Đới:

#### A. UI Components & Pages (Giao diện Admin & Layout)
1. **[page.tsx (Subscriptions)](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/app/%28admin%29/admin/subscriptions/page.tsx)**
   - *Thay đổi:* Dịch thuật toàn bộ tiêu đề, bộ lọc, bảng dữ liệu, toast thông báo, modal trigger và phân trang sang tiếng Anh.
   - *Ảnh hưởng:* Giao diện quản lý gói dịch vụ hiển thị chuẩn hóa tiếng Anh 100% cho quản trị viên.
2. **[SubscriptionForm.tsx](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/app/%28admin%29/admin/subscriptions/components/SubscriptionForm.tsx)**
   - *Thay đổi:* Dịch thuật validation schema, nhãn nhập liệu, placeholders và nút hành động sang tiếng Anh.
   - *Ảnh hưởng:* Đảm bảo form tạo mới/cập nhật gói cước hiển thị tiếng Anh và các thông báo validate dạng toast thân thiện.
3. **[CoinConfigTab.tsx](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/app/%28admin%29/admin/subscriptions/components/CoinConfigTab.tsx)**
   - *Thay đổi:* Dịch thuật toàn bộ bảng mệnh giá coin, cấu hình tỷ giá, chi phí AI và các thông báo validate liên quan sang tiếng Anh.
   - *Ảnh hưởng:* Đồng bộ hóa ngôn ngữ giao diện cấu hình ví và coin của hệ thống.
4. **[AppShell.tsx](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/components/layout/AppShell.tsx)**
   - *Thay đổi:* Dịch thông điệp màn hình chờ khi tải dữ liệu từ `"Đang tải..."` sang `"Loading..."`.
   - *Ảnh hưởng:* Đảm bảo đồng bộ hóa ngôn ngữ ngay cả trong trạng thái tải trang ở toàn bộ các module.
5. **[page.tsx (Master Data)](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/app/%28admin%29/admin/master-data/page.tsx)**
   - *Thay đổi:* Dịch thuật toàn bộ giao diện quản lý dữ liệu danh mục (Quốc gia, Kỹ năng, Ngành nghề, Cấp bậc, v.v.), bộ lọc, các thông báo phản hồi sang tiếng Anh.
   - *Ảnh hưởng:* Chú ý giữ nguyên các chuỗi kiểm tra lỗi trả về từ API backend bằng tiếng Việt (ví dụ: các thông báo lỗi liên quan đến ràng buộc khóa ngoại hoặc trạng thái sử dụng) để không làm sai lệch logic so sánh ở frontend.

---

## 6. Phân Hệ: Staff Account Creation & UI Improvements (Tính năng Tạo tài khoản Staff cho Admin)
*Ngày cập nhật: 24-06-2026*

### Các Tệp Thay Đổi & Sự Ảnh Hưởng Liên Đới:

#### A. API Types & Service Layer
1. **[user-governance.types.ts](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/types/user-governance.types.ts)**
   - *Thay đổi:* Khai báo interface DTO `CreateStaffDto` gồm email và password.
   - *Ảnh hưởng:* Đồng bộ kiểu dữ liệu truyền vào của service API và hook mutation.
2. **[user-governance.service.ts](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/services/user-governance.service.ts)**
   - *Thay đổi:* Thêm hàm gọi api `createStaff` gửi request POST `/api/user-governance/staff`.
   - *Ảnh hưởng:* Giao diện truyền dẫn HTTP kết nối frontend tới endpoint backend.

#### B. React Query Hooks Layer
3. **[useUserGovernance.ts](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/hooks/useUserGovernance.ts)**
   - *Thay đổi:* Thêm hook `useCreateStaff` sử dụng `useMutation` để kích hoạt API tạo staff và invalidate query danh sách `users` khi thành công.
   - *Ảnh hưởng:* Quản lý chu kỳ làm mới dữ liệu, giúp giao diện tự động cập nhật danh sách tài khoản ngay sau khi tạo thành công.

#### C. UI Components & Pages (Giao diện)
4. **[page.tsx (Accounts List)](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/app/%28admin%29/admin/accounts/page.tsx)**
   - *Thay đổi:*
     - Nhập khẩu icon `UserPlus` và mutation hook `useCreateStaff`.
     - Tích hợp nút bấm "Create Staff Account" ở góc trên bên phải của phần Header.
     - Triển khai modal form `CreateStaffModal` với cơ chế validation client-side, hiển thị lỗi phản hồi từ API backend, và hiển thị thông báo toast thành công.
     - Nâng cấp hiển thị tên Staff: Nếu thuộc tính `fullName` rỗng, hiển thị trực tiếp `email` của Staff thay vì chuỗi "Not updated".
   - *Ảnh hưởng:* Tích hợp toàn diện quy trình tạo tài khoản và quản lý danh sách ở frontend.
5. **[page.tsx (Account Detail)](file:///d:/Desktop/FU/do%20an/Capstone%20Project/frontend/src/app/%28admin%29/admin/accounts/%5BuserId%5D/page.tsx)**
   - *Thay đổi:*
     - Thay thế card thông báo "No personal profile" đơn giản bằng một Profile Card cao cấp được tùy chỉnh riêng cho Admin và Staff.
     - Hiển thị Email làm tên chính trên header của Profile card dành riêng cho tài khoản Staff.
   - *Ảnh hưởng:* Cải tiến trải nghiệm trực quan (UX) premium của hệ thống quản trị, nhất quán hóa định dạng dữ liệu họ tên trống của nhân viên.


