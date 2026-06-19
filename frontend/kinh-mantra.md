kiểm tra 1 lần nữa:
# ITHunterview — Frontend Architecture Rules
> Context cho AI codegen. Next.js 16 App Router · TypeScript strict · shadcn/ui · TanStack Query · Zustand (auth/ui only)

## 0. CORE CONSTRAINTS — tuân thủ tuyệt đối, không ngoại lệ

1. **Luồng dữ liệu cố định (không nhảy tầng):**
   `page.tsx → hook → service → api-client → backend`
   - Page: chỉ gọi hook, KHÔNG gọi axios trực tiếp.
   - Hook: chỉ gọi service, KHÔNG render JSX.
   - Service: chỉ gọi axios, KHÔNG có useState/router/JSX/toast.
2. **5 route groups theo role:** `(public)` Guest · `(auth)` login/register chung · `(candidate)` · `(recruiter)` · `(staff)` · `(admin)`.
   - Mỗi group (trừ `(public)`, `(auth)`) có `layout.tsx` riêng làm auth guard theo role.
   - Trang trong `(public)` KHÔNG import hook/service yêu cầu auth token.
3. **State management:**
   - TanStack Query: mọi server state (data, loading, error, cache).
   - Zustand: CHỈ `auth.store.ts` (session/role) và `ui.store.ts` (sidebar, global notif). Không tạo store khác.
4. **shadcn/ui:** chỉ thêm bằng `npx shadcn add`. KHÔNG sửa tay file trong `components/ui/`.
5. **Component mới ở `components/shared/`:** chỉ tạo khi dùng ≥3 nơi (Rule of 3). Ngoại lệ luôn extract: Sidebar, Header, layout shell.
6. **Naming chuẩn:** `[domain].service.ts` · `use[Feature].ts` · `PascalCase.tsx` (component) · `[domain].types.ts`.

## 1. Folder Structure

```text
src/
├── app/
│   ├── (public)/        # Guest, no auth — home, jobs (public), pricing, apply-recruiter
│   ├── (auth)/           # login, register, forgot-password
│   ├── (candidate)/      # dashboard, profile, resumes, jobs, cv-optimizer, interviews, billing, notifications, settings
│   ├── (recruiter)/      # dashboard, company, profile, jobs, candidates, billing, notifications, settings
│   ├── (staff)/          # dashboard, ai-configs, prompts, question-bank, audit-logs, notifications, settings
│   ├── (admin)/          # dashboard, accounts, companies, master-data, subscriptions, finance, notifications, settings
│   ├── layout.tsx        # Root layout + Providers (Auth, QueryClient)
│   └── not-found.tsx
├── components/
│   ├── ui/               # shadcn auto-gen — READ ONLY
│   ├── layout/            # AppShell, GuestHeader, Sidebar, TopBar
│   ├── shared/            # JobCard, CvCard, CompanyCard, SkillBadge, StatusBadge, ConfirmDialog, EmptyState, PageLoader
│   └── forms/             # SkillSelector, RichTextEditor
├── services/              # 1 file = 1 domain, KHÔNG theo route group
│   ├── api-client.ts · auth.service.ts · public.service.ts · candidate.service.ts
│   ├── job.service.ts · ai.service.ts · billing.service.ts · notification.service.ts
│   └── recruiter.service.ts · staff.service.ts · admin.service.ts
├── hooks/                 # use[Feature].ts — wrap service + TanStack Query
├── types/                 # [domain].types.ts — map 1-1 DB entities; + api.types.ts (ApiResponse<T>, PaginatedResponse<T>)
├── lib/                   # utils.ts, validators.ts (zod), constants.ts (APP_ROUTES, ROLE_MENUS)
└── store/                 # auth.store.ts, ui.store.ts — ONLY these 2
```

**Lưu ý domain dùng chéo route group:** `job.service.ts` dùng cho cả Guest/Candidate/Recruiter (khác hàm: `getPublicList()` vs `getRecommended()` vs `createJob()`). `CompanyCard` dùng ở `(public)`, `(recruiter)`, `(admin)`.

## 2. Phân tầng trách nhiệm

