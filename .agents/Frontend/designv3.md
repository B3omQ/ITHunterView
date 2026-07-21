# 🎯 ITHunterview Design System v3: Candidate List Pages — Chuẩn Hóa Toàn Diện

> **Phiên bản:** v3.0 — Nghiên cứu thực tế toàn bộ 6 trang list trong role Candidate
> **Ngày tạo:** 2026-07-21
> **Tác giả:** Từ phân tích source code thực tế của dự án
> **Phạm vi áp dụng:** Tất cả các trang có danh sách dữ liệu trong Candidate Dashboard

---

## 0. Bản đồ các trang List trong Candidate

| Trang | Route | Card type | Có avatar? | Có badge? | Có score/progress? | Có action row? |
|---|---|---|---|---|---|---|
| **Saved Jobs** | `/candidate/saved-jobs` | `SavedJobCard` | ✅ Logo công ty | ❌ | ❌ | ✅ 3 nút |
| **Applied Jobs** | `/candidate/applications` | Inline card | ✅ Logo công ty | ✅ Status | ❌ | ✅ View Job |
| **CV Matching** | `/candidate/cv-matching` | Inline card | ✅ Icon Activity | ✅ Status | ✅ Score tròn | ✅ View Report |
| **Learning Path** | `/candidate/learning-path` | Inline card | ✅ Icon Map | ✅ Status | ✅ Progress bar | ❌ ArrowRight |
| **Mock Interview** | `/candidate/interview` | Inline card | ✅ Icon MessageSquare | ✅ Status | ❌ | ❌ ArrowRight |
| **Resumes (CV)** | `/candidate/resumes` | `CvCard` | ✅ Icon FileText | ❌ | ❌ | ✅ Delete link |

> ⚠️ **Billing History** dùng Table layout riêng — không thuộc phạm vi card spec này.
> ⚠️ **Resumes** có layout đặc thù (split-view với iframe preview) — card nhỏ trong panel trái.

---

## 1. Vấn đề hiện tại (As-is Problems)

### 1.1. Avatar/Icon không nhất quán

| Trang | Size | Shape | Có border? |
|---|---|---|---|
| Saved Jobs | `w-12 h-12` | `rounded` (vuông bo) | ✅ `border` |
| Applied Jobs | `w-16 h-16` | `rounded-md` | ✅ `border-zinc-100` |
| CV Matching | `w-12 h-12` | `rounded` | ✅ `border` |
| Learning Path | `w-10 h-10` | `rounded-full` **tròn** | ❌ không có |
| Mock Interview | `w-10 h-10` | `rounded-full` **tròn** | ❌ không có |
| CV Card | `w-12 h-12` | `rounded-lg` | ❌ không có |

**→ Có tới 3 shape khác nhau, 2 size khác nhau.**

### 1.2. Action zone không nhất quán

| Trang | Pattern action |
|---|---|
| Saved Jobs | Action Row riêng ở dưới (border-t), flex-wrap |
| Applied Jobs | Badge + Button nằm cột bên phải (column, items-end) |
| CV Matching | Action Row ở dưới (border-t), justify-between |
| Learning Path | Trash icon + ArrowRight cùng hàng bên phải |
| Mock Interview | Trash icon + ArrowRight cùng hàng bên phải |
| CV Card | Action Row ở dưới (border-t) nhưng dùng `<button>` HTML thuần |

### 1.3. Màu sắc hardcode rải rác

- `Applications`: `text-zinc-900`, `hover:text-blue-600`, `bg-blue-50 text-blue-700`
- `Learning Path`: `bg-[#E6F4EA] text-[#137333]`, `bg-[#E6F0FF] text-[#0052CC]` — hardcode hex
- `CV Matching`: `text-emerald-600 bg-emerald-500/10` — semantic nhưng không thống nhất

### 1.4. Click toàn card không đồng nhất

- Learning Path, Mock Interview: Không có click toàn card (click phải vào Link)
- CV Card: Có `onClick` toàn card
- Applications: Không có click toàn card

---

