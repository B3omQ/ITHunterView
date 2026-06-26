# AGENTS.md — ITHunterview Capstone

Đọc file này đầu tiên mỗi phiên. Không làm gì trước khi đọc xong.

## Dự án

Nền tảng tuyển dụng IT (Next.js 16 + ASP.NET Core) tích hợp AI Mock Interview và CV-JD Matching.
Đặc tả nghiệp vụ đầy đủ: `business_logic_specification.md`

## Tech Stack

| Layer | Công nghệ |
|---|---|
| **Backend** | .NET 8 / ASP.NET Core Web API, EF Core, PostgreSQL 15 |
| **Frontend** | Next.js 16 App Router, TypeScript strict, shadcn/ui, TanStack Query, Zustand |
| **Database** | PostgreSQL — schema tham chiếu: `schema.sql` |
| **Package Manager** | Backend: dotnet CLI · Frontend: npm |

## Quy trình Khởi động (Bắt đầu MỌI phiên — không bỏ qua)

```
1. cat claude-progress.md          ← đọc "Bước tiếp theo"
2. cat feature_list.json           ← xác nhận tính năng đang làm
3. Đọc dependency_map trong system_registry.json cho file sẽ sửa
4. .\init.ps1                      ← xác minh môi trường
```

**Nếu init thất bại → sửa ngay, KHÔNG thêm tính năng mới.**

## Lệnh Xác minh (chạy trước khi báo hoàn thành)

```powershell
# Backend
cd backend
dotnet build ITHunterview_V1.slnx

# Frontend
cd frontend
npm run build
```

## Quy tắc Làm việc

- Làm **1 tính năng** tại 1 thời điểm
- Khi sửa file X → đọc `dependency_map` trong `system_registry.json` → cập nhật tất cả file bị ảnh hưởng **trong cùng phiên**
- **KHÔNG** đánh dấu `done` nếu chưa chạy lệnh xác minh và có bằng chứng
- Cuối phiên: cập nhật `claude-progress.md` + `feature_list.json` + `system_registry.json`

## Ràng buộc Kiến trúc (Không thương lượng)

### Backend
- Kiến trúc 3 tầng: `Domain → Service → WebAPI` — không nhảy tầng
- Không viết business logic trong Controller
- Không dùng `Guid.NewGuid()` ở client side cho PK — dùng `uuid_generate_v4()` qua DB default
- Xem chi tiết: `.agents/Backend/CODING_RULES.md`

### Frontend
- Luồng dữ liệu cố định: `page.tsx → hook → service → api-client → backend`
- Page không gọi axios trực tiếp. Service không có useState/JSX
- 6 route groups: `(public)` `(auth)` `(candidate)` `(recruiter)` `(staff)` `(admin)`
- Xem chi tiết: `frontend/kinh-mantra.md` + `.agents/Frontend/DESIGN.md`

## Tài liệu chi tiết

| Nội dung | File |
|---|---|
| Logic nghiệp vụ đầy đủ | `business_logic_specification.md` |
| Kiến trúc Backend | `.agents/Backend/ARCHITECTURE.md` |
| Luật code Backend | `.agents/Backend/CODING_RULES.md` |
| Thay đổi Backend đã làm | `.agents/Backend/CHANGES_REGISTRY.md` |
| **Luật code Frontend** | `frontend/kinh-mantra.md` ← ĐỌC TOÀN BỘ trước khi viết frontend |
| **Design System Frontend** | `.agents/Frontend/DESIGN.md` ← ĐỌC TOÀN BỘ trước khi viết frontend |
| Các lỗi đã biết & trạng thái | `system_registry.json → identified_flaws` |
| Dependency map | `system_registry.json → dependency_map` |
| Lịch sử thay đổi | `action_history.json` |
| Tiến độ phiên hiện tại | `claude-progress.md` |
| Danh sách tính năng | `feature_list.json` |

## Cuối Phiên (Trước khi kết thúc)

1. Cập nhật `claude-progress.md` — ghi đã làm gì, bước tiếp theo
2. Cập nhật `feature_list.json` — đổi status, ghi verification evidence
3. Cập nhật `system_registry.json` — thêm `dependency_map` entry nếu có file mới
4. Cập nhật `action_history.json` — append action mới
5. Commit với message mô tả rõ
