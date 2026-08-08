# Hướng Dẫn Triển Khai Đa Ngôn Ngữ (Switch Language)

Tài liệu này quy định quy chuẩn triển khai tính năng đa ngôn ngữ (Localization) cho các tính năng mới trong dự án ITHunterView, nhằm đảm bảo mã nguồn gọn gàng, không bị *spaghetti code* và hạn chế tối đa conflict khi làm việc nhóm.

Dự án sử dụng **`next-intl`** kết hợp với **Static JSON Generation** để quản lý ngôn ngữ.

## 1. Nguyên Tắc Cốt Lõi
- **TUYỆT ĐỐI KHÔNG** sửa trực tiếp vào các file `messages/en.json` và `messages/vi.json` bằng tay. Hai file này là file được tự động sinh ra (auto-generated). Nếu bạn sửa tay, code của bạn sẽ bị ghi đè và mất dữ liệu.
- Mọi bản dịch phải được quản lý thông qua các file script cập nhật JSON (ví dụ: `update_recruiter_json.js`, `update_candidate_json.js`, `update_master_data_json.js`, v.v.) nằm ở thư mục `frontend/`.
- Phải gom nhóm (namespace) rõ ràng cho từng page hoặc module để tránh trùng lặp Key.

## 2. Quy Trình 3 Bước Để Localize Tính Năng Mới

### Bước 1: Tạo hoặc Cập nhật file Script JSON
Tìm file script tương ứng với module bạn đang làm (VD: nếu làm tính năng cho Staff, hãy tạo `update_staff_json.js` hoặc sửa file hiện có). 

Định nghĩa object chứa key-value cho cả tiếng Anh và tiếng Việt:

```javascript
// Ví dụ nội dung update_staff_json.js

const fs = require('fs');
const en = require('./messages/en.json');
const vi = require('./messages/vi.json');

const staffDashboardEn = {
  welcomeMessage: 'Welcome back, {name}',
  totalUsers: 'Total Users',
  // ...
};

const staffDashboardVi = {
  welcomeMessage: 'Chào mừng quay trở lại, {name}',
  totalUsers: 'Tổng số người dùng',
  // ...
};

// Gán namespace vào file JSON
en['StaffDashboard'] = staffDashboardEn;
vi['StaffDashboard'] = staffDashboardVi;

// Ghi đè lại file JSON
fs.writeFileSync('messages/en.json', JSON.stringify(en, null, 2));
fs.writeFileSync('messages/vi.json', JSON.stringify(vi, null, 2));

console.log('Staff translations updated successfully!');
```

### Bước 2: Chạy Script để Sinh file JSON
Mở terminal, di chuyển vào thư mục `frontend/` và chạy script bạn vừa tạo/cập nhật:

```bash
cd frontend
node update_staff_json.js
```
*Sau khi chạy, script sẽ tự động chèn namespace `StaffDashboard` vào `messages/en.json` và `messages/vi.json`.*

### Bước 3: Sử dụng Hook `useTranslations` trong Component
Trong file `.tsx` (Server hoặc Client component), import và sử dụng hook `useTranslations` của `next-intl` với đúng tên Namespace mà bạn đã gán ở Bước 1.

```tsx
import { useTranslations } from "next-intl"

export default function StaffDashboardPage({ userName }) {
  // Lấy hàm t từ namespace 'StaffDashboard'
  const t = useTranslations("StaffDashboard")

  return (
    <div>
      {/* Truyền param động vào chuỗi */}
      <h1>{t("welcomeMessage", { name: userName })}</h1>
      
      {/* Lấy text thông thường */}
      <p>{t("totalUsers")}: 150</p>
    </div>
  )
}
```

## 3. Các Trường Hợp Nâng Cao Cần Lưu Ý

### 3.1. Formatting với Component HTML/React (Rich Text)
Đôi khi bạn cần in đậm, chèn thẻ `<span>` với class màu sắc vào giữa một chuỗi. Thay vì hardcode HTML, hãy dùng `t.rich()`.

**Khai báo script JS:**
```javascript
showingText: 'Đang xem <span>{start}</span> đến <span>{end}</span>',
```

**Sử dụng trong React Component:**
```tsx
<p>
  {t.rich("showingText", { 
    start: () => 1,
    end: () => 10,
    span: (chunks) => <span className="font-bold text-red-500">{chunks}</span>
  })}
</p>
```
*Lưu ý: Bạn bắt buộc phải truyền giá trị dạng arrow function `() => value` nếu giá trị đó là số/chữ nguyên thuỷ khi dùng `t.rich`.*

### 3.2. Không Cho Phép Chữ Bị Rớt Dòng (UI Bugs)
Khi chuyển ngôn ngữ (đặc biệt từ EN sang VI), chữ thường sẽ dài hơn và làm vỡ Layout hoặc rớt dòng ở các nút bấm (Button). 
- Luôn kiểm tra giao diện sau khi tích hợp.
- Nếu text bị rớt dòng sai ý muốn, hãy sử dụng Tailwind class `whitespace-nowrap` bọc ngoài phần tử đó (như `<Link>` hoặc `<button>`) và thêm `shrink-0` vào icon đi kèm.

### 3.3. Check Lỗi Typescript Lần Cuối
`next-intl` kiểm tra type (kiểu dữ liệu) rất chặt chẽ giữa code của bạn và cấu trúc file JSON. Nếu bạn gọi một key không tồn tại, Typescript sẽ báo lỗi.
Hãy chạy command sau ở thư mục `frontend` trước khi push code để rà soát:
```bash
npx tsc --noEmit
```

Tuân thủ đúng quy tắc này, ứng dụng của bạn sẽ được dịch một cách hoàn hảo, không dính conflict, và dễ dàng thêm mới bao nhiêu trang tuỳ thích!
