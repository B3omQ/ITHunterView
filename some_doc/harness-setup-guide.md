# Harness Engineering — Hướng dẫn Setup cho Coding Agent

> Tài liệu này được tổng hợp từ [Learn Harness Engineering](https://walkinglabs.github.io/learn-harness-engineering/vi/) và các nghiên cứu từ OpenAI & Anthropic. Mục đích: giúp coding agent (Antigravity IDE hoặc tương đương) đọc, hiểu và áp dụng vào phát triển phần mềm một cách đáng tin cậy.

---

## Mục lục

1. [Harness là gì và tại sao cần nó](#1-harness-là-gì-và-tại-sao-cần-nó)
2. [5 Hệ thống phụ của một Harness hoàn chỉnh](#2-5-hệ-thống-phụ-của-một-harness-hoàn-chỉnh)
3. [Cấu trúc file bắt buộc](#3-cấu-trúc-file-bắt-buộc)
4. [Template AGENTS.md (file quan trọng nhất)](#4-template-agentsmd-file-quan-trọng-nhất)
5. [Template feature_list.json](#5-template-feature_listjson)
6. [Template claude-progress.md](#6-template-claude-progressmd)
7. [Script khởi động init.sh](#7-script-khởi-động-initsh)
8. [Quy trình làm việc của Agent theo từng phiên](#8-quy-trình-làm-việc-của-agent-theo-từng-phiên)
9. [Định nghĩa Hoàn thành (Definition of Done)](#9-định-nghĩa-hoàn-thành-definition-of-done)
10. [Các lỗi phổ biến và cách phòng tránh](#10-các-lỗi-phổ-biến-và-cách-phòng-tránh)
11. [Vòng lặp chẩn đoán khi có sự cố](#11-vòng-lặp-chẩn-đoán-khi-có-sự-cố)
12. [Checklist Setup nhanh](#12-checklist-setup-nhanh)

---

## 1. Harness là gì và tại sao cần nó

**Harness = tất cả mọi thứ bên ngoài trọng số mô hình AI.**

Nếu không phải là model weights, thì đó là harness: file hướng dẫn, môi trường thực thi, lệnh xác minh, trạng thái tiến độ, cấu trúc thư mục.

### Tại sao model mạnh vẫn thất bại không có Harness

| Vấn đề | Nguyên nhân | Lớp Harness cần sửa |
|--------|------------|---------------------|
| Agent làm sai yêu cầu | Task không được đặc tả rõ | Hệ thống Hướng dẫn |
| Agent vi phạm convention của dự án | Không biết các quy tắc ẩn | Hệ thống Hướng dẫn |
| Agent tốn thời gian tìm hiểu cấu trúc | Thiếu context về repo | Hệ thống Hướng dẫn |
| Agent báo "xong" nhưng code lỗi | Không có lệnh xác minh | Hệ thống Phản hồi |
| Phiên mới phải làm lại từ đầu | Không lưu trạng thái | Hệ thống Trạng thái |
| Dependency conflict, môi trường sai | Môi trường không chuẩn | Hệ thống Môi trường |

### Bằng chứng thực nghiệm

Anthropic đã chạy cùng một prompt ("xây dựng trò chơi retro 2D"), cùng model (Opus 4.5):
- **Không có Harness**: 20 phút, $9, tính năng cốt lõi không hoạt động
- **Có Harness đầy đủ**: 6 giờ, $200, trò chơi có thể chơi được

OpenAI dùng Codex xây dựng sản phẩm nội bộ với ~1 triệu dòng code, 3 kỹ sư, ~1500 PR trong 5 tháng — **không ai viết code trực tiếp**, tất cả qua agent có harness.

> **Nguyên tắc cốt lõi**: Khi có sự cố, kiểm tra harness trước, không phải đổi model.

---

## 2. 5 Hệ thống phụ của một Harness hoàn chỉnh

```
┌─────────────────────────────────────────────────────────┐
│                    CODING AGENT                          │
│                                                         │
│  ┌──────────────┐    ┌──────────────┐                  │
│  │ 1. HƯỚNG DẪN │    │ 2. CÔNG CỤ  │                  │
│  │  AGENTS.md   │    │ shell, git,  │                  │
│  │  CLAUDE.md   │    │ file access  │                  │
│  └──────────────┘    └──────────────┘                  │
│                                                         │
│  ┌──────────────┐    ┌──────────────┐                  │
│  │ 3. MÔI TRƯỜNG│    │  4. TRẠNG   │                  │
│  │ pyproject,   │    │   THÁI      │                  │
│  │ .nvmrc,      │    │progress.md  │                  │
│  │ Docker       │    │feature_list │                  │
│  └──────────────┘    └──────────────┘                  │
│                                                         │
│  ┌──────────────────────────────────┐                  │
│  │         5. PHẢN HỒI             │                  │
│  │  pytest, mypy, ruff, make check  │                  │
│  └──────────────────────────────────┘                  │
└─────────────────────────────────────────────────────────┘
```

### Hệ thống 1 — Hướng dẫn (kệ công thức)

File `AGENTS.md` hoặc `CLAUDE.md` ở thư mục gốc. Không phải bách khoa toàn thư — chỉ khoảng **100 dòng**, đủ để agent không phải đoán bất cứ điều gì thiết yếu.

### Hệ thống 2 — Công cụ (giá dao)

Agent phải có quyền truy cập shell, git, và file system. Không vô hiệu hóa shell vì "bảo mật" — nhưng áp dụng nguyên tắc ít đặc quyền nhất.

### Hệ thống 3 — Môi trường (bếp)

Môi trường phải **tự mô tả và tái tạo được**:
- `pyproject.toml` / `package.json` để lock dependencies
- `.python-version` / `.nvmrc` để lock runtime versions
- `Dockerfile` hoặc devcontainer để tái tạo đầy đủ

### Hệ thống 4 — Trạng thái (bàn chuẩn bị)

Các tác vụ dài cần ghi lại tiến độ. File `claude-progress.md` ghi: đã xong gì, đang làm gì, bị chặn ở đâu.

### Hệ thống 5 — Phản hồi / Xác minh (cửa sổ kiểm tra chất lượng) ⭐

**Đây là hệ thống có ROI cao nhất.** Liệt kê rõ ràng các lệnh xác minh. Không có hệ thống này, agent tự đánh giá công việc của mình và luôn nói "xong."

---

## 3. Cấu trúc file bắt buộc

Đặt các file này ở **thư mục gốc** của repository:

```
your-project/
├── AGENTS.md              ← Hướng dẫn chính cho agent (hoặc CLAUDE.md)
├── feature_list.json      ← Danh sách tính năng và trạng thái
├── claude-progress.md     ← Nhật ký tiến độ theo phiên
├── init.sh                ← Script khởi động và xác minh môi trường
├── session-handoff.md     ← (Tùy chọn) Bàn giao phiên ngắn gọn
│
├── docs/                  ← Tài liệu chi tiết hơn (AGENTS.md link tới đây)
│   ├── architecture.md
│   ├── api-conventions.md
│   └── decisions/         ← Bản ghi quyết định kiến trúc (ADRs)
│
└── src/                   ← Code của dự án
```

> **Nguyên tắc "Repo là nguồn sự thật duy nhất"**: Bất cứ thứ gì agent không thể nhìn thấy trong repo, theo mọi nghĩa thực tế, không tồn tại.

---

## 4. Template AGENTS.md (file quan trọng nhất)

Sao chép file này vào gốc repo, thay thế các placeholder `[...]`:

```markdown
# AGENTS.md

Kho lưu trữ này được thiết kế cho công việc coding-agent chạy lâu.
Mục tiêu: để lại repo ở trạng thái mà phiên tiếp theo có thể tiếp tục mà không cần đoán.

## Dự án

[Mô tả dự án trong 1-2 câu. Ví dụ: "API backend quản lý đơn hàng cho hệ thống thương mại điện tử."]

## Tech Stack

- **Ngôn ngữ**: [Python 3.11 / TypeScript 5.x / ...]
- **Framework**: [FastAPI 0.100+ / Next.js 14 / ...]
- **Database**: [PostgreSQL 15 / MongoDB 7 / ...]
- **Package Manager**: [pip + pyproject.toml / npm / yarn / pnpm]
- **Test**: [pytest / jest / vitest]
- **Lint/Format**: [ruff + mypy / eslint + prettier]

## Lệnh Xác minh (QUAN TRỌNG — chạy trước khi báo hoàn thành)

```bash
# Chạy toàn bộ kiểm tra
make check

# Hoặc từng bước:
pytest tests/ -x                    # Unit tests
mypy src/ --strict                  # Type checking
ruff check src/                     # Linting

# Kiểm tra môi trường hoạt động
./init.sh
```

## Quy trình Khởi động (Đầu mỗi phiên)

1. Xác nhận thư mục: `pwd`
2. Đọc `claude-progress.md` — trạng thái đã xác minh mới nhất
3. Đọc `feature_list.json` — chọn tính năng ưu tiên cao nhất chưa hoàn thành
4. Xem commit gần đây: `git log --oneline -5`
5. Chạy `./init.sh`
6. Chạy smoke test trước khi bắt đầu công việc mới

**Nếu xác minh baseline thất bại → sửa trước, không chồng tính năng mới lên.**

## Quy tắc Làm việc

- Làm một tính năng tại một thời điểm
- Không đánh dấu hoàn thành chỉ vì code đã được thêm — phải chạy xác minh
- Giữ thay đổi trong phạm vi tính năng đã chọn
- Không thay đổi ngầm quy tắc xác minh trong khi triển khai
- Ưu tiên artifact repo lâu dài hơn tóm tắt trong chat

## Ràng buộc Kiến trúc (Không thương lượng)

- [Ví dụ: Tất cả API endpoint phải dùng OAuth 2.0]
- [Ví dụ: Không dùng `any` trong TypeScript]
- [Ví dụ: Mọi function công khai phải có docstring]
- [Ví dụ: Database query phải dùng parameterized query, không string concatenation]

## Quy ước Code

- [Ví dụ: Đặt tên file: snake_case cho Python, kebab-case cho TypeScript]
- [Ví dụ: Component React: PascalCase, một file mỗi component]
- [Ví dụ: API response format: `{ data: ..., error: null }` hoặc `{ data: null, error: "..." }`]

## Tài liệu chi tiết hơn

- Kiến trúc tổng thể: `docs/architecture.md`
- Quy ước API: `docs/api-conventions.md`
- Lịch sử quyết định: `docs/decisions/`

## Cuối Phiên (Trước khi kết thúc)

1. Cập nhật `claude-progress.md`
2. Cập nhật `feature_list.json`
3. Ghi lại rủi ro hoặc blocker chưa giải quyết
4. Commit với message mô tả khi ở trạng thái an toàn
5. Đảm bảo phiên tiếp theo có thể chạy `./init.sh` ngay lập tức
```

---

## 5. Template feature_list.json

File này là **nguồn sự thật duy nhất** cho trạng thái tính năng. Agent đọc đây để biết phải làm gì tiếp theo.

```json
{
  "project": "Tên dự án",
  "last_updated": "2025-01-15",
  "features": [
    {
      "id": "feat-001",
      "name": "User Authentication",
      "priority": "high",
      "status": "done",
      "verification": "pytest tests/auth/ -v → PASSED (2025-01-14)",
      "notes": "JWT với refresh token, OAuth2 Google"
    },
    {
      "id": "feat-002",
      "name": "Product Listing API",
      "priority": "high",
      "status": "in_progress",
      "verification": null,
      "notes": "GET /api/v1/products đã xong, cần thêm pagination và filter"
    },
    {
      "id": "feat-003",
      "name": "Order Management",
      "priority": "medium",
      "status": "pending",
      "verification": null,
      "notes": "Chờ feat-002 hoàn thành"
    },
    {
      "id": "feat-004",
      "name": "Payment Integration",
      "priority": "low",
      "status": "pending",
      "verification": null,
      "notes": "VNPay + Stripe"
    }
  ],
  "status_legend": {
    "pending": "Chưa bắt đầu",
    "in_progress": "Đang làm",
    "done": "Hoàn thành và đã xác minh",
    "blocked": "Bị chặn, cần xử lý"
  }
}
```

**Quy tắc quan trọng**: Status chỉ được chuyển sang `"done"` khi trường `verification` có bằng chứng cụ thể (lệnh đã chạy + kết quả).

---

## 6. Template claude-progress.md

Nhật ký tiến độ phiên. Agent **đọc lúc bắt đầu**, **viết lúc kết thúc** mỗi phiên.

```markdown
# Nhật ký Tiến độ

## Trạng thái Đã Xác minh Mới nhất

**Ngày**: 2025-01-15  
**Phiên**: #4  
**Trạng thái baseline**: ✅ PASS — `./init.sh && make check` đều pass

### Đã hoàn thành trong phiên này
- [x] Thêm endpoint GET /api/v1/products với pagination
- [x] Viết test `tests/products/test_listing.py` — 12 test cases pass
- [x] Xác minh: `pytest tests/products/ -v` → PASSED

### Đang tiến hành
- [ ] Filter theo category và price range (50% xong)
  - File: `src/api/products/filters.py`
  - Vấn đề còn lại: full-text search chưa implement

### Bị chặn / Rủi ro
- Chưa có: không có blocker

### Bước tiếp theo (Phiên kế tiếp bắt đầu từ đây)
1. Hoàn thành filter full-text search trong `src/api/products/filters.py`
2. Thêm test cho filter
3. Cập nhật `feature_list.json` feat-002 thành "done"
4. Bắt đầu feat-003 Order Management

---

## Lịch sử Phiên

### Phiên #3 (2025-01-14)
- Hoàn thành Authentication (feat-001)
- Bắt đầu Product Listing API

### Phiên #2 (2025-01-12)
- Setup cơ bản: database models, migration
- Cấu hình môi trường Docker

### Phiên #1 (2025-01-10)
- Khởi tạo project, setup CI/CD
```

---

## 7. Script khởi động init.sh

Script này đảm bảo môi trường sẵn sàng trước khi agent bắt đầu làm việc.

```bash
#!/bin/bash
# init.sh — Khởi động và xác minh môi trường
# Agent chạy script này đầu mỗi phiên

set -e  # Dừng nếu bất kỳ lệnh nào thất bại

echo "=== Khởi động môi trường ==="

# 1. Xác nhận vị trí
echo "📁 Thư mục hiện tại: $(pwd)"

# 2. Kiểm tra runtime version
echo "🐍 Python: $(python --version)"
# Hoặc: echo "📦 Node: $(node --version)"

# 3. Cài đặt dependencies
echo "📦 Cài đặt dependencies..."
pip install -e ".[dev]" --quiet
# Hoặc: npm ci

# 4. Kiểm tra kết nối database (nếu có)
# python -c "from src.db import engine; engine.connect(); print('✅ Database OK')"

# 5. Chạy smoke test nhanh
echo "🔥 Smoke test..."
pytest tests/smoke/ -x -q 2>/dev/null || pytest tests/ -x -q --co -q 2>/dev/null | head -5

echo ""
echo "=== Môi trường sẵn sàng ==="
echo "💡 Lệnh xác minh đầy đủ: make check"
echo "📋 Trạng thái tiến độ: cat claude-progress.md"
echo "📊 Tính năng cần làm: cat feature_list.json | python -m json.tool"
```

Cấp quyền thực thi: `chmod +x init.sh`

---

## 8. Quy trình làm việc của Agent theo từng phiên

### 🟢 Bắt đầu phiên

```
1. pwd                                    ← Xác nhận vị trí
2. cat claude-progress.md                 ← Đọc trạng thái
3. cat feature_list.json                  ← Chọn tính năng tiếp theo
4. git log --oneline -5                   ← Xem commit gần đây
5. ./init.sh                              ← Khởi động môi trường
6. make check (hoặc pytest + mypy + ruff) ← Xác minh baseline
```

**Nếu bước 6 thất bại → sửa ngay, không bắt đầu tính năng mới.**

### 🔵 Trong phiên làm việc

```
Vòng lặp chính:
  - Chọn 1 tính năng từ feature_list.json (ưu tiên cao + pending/in_progress)
  - Viết code
  - Chạy xác minh: pytest + mypy + ruff
  - Nếu pass → cập nhật feature_list.json status
  - Nếu fail → sửa, không báo hoàn thành
  - Commit khi ở trạng thái ổn định
```

### 🔴 Kết thúc phiên

```
1. Chạy make check một lần cuối           ← Xác minh toàn bộ
2. Cập nhật claude-progress.md            ← Ghi lại đã xong gì
3. Cập nhật feature_list.json             ← Cập nhật trạng thái tính năng
4. git add -A && git commit -m "..."      ← Commit ở trạng thái sạch
5. Đảm bảo ./init.sh có thể chạy ngay    ← Kiểm tra phiên tiếp theo
```

---

## 9. Định nghĩa Hoàn thành (Definition of Done)

**Không bao giờ báo hoàn thành chỉ vì code đã được thêm.**

Một tính năng chỉ được đánh dấu `"done"` khi **tất cả** điều sau đúng:

```
✅ Hành vi mục tiêu đã được triển khai
✅ Lệnh xác minh đã chạy và có bằng chứng pass
✅ Bằng chứng đã ghi vào feature_list.json hoặc claude-progress.md
✅ Repo vẫn có thể khởi động lại từ ./init.sh
✅ Không có regression (test cũ vẫn pass)
```

### Ví dụ Định nghĩa Hoàn thành tốt

```
Tính năng: Thêm Search API

Tiêu chí hoàn thành:
- Endpoint GET /api/v1/search?q=... hoạt động
- Hỗ trợ pagination (default 20 items/page)
- Kết quả có snippet được highlight
- pytest tests/search/ -v → tất cả PASS
- mypy src/api/search.py --strict → no errors
- ruff check src/api/search.py → no issues
```

---

## 10. Các lỗi phổ biến và cách phòng tránh

### ❌ "Context Anxiety" — Agent vội vàng vì context gần đầy

**Triệu chứng**: Agent bỏ qua xác minh, chọn giải pháp đơn giản hơn, báo "xong" sớm.

**Phòng tránh**: 
- Chia tính năng lớn thành tasks nhỏ hơn trong `feature_list.json`
- Mỗi task phải hoàn thành và xác minh được trong một phiên
- Không đặt quá nhiều mục "in_progress" cùng lúc

### ❌ Agent "khám phá" mà không làm việc

**Triệu chứng**: Agent tốn 40%+ context để đọc file, hiểu cấu trúc.

**Phòng tránh**:
- `AGENTS.md` mô tả rõ cấu trúc thư mục quan trọng
- `init.sh` tự động in ra thông tin cần thiết
- `claude-progress.md` ghi lại file nào đang được làm việc

### ❌ Agent vi phạm convention mà không biết

**Triệu chứng**: Code dùng SQLAlchemy 1.x trong khi dự án dùng 2.x; không theo error handling pattern.

**Phòng tránh**:
- Liệt kê rõ phiên bản trong `AGENTS.md`
- Thêm section "Ràng buộc Kiến trúc" với ví dụ cụ thể
- Link tới file ví dụ: "Xem cách dùng ORM tại `src/examples/db_usage.py`"

### ❌ Mỗi phiên mới làm lại từ đầu

**Triệu chứng**: Agent không biết đã xong gì, hỏi lại các câu đã trả lời.

**Phòng tránh**:
- Duy trì `claude-progress.md` nghiêm túc
- Commit thường xuyên với message mô tả
- `init.sh` in ra "Bước tiếp theo" từ progress file

### ❌ Một file hướng dẫn khổng lồ

**Triệu chứng**: `AGENTS.md` dài 1000+ dòng, agent bỏ qua hoặc overwhelm.

**Phòng tránh**:
- `AGENTS.md` tối đa ~100 dòng
- Chi tiết hơn → đặt trong `docs/` và link từ AGENTS.md
- Agent đọc theo nhu cầu, không phải toàn bộ

---

## 11. Vòng lặp chẩn đoán khi có sự cố

Khi agent thất bại, không đổi model ngay. Ánh xạ lỗi vào 5 lớp:

```
Lỗi xảy ra
    │
    ▼
[1] Task không rõ ràng?
    → Sửa: Thêm tiêu chí cụ thể vào AGENTS.md / task description
    │
    ▼
[2] Thiếu context về dự án?
    → Sửa: Cập nhật AGENTS.md, thêm vào docs/, hoặc thêm ví dụ code
    │
    ▼
[3] Môi trường thực thi lỗi?
    → Sửa: Cập nhật init.sh, lock dependencies, cải thiện Docker setup
    │
    ▼
[4] Không có cách xác minh?
    → Sửa: Thêm/cải thiện lệnh test, lint, type check vào AGENTS.md
    │
    ▼
[5] Mất trạng thái giữa các phiên?
    → Sửa: Cải thiện claude-progress.md, feature_list.json
```

**Sau mỗi lần sửa**: Ghi lại lớp nào đã sửa và kết quả. Sau vài vòng, bạn sẽ thấy lớp nào là bottleneck chính.

---

## 12. Checklist Setup nhanh

Sử dụng checklist này khi bắt đầu một dự án mới hoặc thêm harness vào dự án hiện có:

### Giai đoạn 1 — Tối thiểu (làm ngay, ~30 phút)

- [ ] Tạo `AGENTS.md` với tech stack, lệnh xác minh, ràng buộc cứng
- [ ] Tạo `feature_list.json` với danh sách tính năng cần làm
- [ ] Tạo `claude-progress.md` với trạng thái hiện tại
- [ ] Tạo `init.sh` và test thủ công: `chmod +x init.sh && ./init.sh`
- [ ] Đảm bảo có ít nhất 1 lệnh test chạy được (dù chỉ 1 test)

### Giai đoạn 2 — Ổn định (~2 giờ)

- [ ] Thêm `Makefile` với target `check` chạy test + lint + typecheck
- [ ] Lock dependencies (`pyproject.toml` hoặc `package-lock.json`)
- [ ] Thêm `.python-version` hoặc `.nvmrc`
- [ ] Viết ít nhất 1 smoke test kiểm tra app khởi động được
- [ ] Tạo `docs/architecture.md` với mô tả cấu trúc thư mục

### Giai đoạn 3 — Hoàn chỉnh (~4 giờ)

- [ ] Docker hoặc devcontainer để tái tạo môi trường
- [ ] `session-handoff.md` template cho phiên lớn
- [ ] `docs/decisions/` cho Architecture Decision Records
- [ ] CI/CD tự động chạy `make check` trên mỗi commit
- [ ] `evaluator-rubric.md` nếu dùng multi-agent (planner + evaluator)

---

## Tài liệu tham khảo

- [Learn Harness Engineering (Vietnamese)](https://walkinglabs.github.io/learn-harness-engineering/vi/)
- [OpenAI: Harness Engineering — Leveraging Codex](https://openai.com/index/harness-engineering/)
- [Anthropic: Effective Harnesses for Long-Running Agents](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents)
- [Anthropic: Harness Design for Long-Running App Development](https://www.anthropic.com/engineering/harness-design-long-running-apps)

---

*Tài liệu này được thiết kế để coding agent (Antigravity IDE hoặc tương đương) đọc trực tiếp và áp dụng. Cập nhật khi dự án thay đổi.*
