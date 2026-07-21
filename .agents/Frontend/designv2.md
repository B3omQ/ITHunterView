# 🎯 ITHunterview Design System v2: BaseListCard Standardization

> **Phiên bản:** v2.0 — Cập nhật dựa trên phân tích thực tế `SavedJobCard` (trang Saved Jobs).
> **Ngày tạo:** 2026-07-21
> **Áp dụng cho:** `Saved Jobs`, `Learning Paths`, `Mock Interview`

---

## Lời gọi hệ thống (System Prompt for AI Agent)

Bạn đóng vai trò là một **Chuyên gia Frontend Developer / UI Engineer** làm việc trên dự án **ITHunterView** (Next.js + shadcn/ui + Tailwind CSS). Nhiệm vụ của bạn là refactor các Component Card trên các trang `Saved Jobs`, `Learning Paths`, và `Mock Interview`. Tất cả các thẻ phải tuân thủ cấu trúc `BaseListCard v2` dưới đây, đảm bảo đồng nhất 100% trên toàn hệ thống **và** phù hợp với tech stack thực tế của dự án.

---

## ⚡ Điểm khác biệt so với v1 (Gap Analysis)

| # | Tiêu chí | v1 (Spec gốc) | Thực tế `SavedJobCard` | Quyết định v2 |
|---|---|---|---|---|
| 1 | **Layout tổng thể** | Horizontal list, 1 Zone 3 cột ngang duy nhất | **2 rows**: Row trên (Info + Toggle), Row dưới (Action buttons) ngăn cách bởi `border-t` | ✅ Giữ 2-row layout. Phù hợp với nhiều action hơn |
| 2 | **Action Zone** | Nút hành động dạt sang **mép phải**, cùng hàng với content | Action buttons nằm ở **row riêng phía dưới** (`border-t`), layout `flex-wrap` | ✅ Cho phép Action Row riêng khi có ≥ 2 actions chức năng |
| 3 | **Zone 1 Avatar** | Hình vuông bo góc `8px`, `48x48px` | `w-12 h-12` (48px) + `rounded` + `overflow-hidden` + `border` | ✅ Giữ nguyên, thêm `border` wrapper |
| 4 | **Tech stack styling** | Đề xuất CSS Variables thuần | Dùng **shadcn/ui** (`Card`, `CardContent`, `Button`) + Tailwind utility classes | ✅ Bắt buộc dùng `shadcn/ui` + Tailwind. Không dùng inline CSS hay CSS modules |
| 5 | **Status Badge** | Badge ngay cạnh Title (Row 1) | Không có badge trên `SavedJobCard`; badge dùng cho Learning Path/Interview | ✅ Badge chỉ bắt buộc ở Learning Path và Interview card |
| 6 | **Màu sắc Primary** | `#1877F2` (hardcode hex) | Dùng Tailwind token: `text-primary`, `hover:text-primary/80`, `bg-primary/10` | ✅ Bắt buộc dùng Tailwind semantic tokens, không hardcode hex |
| 7 | **Nút thứ cấp (Secondary Action)** | Icon thùng rác / trái tim xám | Nút có nhãn + icon màu theo chức năng: `indigo` (Match CV), `emerald` (Mock Interview) | ✅ Cho phép color-coded secondary buttons để phân biệt chức năng |
| 8 | **Metadata icons** | Icon cùng màu xám `#6B7280`, `16x16px` | `w-3 h-3` (12px), màu slate tự động | ✅ Dùng `w-3 h-3` cho metadata, `w-4 h-4` cho action buttons |
| 9 | **Hover effect** | Không đề cập | `hover:border-primary/50 transition-colors group` trên `<Card>` | ✅ Bắt buộc có hover border effect trên toàn thẻ |
| 10 | **Company Name** | Không đề cập | Dòng phụ dưới Title: `text-muted-foreground text-sm` | ✅ Thêm vào spec: Subtitle/Subname là hàng riêng dưới Title |

---

## 1. Cấu trúc Tổng thể (Overall Layout)

- **Component Shell:** Bắt buộc dùng `<Card>` và `<CardContent>` từ shadcn/ui.
- **Width:** `w-full` của container cha.
- **Container:** `<div className="flex flex-col gap-4">` bao ngoài list.
- **Hover:** Thêm `hover:border-primary/50 transition-colors group` trên `<Card>`.
- **Padding:** `p-4` trên `<CardContent>`.
- **Cấm:** Layout 2 cột (grid-cols-2) cho các trang danh sách. Cấm shadow nặng.

---

## 2. Giải phẫu Component (Card Anatomy — 2 Rows)

### 🔷 Row 1 (Top Row): Info Row

```
[ Zone 1: Avatar ] [ Zone 2: Content (flex-grow) ] [ Zone 3: Toggle Action ]
```

Layout: `flex items-start justify-between gap-4`

---

#### Zone 1 — Avatar / Logo
- **Size:** `w-12 h-12` (48×48px)
- **Shape:** `rounded overflow-hidden` + `border` (viền nhạt)
- **Background:** `bg-slate-100`
- **Fallback:** Dùng component `<CompanyLogo>` với `fallbackType` (vd: `"briefcase"`, `"book"`, `"mic"`)
- **Fallback icon:** `text-slate-400 w-5 h-5`
- **Wrapper:** Bao bằng `<Link href={...}>` để click vào avatar → điều hướng đến detail page