## 2. Design Decisions (To-be)

### 2.1. Tại sao KHÔNG làm 1 component `BaseListCard` duy nhất?

Sau khi phân tích, **không nên** tạo một generic component quá trừu tượng vì:
- Mỗi card có quá nhiều trường dữ liệu đặc thù (Score tròn, Progress bar, Avatar thật vs Icon)
- Prop drilling sẽ rất phức tạp và khó maintain
- **Quyết định:** Tạo ra **một pattern thống nhất** (cấu trúc HTML/class giống nhau), mỗi card là component riêng, nhưng **đều follow đúng skeleton layout này**.

### 2.2. Layout được chọn: Single-Row Compact (học từ Interview + Learning Path)

```
┌─────────────────────────────────────────────────────────────┐
│  [Avatar]  [Zone Content flex-1]            [Zone Action]   │  ← Row chính (luôn có)
│             Title + Badge (inline)                          │
│             Subtitle (nếu có)                               │
│             Metadata icons                                  │
│─────────────────────────────────────────────────────────────│
│  [Nút chức năng 1]  [Nút chức năng 2]  [Nút Delete ghost]  │  ← Action Row (chỉ khi cần)
└─────────────────────────────────────────────────────────────┘
```

**Lý do chọn Single-Row:** Compact hơn, thông tin chính nổi bật hơn, ít chiều dọc hơn.
**Action Row:** Chỉ xuất hiện khi card có ≥ 2 action chức năng khác nhau.

---

## 3. Chuẩn Avatar/Icon Zone (Áp dụng cho tất cả)

```tsx
// ✅ Khi có ảnh thật (logo công ty)
<div className="w-11 h-11 rounded-lg overflow-hidden bg-muted flex items-center justify-center border border-border shrink-0">
  <img src={logoUrl} alt={alt} className="w-full h-full object-contain" />
</div>

// ✅ Khi dùng icon thay thế (không có ảnh thật)
<div className="w-11 h-11 rounded-lg bg-{color}/10 flex items-center justify-center shrink-0">
  <Icon className="w-5 h-5 text-{color}-500" />
</div>
```

**Quy tắc cứng:**
- **Size:** `w-11 h-11` (44×44px) — thống nhất tất cả card
- **Shape:** `rounded-lg` — **không dùng `rounded-full`** (tròn) cho avatar card
- **Avatar thật (logo công ty):** thêm `border border-border` + `bg-muted`
- **Icon placeholder:** dùng `bg-{color}/10`, **không có border**
- **Icon màu sắc:** Mỗi loại card có màu đặc trưng riêng (xem bảng bên dưới)

### Bảng màu Icon theo loại card:

| Card | Icon | Màu nền | Màu icon |
|---|---|---|---|
| Saved Jobs | Logo thật / `Briefcase` | `bg-muted` + `border` | `text-slate-400` |
| Applied Jobs | Logo thật / `Building2` | `bg-muted` + `border` | `text-slate-400` |
| CV Matching | `Activity` | `bg-indigo-500/10` | `text-indigo-500` |
| Learning Path | `Map` | `bg-blue-500/10` | `text-blue-500` |
| Mock Interview | `MessageSquare` | `bg-emerald-500/10` | `text-emerald-500` |
| CV Card | `FileText` | `bg-primary/10` | `text-primary` |

---

## 4. Chuẩn Content Zone

### Row A — Title + Status Badge (cùng hàng)
```tsx
<div className="flex items-center gap-2 min-w-0">
  <span className="font-semibold text-base text-foreground group-hover:text-primary transition-colors truncate">
    {title}
  </span>
  {statusBadge && <StatusBadge status={statusBadge} />}
</div>
```
- Title: `font-semibold text-base text-foreground group-hover:text-primary transition-colors truncate`
- Nếu title là Link: bao bằng `<Link>` với `className="truncate font-semibold text-base ..."`
- Badge: nằm inline ngay sau title, `shrink-0`

