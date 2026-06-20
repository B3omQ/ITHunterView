# Task: Implement Frontend — Company Profile & Legal Verification flow

## Context
Recruiter cần hoàn thành 2 bước riêng để công ty được xác minh trên hệ thống:
1. **Company Profile** — thông tin hiển thị công khai cho candidate (logo, tên, ngành, quy mô, website, mô tả).
2. **Legal Verification** — thông tin pháp lý để admin duyệt (mã số thuế, địa chỉ, phương thức xác minh, file chứng từ).

2 mockup tham khảo: "Company Profile" và "Legal Verification" (đã có sẵn trong repo/design). Đây là 2 trang/section riêng, không gộp chung 1 form.

## Use case flow cần implement

### UC1 — Recruiter vào trang Company Profile lần đầu (chưa có company)
1. Gọi API lấy company hiện tại của recruiter (`GET /api/companies/me` hoặc tương đương theo BE đã định nghĩa).
2. Nếu chưa có company → hiển thị form Company Profile ở trạng thái rỗng, không hiển thị badge trạng thái xác minh.
3. Recruiter điền: Company Logo (upload ảnh), Company Name, Industry (dropdown), Company Size (dropdown), Website URL, Company Description (rich text).
4. Validate trước khi submit:
   - `Company Name`: required
   - `Industry`: required, chọn từ danh sách enum cố định
   - `Company Size`: required, chọn từ danh sách enum cố định
   - `Website URL`: optional, nếu có nhập phải đúng format URL
   - `Company Description`: required, tối thiểu 100 ký tự (theo gợi ý trên mockup "Minimum 100 characters recommended" — hiển thị đếm ký tự realtime dưới textarea)
5. Khi bấm "Save Company Profile":
   - Nếu chưa có company → gọi API tạo mới (`POST /api/companies`)
   - Nếu đã có company → gọi API update (`PATCH /api/companies/{id}/profile`)
6. Sau khi save thành công → hiển thị toast/thông báo thành công, và (nếu là lần đầu tạo) tự động chuyển hướng hoặc gợi ý sang bước Legal Verification tiếp theo.
7. Nút "Preview as Candidate" → mở xem trước trang công ty hiển thị với candidate (route riêng hoặc modal preview), KHÔNG submit form.
8. Nút "Cancel" → quay lại không lưu thay đổi, nếu form đã chỉnh sửa (dirty) thì hỏi xác nhận trước khi rời trang.

### UC2 — Logo upload
1. Click "Upload Image" hoặc click vào ô Logo → mở file picker.
2. Validate phía client trước khi gọi API: chỉ nhận image (jpeg/jpg/png), kích thước tối thiểu gợi ý 200×200px (theo mockup), giới hạn dung lượng theo cấu hình BE.
3. Trong khi upload: hiển thị loading state trên vùng logo (spinner/progress).
4. Upload xong → backend trả về URL, hiển thị preview logo ngay (không cần reload), lưu URL này vào state form để gửi kèm khi submit Save.
5. Nếu upload lỗi (sai format, quá dung lượng) → hiển thị lỗi inline ngay tại khu vực upload, không chặn phần còn lại của form.

### UC3 — Recruiter vào trang Legal Verification
1. Gọi API lấy company hiện tại + trạng thái verification.
2. Nếu chưa có company (chưa hoàn thành UC1) → chặn truy cập trang này, hiển thị thông báo yêu cầu hoàn thành Company Profile trước, kèm link/nút điều hướng sang đó.
3. Nếu đã có company → hiển thị form với data hiện có (nếu đã từng nộp), kèm **badge trạng thái** hiển thị rõ: `PENDING` (đang chờ duyệt), `VERIFIED` (đã xác minh), `REJECTED` (bị từ chối, kèm hiển thị `reject_reason` nếu có).
4. Recruiter điền: Tax ID / Registration Number, Company Name (đồng bộ/hiển thị lại từ Company Profile, có thể cho sửa riêng ở đây hoặc disable — quyết định theo UX, ghi rõ lựa chọn trong code comment), Headquarters Address.
5. Chọn 1 trong 2 radio "Verification method": `Business Registration Certificate` hoặc `Power of Attorney (POA) and Identity Document` — đây là field bắt buộc chọn 1.
6. Tuỳ theo method chọn, label vùng upload file đổi tương ứng (vd "Business Certificate" hoặc "POA & ID Document") — đọc đúng theo mockup.
7. Validate trước khi submit:
   - `Tax ID`: required
   - `Company Name`: required
   - `Headquarters Address`: required
   - `Verification method`: required, chọn 1 trong 2
   - File đính kèm: required, format cho phép `jpeg, jpg, png, pdf`, dung lượng tối đa 5MB (đúng theo text trên mockup "Maximum file size 5MB. Supported formats: jpeg, jpg, png, pdf")