---

#### Zone 2 — Content (Chiếm toàn bộ không gian còn lại)
- **Layout:** `flex-1`, `flex-col`
- **Row 2a — Title:**
  - `font-semibold text-primary hover:underline line-clamp-1 text-base`
  - Bao bằng `<Link>` dẫn đến detail page
- **Row 2b — Subtitle** *(entity phụ: tên công ty, tên lộ trình, loại interview)*:
  - `text-muted-foreground text-sm`
- **Row 2c — Metadata:**
  - Layout: `flex items-center gap-4 mt-1`
  - Typography: `text-xs text-slate-500`
  - Icon: `w-3 h-3` cùng màu với text
  - Separator: `gap-4` hoặc ký tự `•`
  - **Cố định 1 dòng** (dùng `line-clamp-1` nếu quá dài)
  - **Cấm** icon nhiều màu ở metadata row
- **Row 2d — Status Badge** *(chỉ bắt buộc cho Learning Path, Mock Interview)*:
  - Nằm inline với Title hoặc dòng riêng ngay dưới Metadata
  - Style: `rounded-full px-2 py-0.5 text-xs font-medium`
  - Màu sắc theo trạng thái (xem bảng màu bên dưới)

---

#### Zone 3 — Toggle Action (Nút đơn, phải trên cùng)
- **Vị trí:** `shrink-0`, căn `items-start` theo Row 1
- **Dùng cho:** Nút bookmark/unsave (Heart), nút favorite, hoặc menu `...`
- **Style:**
  ```tsx
  <Button variant="ghost" size="icon"
    className="text-primary hover:text-primary/80 hover:bg-primary/10 transition-colors shrink-0">
    <Heart className="w-5 h-5 fill-current" />
  </Button>
  ```
- **Cấm:** Đặt nút chức năng chính (View, Start) ở đây. Zone 3 chỉ dành cho toggle đơn.

---

### 🔶 Row 2 (Action Row): Function Buttons

```
[ Nút chính ] [ Nút phụ 1 ] [ Nút phụ 2 ] ...
```

- **Separator:** `border-t border-border/50 pt-3`
- **Layout:** `flex flex-wrap items-center gap-2`
- **Responsive:** Mỗi nút `flex-1 sm:flex-none` để wrap đẹp trên mobile
- **Bắt buộc có** Action Row khi card có ≥ 2 hành động chức năng
- **Không có** Action Row khi card chỉ có 1 hành động → dùng mũi tên `->` ở Zone 3

#### Phân loại nút:

| Loại | Style | Dùng cho |
|---|---|---|
| **Primary** | `variant="outline" size="sm"` | "View Details", "View Progress" |
| **AI Action** | `variant="secondary" size="sm" bg-indigo-50 text-indigo-700 border-indigo-200 border` | "Match CV", "Analyze" |
| **Interview** | `variant="secondary" size="sm" bg-emerald-50 text-emerald-700 border-emerald-200 border` | "Mock Interview", "Start Session" |
| **Danger** | `variant="ghost" size="sm" text-red-500 hover:bg-red-50` | "Remove", "Delete" |

---

## 3. Bảng màu Status Badge

| Trạng thái | Background | Text | Ví dụ |
|---|---|---|---|
| Active / Completed | `bg-emerald-100` | `text-emerald-700` | "Completed", "Active" |
| In Progress | `bg-blue-100` | `text-blue-700` | "In Progress" |
| Pending | `bg-yellow-100` | `text-yellow-700` | "Pending" |
| Expired / Failed | `bg-red-100` | `text-red-700` | "Expired", "Failed" |
| Saved | `bg-slate-100` | `text-slate-600` | "Saved" |

---

## 4. Bảng màu & Token chuẩn (Tailwind Semantic Tokens)

> **Bắt buộc** dùng Tailwind semantic tokens thay vì hardcode hex.

| Mục đích | Token Tailwind | Tương đương |
|---|---|---|
| Primary brand | `text-primary` / `bg-primary` | Tuỳ theme shadcn |
| Primary nhạt | `bg-primary/10`, `hover:bg-primary/10` | — |
| Title text | `font-semibold text-primary` | Bold + Brand color |
| Subtitle | `text-muted-foreground text-sm` | Xám vừa |
| Metadata | `text-slate-500 text-xs` | Xám nhạt |
| Icon metadata | `w-3 h-3 text-slate-400` | 12px xám |
| Icon action | `w-4 h-4` | 16px |
| Card hover | `hover:border-primary/50 transition-colors` | Border nhấp nháy |
| Avatar bg | `bg-slate-100` | Nền xám nhạt |
| Row separator | `border-t border-border/50` | Đường kẻ nhạt |

---

## 5. Template Code Component (BaseListCard)