### Row B — Subtitle (chỉ khi có entity phụ)
```tsx
<p className="text-sm text-muted-foreground truncate">
  {subtitle}
</p>
```
- Ví dụ: Tên công ty (Saved Jobs / Applied Jobs), Tên CV file (CV Matching)

### Row C — Metadata chips (icons + text)
```tsx
<div className="flex items-center gap-3 flex-wrap text-xs text-muted-foreground mt-0.5">
  <span className="flex items-center gap-1">
    <CalendarIcon className="h-3 w-3 shrink-0" />
    {dateText}
  </span>
  <span className="flex items-center gap-1">
    <LocationIcon className="h-3 w-3 shrink-0" />
    {location}
  </span>
</div>
```
- Font: `text-xs text-muted-foreground`
- Icon: `h-3 w-3 shrink-0` — **tất cả icon metadata dùng size này**
- Gap giữa các chip: `gap-3`
- **Không dùng icon nhiều màu ở row metadata** (chỉ dùng 1 màu: inherit từ `text-muted-foreground`)
- Nếu icon cần màu nhấn nhẹ: chỉ `text-primary/60` tối đa

### Row D — Progress Bar (chỉ cho Learning Path)
```tsx
<div className="flex items-center gap-2 mt-1">
  <Progress value={percent} className="flex-1 h-1.5" />
  <span className="font-semibold text-primary text-xs w-8 text-right">{percent}%</span>
</div>
```

---

## 5. Chuẩn Action Zone (bên phải, cùng row với avatar)

### Khi chỉ có 1 action đơn (navigate):
```tsx
// Pattern: ArrowRight tự animate khi hover
<div className="flex items-center gap-1 shrink-0">
  {/* Nút delete (hiện khi hover) */}
  <Button variant="ghost" size="icon"
    className="h-8 w-8 text-muted-foreground hover:text-destructive hover:bg-destructive/10 rounded-lg transition-colors opacity-0 group-hover:opacity-100">
    <Trash2 className="h-4 w-4" />
  </Button>
  <ArrowRight className="h-4 w-4 text-primary transform group-hover:translate-x-1 transition-transform" />
</div>
```

### Khi có Score đặc biệt (CV Matching):
```tsx
// Score badge tròn — vẫn nằm bên phải, trong action zone
<div className={`flex flex-col items-center justify-center font-bold w-12 h-12 rounded-full border ${scoreColor}`}>
  <span className="text-base leading-none">{score}</span>
  <span className="text-[9px] font-normal leading-none mt-0.5 opacity-80">Score</span>
</div>
```

### Khi có Toggle (Saved Jobs — Heart button):
```tsx
<Button variant="ghost" size="icon"
  className="h-8 w-8 text-primary hover:text-primary/80 hover:bg-primary/10 transition-colors shrink-0"
  onClick={handleUnsave}>
  <Heart className="w-4 h-4 fill-current" />
</Button>
```

---

## 6. Chuẩn Action Row (tầng dưới, chỉ khi cần)

**Điều kiện xuất hiện:** Card có từ **2 action chức năng trở lên** (không tính Delete).

```tsx
{/* Separator + Action Row */}
<div className="flex flex-wrap items-center gap-2 pt-2.5 border-t border-border/50">
  {/* Primary action */}
  <Link href={detailHref} className="flex-1 sm:flex-none">
    <Button variant="outline" size="sm" className="w-full gap-1.5">
      <Eye className="w-3.5 h-3.5" /> View Details
    </Button>
  </Link>

  {/* AI action */}
  <Link href={aiHref} className="flex-1 sm:flex-none">
    <Button size="sm" className="w-full gap-1.5 bg-indigo-50 hover:bg-indigo-100 text-indigo-700 border border-indigo-200">
      <Sparkles className="w-3.5 h-3.5" /> Match CV
    </Button>
  </Link>

  {/* Secondary AI action */}
  <Link href={interviewHref} className="flex-1 sm:flex-none">
    <Button size="sm" className="w-full gap-1.5 bg-emerald-50 hover:bg-emerald-100 text-emerald-700 border border-emerald-200">
      <MessageSquare className="w-3.5 h-3.5" /> Mock Interview
    </Button>
  </Link>

  {/* Delete — dùng Dialog confirm */}
  <Dialog>
    <DialogTrigger render={<Button variant="ghost" size="sm"
      className="text-muted-foreground hover:text-destructive hover:bg-destructive/10 gap-1.5" />}>
      <Trash2 className="h-3.5 w-3.5" /> Delete
    </DialogTrigger>
    {/* ... dialog content */}
  </Dialog>
</div>
```