| Layer | Được làm | KHÔNG được làm |
|---|---|---|
| `app/` (pages) | render page, gọi hook | gọi axios, business logic, CSS dài |
| `components/ui/` | — (auto-gen) | sửa tay |
| `components/shared/` | nhận props, render UI | gọi API, useEffect fetch, state phức tạp |
| `services/` | axios call, transform response, throw typed error | useState, router, toast, JSX |
| `hooks/` | gọi service, quản lý loading/error/data, expose cho page | render JSX, axios trực tiếp |
| `types/` | interface/type | logic |
| `lib/` | pure function (format, validate, constants) | side-effect, API call |
| `store/` | auth session/role, UI global state | mọi state khác (tránh thành Redux thứ 2) |

## 3. API Pattern chuẩn (canonical example — generalize cho mọi domain, không copy nguyên tên hàm)

```typescript
// services/api-client.ts — instance duy nhất
import axios from 'axios';
import { authStore } from '@/store/auth.store';

const api = axios.create({ baseURL: process.env.NEXT_PUBLIC_API_URL, timeout: 15000 });

api.interceptors.request.use((config) => {
  const token = authStore.getState().accessToken; // rỗng với Guest — interceptor tự bỏ qua
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

api.interceptors.response.use(
  (res) => res,
  (error) => {
    if (error.response?.status === 401) {
      authStore.getState().logout();
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default api;
```

```typescript
// services/[domain].service.ts — mỗi hàm = 1 endpoint, trả typed data
import api from './api-client';
export const [domain]Service = {
  getAll: (params?: Params) => api.get<PaginatedResponse<T>>('/[resource]', { params }).then(r => r.data),
  getById: (id: string) => api.get<T>(`/[resource]/${id}`).then(r => r.data),
};
```

```typescript
// hooks/use[Feature].ts — wrap service bằng TanStack Query
import { useQuery } from '@tanstack/react-query';
export function use[Feature](params?: Params) {
  return useQuery({ queryKey: ['[resource]', params], queryFn: () => [domain]Service.getAll(params) });
}
```

```tsx
// app/(role)/[feature]/page.tsx — chỉ gọi hook, render
'use client';
export default function FeaturePage() {
  const { data, isLoading, isError } = use[Feature]();
  if (isLoading) return <PageLoader />;
  if (isError) return <EmptyState title="..." />;
  if (!data?.items.length) return <EmptyState title="..." />;
  return <div className="grid gap-4">{data.items.map(item => <ItemCard key={item.id} item={item} />)}</div>;
}
```

## 4. Naming Cheat Sheet

| Loại | Convention | Ví dụ |
|---|---|---|
| Pages | `page.tsx` | bắt buộc Next.js |
| Route groups | `(role)` lowercase | `(candidate)`, `(staff)` |
| Components | `PascalCase.tsx` | `JobCard.tsx` |
| Hooks | `use[Feature].ts` | `useJobs.ts` |
| Services | `[domain].service.ts` | `job.service.ts` |
| Types | `[domain].types.ts` | `job.types.ts` |
| Constants | `SCREAMING_SNAKE_CASE` | `APP_ROUTES` |

## 5. Vibe-code Prompt Template

Khi sinh code mới cho 1 màn hình, điền và tuân theo:
Màn hình
Route group: [public|auth|candidate|recruiter|staff|admin]

Feature: [tên]

File: src/app/(role)/[feature]/page.tsx

Auth required: [Không | Có — role: x]
API Endpoints
[method] [path] → Response: [shape]
Types
interface [Domain] { ... }
UI/UX
[mô tả ngắn: layout, state hiển thị, action]
Output bắt buộc — đúng thứ tự

src/types/[domain].types.ts
src/services/[domain].service.ts (axios only, no state/UI)
src/hooks/use[Feature].ts (TanStack Query)
src/components/shared/[Item]Card.tsx (props only)
src/app/(role)/[feature]/page.tsx ('use client', gọi hook, render)

Constraints: dùng shadcn/ui components có sẵn, dùng cn() từ @/lib/utils,

named export cho component, default export cho page.

Nếu route group = public → KHÔNG import hook/service cần auth token.

Không tạo folder/pattern ngoài cấu trúc đã quy định ở mục 1.

## 6. Pre-commit Checklist

- [ ] File đúng folder, đúng route group theo role?
- [ ] Service không chứa useRouter/useState/JSX?
- [ ] Component không gọi axios trực tiếp?
- [ ] Type đã khai báo trong `types/`?
- [ ] `'use client'` đã thêm nếu cần state?
- [ ] Page `(public)` không import hook/service cần auth token?
- [ ] Component mới chỉ tạo khi dùng ≥3 nơi (trừ layout)?