8. Hiển thị khối "Document Guidelines" tĩnh (không cần logic, chỉ là nội dung cảnh báo cố định theo mockup).
9. Khi bấm "Save Legal Profile":
   - Gọi API `PATCH /api/companies/{id}/legal`
   - Disable nút submit + hiển thị loading trong lúc gọi API, tránh double-submit
10. Sau khi save thành công:
    - Nếu trước đó status là `REJECTED` hoặc lần đầu nộp → hiển thị rõ status mới chuyển về `PENDING`, thông báo "Hồ sơ đang chờ admin duyệt"
    - Disable toàn bộ form (read-only) trong khi status = `PENDING` hoặc `VERIFIED`, CHỈ cho phép sửa lại khi status = `REJECTED` (đúng business rule: không cho sửa hồ sơ đang chờ duyệt để tránh admin duyệt nhầm data cũ)
11. Nút "Cancel" → tương tự UC1, hỏi xác nhận nếu form dirty.

### UC4 — Chặn hành động tạo job khi công ty chưa VERIFIED
1. Ở trang/flow tạo Job Posting, trước khi cho phép vào form hoặc submit, kiểm tra `company.status`.
2. Nếu `status !== 'VERIFIED'` → chặn, hiển thị thông báo rõ ràng kèm nút điều hướng sang trang Legal Verification để hoàn thành xác minh (map đúng với rule backend đã enforce ở API — đây là chặn ở UI để UX tốt hơn, không phải lớp bảo mật chính).

### UC5 — Hiển thị trạng thái xuyên suốt
1. Có một khu vực hiển thị trạng thái công ty (badge màu) xuất hiện nhất quán ở các trang liên quan (Company Profile, Legal Verification, Job creation):
   - `PENDING` — màu vàng/amber, text "Đang chờ xác minh" / "Pending verification"
   - `VERIFIED` — màu xanh, text "Đã xác minh" / "Verified"
   - `REJECTED` — màu đỏ, text "Bị từ chối" / "Rejected", kèm tooltip/expand hiển thị `reject_reason`

## Ràng buộc UX quan trọng
- 2 form (Profile và Legal) độc lập về submit — sửa Profile không ảnh hưởng status xác minh, chỉ sửa/nộp lại Legal mới đổi status.
- Không cho recruiter sửa Legal Verification khi đang `PENDING` hoặc `VERIFIED` (chỉ xem, không edit) — tránh tình trạng admin đang xem data cũ trong lúc recruiter đổi data mới.
- Toàn bộ upload file (logo, certificate) phải hoàn thành (nhận được URL từ server) TRƯỚC KHI cho phép bấm nút Save chính của form — không submit form khi file đang upload dở.
- Giữ đúng text/label/placeholder như trong mockup (đa số tiếng Anh theo thiết kế gốc).

## Output mong đợi
- Các page/component tương ứng UC1-UC5 theo đúng convention đã định nghĩa trong file stack frontend (kiến trúc thư mục, state management, styling — đọc file đó trước khi code).
- Validation logic implement đúng theo rule liệt kê trên (client-side validate trước, không thay thế server-side validate).
- Xử lý đầy đủ loading/error/success state cho mọi lần gọi API.
- Không cần style pixel-perfect 100% theo mockup nếu file stack đã có design system riêng — ưu tiên đúng spacing/component pattern của design system, miễn giữ đúng field, đúng luồng, đúng validation rule.