### Bảng Action Row theo từng card:

| Card | Action Row có không? | Nội dung |
|---|---|---|
| Saved Jobs | ✅ Có | View Details (outline) + Match CV (indigo) + Mock Interview (emerald) |
| Applied Jobs | ❌ Không | Chỉ 1 nút "View Job" → đặt trong Action Zone (bên phải) |
| CV Matching | ✅ Có | View Full Report (outline) + Delete (ghost) |
| Learning Path | ❌ Không | Chỉ ArrowRight navigate + Trash (hover) |
| Mock Interview | ❌ Không | Chỉ ArrowRight navigate + Trash (hover) |
| CV Card | ✅ Có | Delete button (destructive text) |

---

## 7. Chuẩn Status Badge

**Luôn dùng shadcn `<Badge>` với custom className. Không dùng `<span>` inline.**

```tsx
// ✅ Chuẩn
<Badge className="shrink-0 text-[10px] px-1.5 py-0 border-none font-semibold bg-emerald-500/10 text-emerald-700">
  Completed
</Badge>

// ❌ Sai — hardcode hex
<Badge className="bg-[#E6F4EA] text-[#137333]">
  Completed
</Badge>
```

### Bảng màu Status Badge chuẩn:

| Trạng thái | className |
|---|---|
| Completed / Active / Applied | `bg-emerald-500/10 text-emerald-700` |
| In Progress / Viewed | `bg-blue-500/10 text-blue-700` |
| Pending / Processing | `bg-amber-500/10 text-amber-700` |
| Rejected / Failed / Expired | `bg-rose-500/10 text-rose-700` |
| Default / Unknown | `bg-muted text-muted-foreground` |

**Props cố định cho mọi Badge:**
```
text-[10px] px-1.5 py-0 border-none font-semibold shrink-0
```

---

## 8. Chuẩn Card Shell (wrapper)

```tsx
<Card className="group hover:border-primary/50 transition-colors">
  <CardContent className="p-4 flex flex-col gap-3">

    {/* Main Row */}
    <div className="flex items-center gap-3">
      {/* Zone 1: Avatar */}
      ...

      {/* Zone 2: Content */}
      <div className="flex-1 min-w-0">
        {/* Row A: Title + Badge */}
        {/* Row B: Subtitle (optional) */}
        {/* Row C: Metadata */}
        {/* Row D: Progress (optional, Learning Path only) */}
      </div>

      {/* Zone 3: Action Zone */}
      ...
    </div>

    {/* Action Row (optional) */}
    ...

  </CardContent>
</Card>
```

**Quy tắc cứng:**
- `group` bắt buộc trên `<Card>` để dùng `group-hover:` và `group-hover:opacity-100`
- `hover:border-primary/50 transition-colors` bắt buộc trên `<Card>`
- `p-4` trên `<CardContent>` — không dùng `p-0` rồi nest thêm `p-6` bên trong
- Main row: `flex items-center gap-3` — **dùng `items-center`** thay vì `items-start`
- Content zone: `flex-1 min-w-0` — `min-w-0` bắt buộc để `truncate` hoạt động

---

## 9. Chuẩn Pagination

**Tất cả các trang có pagination phải dùng cùng 1 pattern:**