```tsx
// components/shared/BaseListCard.tsx
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import Link from 'next/link';

interface BaseListCardProps {
  // Zone 1
  logoSrc?: string;
  logoAlt?: string;
  logoFallbackType?: 'briefcase' | 'book' | 'mic';
  detailHref: string;

  // Zone 2
  title: string;
  subtitle?: string;
  metadata: { icon: React.ReactNode; text: string }[];
  statusBadge?: { label: string; className: string };

  // Zone 3
  toggleAction?: React.ReactNode;

  // Row 2 - Actions
  actions?: {
    label: string;
    href?: string;
    onClick?: () => void;
    icon?: React.ReactNode;
    className?: string;
    variant?: 'outline' | 'secondary' | 'ghost';
  }[];
}

export function BaseListCard({ ... }: BaseListCardProps) {
  return (
    <Card className="hover:border-primary/50 transition-colors group">
      <CardContent className="p-4 flex flex-col gap-4">
        {/* Row 1: Info */}
        <div className="flex items-start justify-between gap-4">
          {/* Zone 1: Avatar */}
          <Link href={detailHref} className="shrink-0">
            <div className="w-12 h-12 rounded overflow-hidden bg-slate-100 flex items-center justify-center border">
              <CompanyLogo src={logoSrc} alt={logoAlt} fallbackType={logoFallbackType} />
            </div>
          </Link>

          {/* Zone 2: Content */}
          <div className="flex-1">
            <Link href={detailHref} className="font-semibold text-primary hover:underline line-clamp-1 text-base">
              {title}
            </Link>
            {subtitle && <p className="text-muted-foreground text-sm">{subtitle}</p>}
            <div className="flex items-center gap-4 mt-1 text-xs text-slate-500">
              {metadata.map((m, i) => (
                <span key={i} className="flex items-center gap-1">{m.icon} {m.text}</span>
              ))}
            </div>
            {statusBadge && (
              <span className={`mt-1 inline-block rounded-full px-2 py-0.5 text-xs font-medium ${statusBadge.className}`}>
                {statusBadge.label}
              </span>
            )}
          </div>

          {/* Zone 3: Toggle */}
          {toggleAction && <div className="shrink-0">{toggleAction}</div>}
        </div>

        {/* Row 2: Actions (chỉ render khi có actions) */}
        {actions && actions.length > 0 && (
          <div className="flex flex-wrap items-center gap-2 pt-3 border-t border-border/50">
            {actions.map((action, i) =>
              action.href ? (
                <Link key={i} href={action.href} className="flex-1 sm:flex-none">
                  <Button variant={action.variant ?? 'outline'} size="sm"
                    className={`w-full gap-2 ${action.className ?? ''}`}>
                    {action.icon}{action.label}
                  </Button>
                </Link>
              ) : (
                <Button key={i} variant={action.variant ?? 'outline'} size="sm"
                  onClick={action.onClick}
                  className={`flex-1 sm:flex-none gap-2 ${action.className ?? ''}`}>
                  {action.icon}{action.label}
                </Button>
              )
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
```

---

## 6. Yêu cầu thực thi (Action Required for Agent)

### Bước 1 — Đọc code hiện tại
Đọc toàn bộ code của:
- `SavedJobCard.tsx` (baseline chuẩn)
- `LearningPathCard.tsx` hoặc tương đương trong trang Learning Path
- Card component trong trang Mock Interview

### Bước 2 — Xác định điểm lệch
So sánh mỗi card với spec v2. Liệt kê cụ thể:
- Zone nào thiếu / sai layout
- Màu hardcode nào cần đổi sang Tailwind token
- Action button nào đang đặt sai vị trí

### Bước 3 — Refactor (KHÔNG xóa logic, CHỈ đổi UI)
- Giữ nguyên: hooks, props interface, data fetching, links.
- Chỉ đổi: cấu trúc JSX, className Tailwind, icon sizing.
- Ưu tiên: dùng `BaseListCard` làm wrapper nếu có thể, hoặc mirror pattern từ `SavedJobCard`.

### Bước 4 — Validate
Sau khi refactor, kiểm tra:
- [ ] Layout horizontal, 2 rows rõ ràng
- [ ] Avatar 48x48, bo góc, có border
- [ ] Title `font-semibold text-primary`, Subtitle `text-muted-foreground`
- [ ] Metadata `text-xs text-slate-500`, icon `w-3 h-3`
- [ ] Action Row nằm dưới, tách bằng `border-t`
- [ ] Action buttons color-coded đúng chức năng
- [ ] Card có `hover:border-primary/50 transition-colors`
- [ ] Không có hardcode hex màu nào
- [ ] Không có drop-shadow 3D đậm

---

## 7. Lệnh cấm tuyệt đối

- ❌ Hardcode hex màu trong JSX/Tailwind (dùng token thay thế)
- ❌ Layout 2 cột cho danh sách
- ❌ Avatar hình tròn (`rounded-full` trên avatar block)
- ❌ Action buttons nằm cùng row với avatar/title
- ❌ Drop shadow nặng (`shadow-lg`, `shadow-xl`)
- ❌ Nối nhiều action vào Zone 3 — Zone 3 chỉ dành cho toggle đơn (save/unsave)
- ❌ Import CSS module hoặc styled-components — toàn bộ dùng Tailwind utility