```tsx
{totalPages > 1 && (
  <div className="flex items-center justify-center gap-2 pt-6">
    <Button variant="outline" size="icon"
      onClick={() => setPage(p => Math.max(1, p - 1))}
      disabled={page === 1}
      className="h-9 w-9 rounded-lg border-border hover:bg-muted">
      <ChevronLeft className="h-4 w-4" />
    </Button>

    {/* Page numbers (hiện khi totalPages ≤ 7) */}
    <div className="flex items-center gap-1">
      {Array.from({ length: totalPages }, (_, i) => i + 1).map(p => (
        <Button key={p} size="sm"
          variant={page === p ? "default" : "outline"}
          onClick={() => setPage(p)}
          className={`h-9 w-9 rounded-lg transition-colors ${
            page === p
              ? "bg-primary text-primary-foreground font-bold"
              : "border-border hover:bg-muted text-muted-foreground"
          }`}>
          {p}
        </Button>
      ))}
    </div>

    {/* Text fallback (hiện khi totalPages > 7) */}
    {totalPages > 7 && (
      <span className="text-sm font-medium text-muted-foreground">
        Page {page} of {totalPages}
      </span>
    )}

    <Button variant="outline" size="icon"
      onClick={() => setPage(p => Math.min(totalPages, p + 1))}
      disabled={page === totalPages}
      className="h-9 w-9 rounded-lg border-border hover:bg-muted">
      <ChevronRight className="h-4 w-4" />
    </Button>
  </div>
)}
```

---

## 10. Chuẩn Empty State

**Tất cả trang phải dùng component `<EmptyState>` có sẵn. Không custom inline.**

```tsx
<EmptyState
  title="No saved jobs yet"
  description="Keep track of jobs you're interested in by clicking the save icon."
  icon={<Bookmark className="w-10 h-10 text-muted-foreground/40" />}
>
  <Link href="/candidate/jobs">
    <Button className="mt-4 gap-2">
      <Search className="h-4 w-4" /> Browse Jobs
    </Button>
  </Link>
</EmptyState>
```

---

## 11. Chuẩn Page Header

**Tất cả các trang list dùng cùng pattern header:**

```tsx
{/* Simple header (không có CTA button) */}
<div>
  <h1 className="text-2xl font-bold tracking-tight">Your Saved Jobs</h1>
  <p className="text-muted-foreground mt-1">
    You have saved {count} jobs
  </p>
</div>

{/* Header với CTA button */}
<div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
  <div>
    <h1 className="text-2xl font-bold tracking-tight">AI Mock Interview</h1>
    <p className="text-muted-foreground mt-1 max-w-2xl">
      Description text here...
    </p>
  </div>
  <Button className="gap-2 shrink-0">
    <Plus className="h-4 w-4" /> Start New
  </Button>
</div>
```

**Quy tắc:**
- Tiêu đề trang: `text-2xl font-bold tracking-tight` (không dùng `text-3xl`)
- Mô tả: `text-muted-foreground mt-1`
- CTA Button: gradient chỉ dùng cho tính năng AI (`bg-gradient-to-r from-blue-600 to-blue-400 ...`)
- Trang không có AI feature: dùng `<Button>` default

---

## 12. Skeleton Loading Pattern

```tsx
// Mỗi trang nên có skeleton giống cấu trúc card thật
const CardSkeleton = () => (
  <Card>
    <CardContent className="p-4">
      <div className="flex items-center gap-3">
        <Skeleton className="w-11 h-11 rounded-lg shrink-0" />
        <div className="flex-1 space-y-2">
          <Skeleton className="h-4 w-1/2" />
          <Skeleton className="h-3 w-1/3" />
          <Skeleton className="h-3 w-2/3" />
        </div>
        <Skeleton className="h-8 w-8 rounded-lg shrink-0" />
      </div>
    </CardContent>
  </Card>
);

// Render 3-5 skeletons trong loading state
{isLoading && (
  <div className="flex flex-col gap-3">
    {[1, 2, 3].map(n => <CardSkeleton key={n} />)}
  </div>
)}
```

---

## 13. Design Token Summary

```
Avatar size:         w-11 h-11
Avatar shape:        rounded-lg (KHÔNG được dùng rounded-full)
Avatar gap:          gap-3 (giữa avatar và content)
Content text title:  font-semibold text-base text-foreground
Content text sub:    text-sm text-muted-foreground
Metadata text:       text-xs text-muted-foreground
Metadata icon:       h-3 w-3 (12px)
Action icon:         h-4 w-4 (16px)
Badge size:          text-[10px] px-1.5 py-0
Card hover:          hover:border-primary/50 transition-colors
Card group:          group (bắt buộc)
Card padding:        p-4 (trên CardContent)
Row gap:             gap-3 (main row), gap-2 (action row buttons)
Action row sep:      border-t border-border/50 pt-2.5
Delete button:       ghost + hover:text-destructive + opacity-0 group-hover:opacity-100
ArrowRight:          h-4 w-4 text-primary + group-hover:translate-x-1 transition-transform
```

---

## 14. Anti-patterns — Lệnh cấm tuyệt đối

```
❌  rounded-full trên avatar block
❌  w-16 h-16 hoặc w-10 h-10 cho avatar — chỉ dùng w-11 h-11
❌  items-start trên main row — phải là items-center
❌  Hardcode hex: bg-[#E6F4EA], text-[#137333], text-blue-600, bg-zinc-100...
❌  <span> thủ công cho badge — phải dùng <Badge> từ shadcn
❌  text-3xl cho tiêu đề trang — chỉ text-2xl
❌  hover:shadow-md trên card — chỉ dùng hover:border-primary/50
❌  p-0 trên CardContent rồi nest padding bên trong
❌  Action zone đặt cục bộ thành flex-col items-end ở bên phải (Applications hiện tại)
❌  HTML <button> thuần — phải dùng <Button> từ shadcn
❌  Thiếu min-w-0 trên content zone (gây lỗi truncate)
❌  Dialog confirm delete dùng window.confirm() — phải dùng Dialog từ shadcn
```

---

## 15. Lộ trình Refactor (Thứ tự ưu tiên)

### Phase 1 — Cao nhất (visible, nhiều người dùng)
1. `SavedJobCard.tsx` — Chuẩn hóa avatar size từ `w-12` → `w-11`, thêm `items-center` main row
2. `applications/page.tsx` — Refactor inline card, chuẩn hóa avatar, sửa action zone
3. `cv-matching/page.tsx` — Sửa Badge (bỏ inline span), chuẩn hóa Score circle

### Phase 2 — Trung bình
4. `learning-path/page.tsx` — Đổi avatar từ `rounded-full w-10` → `rounded-lg w-11`, sửa Badge hardcode hex
5. `interview/page.tsx` — Đổi avatar từ `rounded-full w-10` → `rounded-lg w-11`

### Phase 3 — Thấp hơn (ít ảnh hưởng UX)
6. `CvCard.tsx` — Thêm border cho avatar, chuẩn hóa action button
7. Chuẩn hóa Pagination component (extract thành `<ListPagination>` dùng chung)
8. Chuẩn hóa `CardSkeleton` per-page

---

## 16. Checkpoint Validate sau refactor

```
[ ] Avatar tất cả card: w-11 h-11 rounded-lg
[ ] Avatar có ảnh thật: thêm border + bg-muted
[ ] Avatar icon: không có border, bg màu phù hợp
[ ] Main row: flex items-center gap-3
[ ] Content zone: flex-1 min-w-0
[ ] Title: font-semibold text-base text-foreground group-hover:text-primary
[ ] Badge: dùng <Badge> shadcn, text-[10px] px-1.5 py-0 border-none
[ ] Badge màu: dùng semantic token (không hardcode hex)
[ ] Metadata: text-xs text-muted-foreground, icon h-3 w-3
[ ] Card: group hover:border-primary/50 transition-colors
[ ] Card padding: p-4 trên CardContent
[ ] Action Row (nếu có): border-t border-border/50 pt-2.5
[ ] Delete: Dialog shadcn (không dùng confirm())
[ ] Pagination: dùng chuẩn Section 9
[ ] Empty state: dùng <EmptyState> component
[ ] Không còn hardcode hex
[ ] Không còn rounded-full trên avatar
[ ] Không còn hover:shadow-md trên card
```
