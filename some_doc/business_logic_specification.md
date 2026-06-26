# ĐẶC TẢ LOGIC NGHIỆP VỤ HỆ THỐNG
## Job Seeking and Mock Interview System for IT Candidates

> **Tài liệu này dành cho ai đọc?** Đây là tài liệu nguồn chính (single source of truth) về logic nghiệp vụ của toàn bộ hệ thống, viết để một coding agent (AI hoặc developer) có thể đọc và triển khai code chính xác mà không cần đoán hoặc hỏi lại. Mọi tên bảng, tên cột, giá trị enum trong tài liệu này đã được đồng bộ với `schema.sql` (PostgreSQL) — phiên bản tham chiếu là file schema 590 dòng sinh từ Eraser.io ERD.
>
> **Tài liệu liên quan:** `schema.sql` (cấu trúc bảng đầy đủ, nguồn chính xác nhất cho kiểu dữ liệu/constraint), `evaluation_criteria.docx` (framework đánh giá chất lượng AI — không cần đọc để code), bộ tiêu chí matching theo từng role (Dev/BA — đang xây dựng, sẽ nạp vào `cv_matching_rubrics`).
>
> **Quy ước:** `RULE:` = ràng buộc bắt buộc. `EDGE CASE:` = tình huống ngoại lệ bắt buộc xử lý. `SCHEMA GAP:` = tính năng cần thiết nhưng chưa có trong schema hiện tại, cần bổ sung trước khi implement. `OPEN ITEM:` = chưa chốt, hỏi lại trước khi code.

---

## MỤC LỤC

1. [Tổng quan hệ thống](#1-tổng-quan-hệ-thống)
2. [Actors và Phân quyền](#2-actors-và-phân-quyền)
3. [Bản đồ Module nghiệp vụ](#3-bản-đồ-module-nghiệp-vụ)
4. [Module: Authentication & Account](#4-module-authentication--account)
5. [Module: Profile & CV Management](#5-module-profile--cv-management)
6. [Module: AI CV-JD Matching](#6-module-ai-cv-jd-matching)
7. [Module: AI Mock Interview](#7-module-ai-mock-interview)
8. [Module: Job Discovery & Application](#8-module-job-discovery--application)
9. [Module: Recruiter & ATS](#9-module-recruiter--ats)
10. [Module: Admin Operations (Business)](#10-module-admin-operations-business)
11. [Module: Staff Operations (Technical AI Ops)](#11-module-staff-operations-technical-ai-ops)
12. [Module: Monetization](#12-module-monetization)
13. [Module: Notification System](#13-module-notification-system)
14. [Nguyên tắc thiết kế AI xuyên suốt](#14-nguyên-tắc-thiết-kế-ai-xuyên-suốt)
15. [Tổng hợp Enum Types từ Schema](#15-tổng-hợp-enum-types-từ-schema)
16. [Tổng hợp bảng theo phân hệ](#16-tổng-hợp-bảng-theo-phân-hệ)
17. [Schema Gaps — Cần bổ sung trước khi implement](#17-schema-gaps--cần-bổ-sung-trước-khi-implement)
18. [Quy tắc & Ràng buộc toàn hệ thống](#18-quy-tắc--ràng-buộc-toàn-hệ-thống)
19. [Open Items — Chưa chốt](#19-open-items--chưa-chốt)
20. [Glossary](#20-glossary)

---

## 1. TỔNG QUAN HỆ THỐNG

### 1.1 Hệ thống là gì

Nền tảng tuyển dụng trực tuyến chuyên biệt cho ngành IT tại Việt Nam, phục vụ đồng thời ứng viên (Candidate), nhà tuyển dụng (Recruiter), và đội vận hành nền tảng (Admin + Staff).

### 1.2 Triết lý AI-native

**RULE:** Mọi tính năng AI được thiết kế sao cho nếu bỏ AI ra, tính năng đó **không có phiên bản thay thế thủ công** — không phải kiểu "thêm nút AI" vào luồng nghiệp vụ sẵn có.

### 1.3 Ba tính năng AI cốt lõi

| # | Tính năng | Module |
|---|---|---|
| 1 | AI Mock Interview — AI đóng vai HR, sinh câu hỏi từ JD thực tế, dẫn dắt phỏng vấn thử | Mục 7 |
| 2 | AI Scoring — chấm điểm câu trả lời theo rubric, có giải thích (explainable) | Mục 7.8 |
| 3 | AI CV-JD Matching theo rubric riêng từng role nghề nghiệp | Mục 6 |

### 1.4 Out of scope (giai đoạn này)

- **AI sinh JD tự động cho Recruiter** — tạm hoãn. Mọi tham chiếu cũ đến tính năng này ở tài liệu khác → bỏ qua.

---

## 2. ACTORS VÀ PHÂN QUYỀN

### 2.1 Sơ đồ Actor

```
Guest (chưa đăng nhập)
  └──► đăng ký ──► Candidate  hoặc  Recruiter (qua KYC)

Vận hành nền tảng (tài khoản được tạo bởi Super Admin nội bộ):
  ├──► Admin   (nghiệp vụ — duyệt KYC, duyệt JD, master data, tài chính)
  └──► Staff   (kỹ thuật AI — API keys, prompts, rubric, question bank)

System (actor tự động — background jobs, không có giao diện)
```

**RULE:** `Admin` và `Staff` là **hai role tách biệt hoàn toàn** trong bảng `roles`. Một tài khoản `users` chỉ giữ đúng một `role_id`. Không được gộp quyền Admin + Staff vào cùng một tài khoản trong production.

### 2.2 Bảng quyền hạn

| Actor | Phạm vi | Module |
|---|---|---|
| **Guest** | Xem trang chủ, danh sách JD, chi tiết JD, bảng giá. Không ứng tuyển, không dùng AI | Mục 8 |
| **Candidate** | Profile, CV, Matching AI, Mock Interview, Job Discovery/Application, Monetization | Mục 5–8, 12 |
| **Recruiter** | Company Profile, Post Job, ATS Kanban, Active Sourcing, Monetization | Mục 9, 12 |
| **Admin** | KYC công ty, master data, subscription config, tài chính, quản lý user, broadcast notification | Mục 10 |
| **Staff** | AI config (API key, model, rate limit), system prompts (versioning), question bank, matching rubrics, audit log, chi phí token | Mục 11 |
| **System** | Gửi email/notification async, tăng `view_count`, trigger scoring queue, cleanup jobs | Mục 13 |

---

## 3. BẢN ĐỒ MODULE NGHIỆP VỤ

```
CANDIDATE
  Auth → Profile/CV → ┬─► AI CV-JD Matching ─► Optimize CV
                      │                      └─► Learning Pathway
                      ├─► AI Mock Interview  ─► Scoring Pipeline
                      └─► Job Discovery & Application

RECRUITER
  Auth → Company Profile (KYC) → Post Job → ATS Kanban
                                           └─► Active Sourcing

ADMIN        │  STAFF
Nghiệp vụ   │  Kỹ thuật AI
KYC/JD/data │  Prompts/Rubrics/Config
```

**Hai hệ rubric tách biệt — KHÔNG dùng lẫn:**

| Hệ rubric | Tính năng | Bảng | Quản lý bởi |
|---|---|---|---|
| Interview Rubric | AI Mock Interview scoring | `interview_question_bank` + system_prompts | Staff |
| Matching Rubric | AI CV-JD Matching | `cv_matching_rubrics` *(cần bổ sung — Mục 17)* | Staff |

---

## 4. MODULE: AUTHENTICATION & ACCOUNT

### 4.1 Đăng ký — Candidate

**Flow:**
1. Nhập email + password + role = CANDIDATE
2. Validate email chưa tồn tại (`users.email UNIQUE`)
3. Tạo `users` với `status = 'PENDING_VERIFICATION'` *(enum `user_status`)*
4. Tạo `candidate_profiles` liên kết 1-1 qua `user_id`
5. Sinh token → lưu `email_verification_tokens` (`token`, `expires_at`, `is_used = false`) → gửi email xác thực qua background job
6. User click link → verify token chưa dùng và chưa hết hạn → set `users.status = 'ACTIVE'`

**RULE:** Tài khoản `PENDING_VERIFICATION` bị từ chối đăng nhập hoàn toàn.

**EDGE CASE:** Token đã dùng hoặc hết hạn → trả lỗi rõ, cho phép gửi lại email xác thực mới (gen token mới, token cũ đánh dấu `is_used = true`).

### 4.2 Đăng ký — Recruiter (kèm KYC)

**Flow:**
1. Nhập thông tin cá nhân + thông tin công ty (`name`, `tax_code`, `verification_method`, file giấy tờ)
2. Tạo `users` với `status = 'PENDING_VERIFICATION'`
3. Tạo `companies` với `status = 'PENDING'`
4. Tạo `recruiter_profiles` liên kết `user_id` + `company_id`
5. Admin duyệt KYC (Mục 10.2) → sau khi approve mới được đăng tin

**RULE:** Recruiter bị chặn đăng tin khi `companies.status != 'VERIFIED'`, bất kể `users.status` là gì.

### 4.3 Đăng nhập

**Flow:**
1. Nhập email + password
2. Kiểm tra `users.status`:
   - `BANNED` → từ chối kèm lý do
   - `INACTIVE` → từ chối, thông báo tài khoản bị vô hiệu hóa
   - `PENDING_VERIFICATION` → từ chối, nhắc xác thực email
3. Verify password hash (bcrypt)
4. Ghi `user_activity_logs` với `action_category = 'AUTH'`, `status = 'SUCCESS'/'FAIL'`
5. Sinh JWT access token (TTL ngắn) + refresh token → lưu `refresh_tokens`

**RULE — Brute force:** Đọc `user_activity_logs` đếm số lần `action = 'LOGIN_FAILED'`, `status = 'FAIL'`, `user_id = X`, `created_at > NOW() - 15 min`. Nếu ≥ 5 lần → từ chối đăng nhập 15 phút tiếp theo dù nhập đúng password.

> **LƯU Ý:** Schema hiện tại không có bảng `login_attempts` riêng — logic brute force sẽ dựa vào `user_activity_logs` (đã có `action_category = 'AUTH'` và `status = 'SUCCESS'/'FAIL'`). Đây là trade-off giữa đơn giản schema vs performance query. Xem thêm Mục 17.

### 4.4 Đăng xuất

Set `refresh_tokens.is_revoked = true` cho token hiện tại. Access token JWT hết hạn tự nhiên.

### 4.5 Quên mật khẩu

Token một lần, lưu `password_resets` (`token`, `expires_at`, `is_used`). Sau khi reset → `is_used = true`.

### 4.6 Kích hoạt / Vô hiệu hóa tài khoản (Admin)

Schema `user_status` có 4 giá trị: `'ACTIVE'`, `'INACTIVE'`, `'BANNED'`, `'PENDING_VERIFICATION'`.

- **Deactivate (vô hiệu tạm thời):** set `status = 'INACTIVE'`, ghi `deactive_at = now()`
- **Reactivate:** set `status = 'ACTIVE'`, xóa `deactive_at`
- **Ban (vi phạm):** set `status = 'BANNED'` — đây là trạng thái vĩnh viễn hơn, cần Admin confirm

**RULE:** Khi ban/deactivate → revoke tất cả `refresh_tokens` của user đó (`is_revoked = true` cho mọi token của `user_id`).

---

## 5. MODULE: PROFILE & CV MANAGEMENT

### 5.1 Candidate Profile — cấu trúc bảng

Schema có **4 bảng riêng** cho portfolio ứng viên (không dùng text tự do):

| Bảng | Nội dung | Quan hệ |
|---|---|---|
| `candidate_profiles` | Thông tin cơ bản: `first_name`, `last_name`, `phone`, `location`, `about_me`, `avatar_url`, `github_url`, `linkedIn_url`, `portfolio_url`, `is_visible_to_recruiters` | 1-1 với `users` |
| `candidate_experiences` | Kinh nghiệm: `title`, `company_name`, `company_id` (FK nullable → `companies`), `employment_type`, `start_date`, `end_date`, `is_current`, `description` | 1-N với `users` |
| `candidate_educations` | Học vấn: `degree`, `major_id` (FK → `majors`), `institution_name`, `gpa`, `max_gpa`, `start_date`, `end_date` | 1-N với `users` |
| `candidate_certifications` | Chứng chỉ: `name`, `issuing_organization`, `issue_date`, `expiration_date`, `credential_url` | 1-N với `users` |

**RULE:** Tất cả FK trong `candidate_experiences`, `candidate_educations` đều có index trong schema (`idx_candidate_experiences_user_id`, `idx_candidate_educations_user_id`).

### 5.2 Active Sourcing Toggle

`candidate_profiles.is_visible_to_recruiters BOOLEAN DEFAULT TRUE` — candidate tự bật/tắt. Khi `true`, hồ sơ xuất hiện trong Active Sourcing của Recruiter (Mục 9.4). Schema đã có cột này.

### 5.3 CV Management

**Flow upload:**
1. Candidate chọn file, validate: dung lượng ≤ 5MB, định dạng PDF/DOCX
2. Upload lên AWS S3 → lưu `cvs`: `file_url`, `file_name`, `file_size`, `file_type`
3. Kích hoạt **bất đồng bộ** AI Parser: extract text → LLM structured extraction → ghi vào `cvs.parsed_data` (JSONB)
4. CV đầu tiên của user → tự động set `is_primary = true`

**RULE — Primary CV phải transaction-safe:**

```sql
-- Schema có Partial Unique Index (cần tạo thủ công vì Eraser không tự gen):
CREATE UNIQUE INDEX idx_cvs_one_primary_per_user
  ON cvs (user_id) WHERE is_primary = true AND deleted_at IS NULL;

-- Khi đổi primary: PHẢI dùng transaction
BEGIN;
  UPDATE cvs SET is_primary = false WHERE user_id = $1 AND is_primary = true;
  UPDATE cvs SET is_primary = true  WHERE id = $2;
COMMIT;
```

**Xóa CV:** Soft delete — set `cvs.deleted_at = now()`, không xóa vật lý. CV bị xóa phải gỡ khỏi search index ngay lập tức (đồng bộ).

### 5.4 User Skills

`user_skills (user_id, skill_id, proficiency_level INT)` — PK composite. Candidate khai báo kỹ năng + mức thành thạo (1–5). Dùng làm input cho AI Matching và Active Sourcing.

---

## 6. MODULE: AI CV-JD MATCHING

> Module này hoàn toàn độc lập với Mock Interview. Hai module khác nhau về mục đích, pipeline, rubric.

### 6.1 Trigger và Input

Candidate chọn CV (mặc định CV `is_primary`) + dán văn bản JD vào ô nhập liệu (free text — **không bắt buộc phải là JD đang có trong hệ thống**).

**⚠ SCHEMA CONFLICT — Vấn đề cần xử lý:**

Schema hiện tại:
```sql
CREATE TABLE cv_job_match_scores (
  cv_id    UUID NOT NULL REFERENCES cvs(id)         -- NOT NULL
  job_id   UUID NOT NULL REFERENCES job_postings(id) -- NOT NULL ← VẤN ĐỀ
  raw_jd_text TEXT                                   -- có nhưng job_id vẫn NOT NULL
);
```

`job_id` là NOT NULL nhưng use case UC-11 cho phép paste JD tùy ý không thuộc `job_postings`. **Cần sửa schema:** `job_id` phải là nullable, và thêm constraint application-level: ít nhất một trong (`job_id`, `raw_jd_text`) phải có giá trị. Xem chi tiết tại Mục 17.

### 6.2 Pipeline (4 bước)

```
Bước 1 — Parsing
  CV  → PDF Parser → raw text → LLM structured extraction (JSON schema bắt buộc)
  JD  → LLM structured extraction → JD JSON
  CACHE: kết quả parse JD theo job_id (nếu có) — 100 candidate cùng test 1 job
         → JD chỉ parse 1 lần

Bước 2 — Skill Weight Calculation (DÙNG CHUNG SERVICE với Mục 7.2)
  raw_weight(skill) = base_weight × (1 + 0.1 × freq) × position_factor × keyword_boost
    base_weight:       required = 1.0,  nice-to-have = 0.4
    position_factor:   job title = 1.3, requirements = 1.0, bonus = 0.6
    keyword_boost:     "must have"/"required"/"bắt buộc" → ×1.5
  normalize:           weight = raw_weight / Σ(raw_weight)  → tổng = 1.0

Bước 3 — Xác định Role Category của JD
  Lookup bảng cv_matching_rubrics theo role_category
  (SCHEMA GAP: bảng này chưa tồn tại — xem Mục 17)

Bước 4 — Áp rubric và tổng hợp điểm
  Với mỗi trục (axis) trong rubric: AI chấm điểm theo barem
  Áp modifiers (kill-switch / penalty / bonus)
  Tổng hợp → match_score cuối cùng
```

### 6.3 Output

Ghi vào `cv_job_match_scores`:
- `match_score DECIMAL(5,2)` — điểm tổng 0–100
- `match_details JSONB` — breakdown theo từng trục, gaps, modifiers nào đã trigger kèm bằng chứng

### 6.4 Tính năng mở rộng từ kết quả Matching

**Optimize CV:** AI sinh đoạn văn mẫu gợi ý chỉnh CV, lồng từ khóa còn thiếu vào ngữ cảnh tự nhiên. Bị giới hạn quota cho gói Basic (`subscriptions.features_config` JSONB).

**Generate Learning Pathway:**

**RULE (đã chốt):** Learning Pathway **CHỈ được sinh từ kết quả AI CV-JD Matching**, KHÔNG liên kết với kết quả Mock Interview.

```
Input: phần gaps trong cv_job_match_scores.match_details
Output: learning_paths.path_data (JSONB) — cây kỹ năng ưu tiên + tài nguyên gợi ý

SCHEMA GAP: learning_paths.session_id vẫn là FK → interview_sessions
  → Cần cột mới: learning_paths.match_score_id (FK → cv_job_match_scores.id)
  → Cột session_id đánh dấu DEPRECATED, không dùng nữa
  (Xem Mục 17)
```

### 6.5 Kiến trúc cv_matching_rubrics (SCHEMA GAP — chưa có)

**RULE đã chốt:**
1. Rubric matching lưu trong bảng riêng có thể CRUD qua UI Staff
2. Áp dụng riêng theo từng `role_category`
3. Có versioning để rollback

```sql
-- Bảng cần tạo (chưa có trong schema.sql)
CREATE TABLE cv_matching_rubrics (
  id             UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  role_category  VARCHAR(100) NOT NULL,  -- 'DEV', 'BA', 'FRONTEND'...
  version        INT NOT NULL DEFAULT 1,
  is_active      BOOLEAN NOT NULL DEFAULT FALSE,
  axes_config    JSONB NOT NULL,  -- cấu trúc linh hoạt theo từng role
  created_by     UUID REFERENCES users(id) ON DELETE SET NULL,
  updated_by     UUID REFERENCES users(id) ON DELETE SET NULL,
  created_at     TIMESTAMP NOT NULL DEFAULT now(),
  updated_at     TIMESTAMP NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX idx_rubrics_one_active_per_role
  ON cv_matching_rubrics (role_category) WHERE is_active = true;
```

**Nội dung `axes_config` JSONB:** khác nhau giữa các role (Dev có 4 trục cố định %, BA phân nhánh theo 3 loại con) → dùng JSONB linh hoạt thay vì cột cứng. Nội dung chi tiết từng rubric sẽ bổ sung sau — agent chỉ dựng kiến trúc lưu trữ này.

---

## 7. MODULE: AI MOCK INTERVIEW

> Chia làm 3 giai đoạn: **(A)** Sinh câu hỏi trước phiên, **(B)** Phiên phỏng vấn thời gian thực, **(C)** Scoring sau phiên.

### 7.1 Thiết lập phiên

**Precondition (RULE bắt buộc):** Kiểm tra credit/subscription trước khi tạo session. Từ chối nếu không đủ.

**Input:** `job_categories` (danh mục), `difficulty_level` (`EASY`/`MEDIUM`/`HARD`), và tuỳ chọn `job_id` nếu luyện theo JD cụ thể (nullable).

**RULE — Partial Unique Index (cần tạo thủ công):**
```sql
CREATE UNIQUE INDEX idx_one_active_session_per_candidate
  ON interview_sessions (candidate_id)
  WHERE status = 'IN_PROGRESS';
```
Một candidate không được có 2 session `IN_PROGRESS` đồng thời.

**Schema `interview_sessions`:**
```
id, candidate_id, job_id (nullable), cv_id (nullable),
difficulty_level, status (IN_PROGRESS/COMPLETED/CANCELLED),
started_at, ended_at
```
`cv_id` đã có trong schema → dùng để cá nhân hóa câu hỏi theo đúng CV candidate đang luyện.

> **LƯU Ý — DB vs Redis state machine:** Bảng `interview_sessions` chỉ lưu 3 trạng thái tổng quát (`IN_PROGRESS`, `COMPLETED`, `CANCELLED`). Các trạng thái chi tiết hơn trong phiên (`PREPARING`, `READY`, `ASKING`, `EVALUATING`, `FOLLOWING_UP`, `TRANSITIONING`, `COMPLETING`, `ABANDONED`) được quản lý ở **Redis** (TTL 2 giờ), không persist vào PostgreSQL. Khi phiên kết thúc, Redis state được xóa và DB được cập nhật trạng thái tổng quát.

### 7.2 Pipeline sinh câu hỏi (8 bước) — GIAI ĐOẠN A

**Phải implement đúng trình tự, không gộp tắt bước.**

#### Bước 1 — Parsing

```
CV  → PDF Parser (pdfplumber / python-docx / pytesseract OCR cho scan)
    → raw text → LLM structured extraction (JSON schema bắt buộc)
    → CV JSON: { candidate_skills[], total_exp_years, notable_projects[], gaps[] }

JD  → LLM structured extraction
    → JD JSON: { role, level, required_skills[], nice_to_have[], responsibilities[] }

CACHE: JD parse result theo job_id (nếu có) — không cache khi job_id null
```

#### Bước 2 — Analysis

```
Skill Weight Calculation: DÙNG CHUNG SERVICE với Mục 6.2
  (không viết logic trùng lặp giữa 2 module)

Gap Analysis:
  gaps    = required_skills(JD) − candidate_skills(CV)
  matched = required_skills(JD) ∩ candidate_skills(CV)
  skill_context[skill] = context từ CV (tên project, năm kinh nghiệm)
                         → dùng để cá nhân hóa câu hỏi Applied
```

#### Bước 3 — Slot Map (Template approach)

```json
// Ví dụ slot_map cho 6 câu:
[
  { "type": "warmup",      "skill": null,                    "count": 1 },
  { "type": "Applied",     "skill": "<skill weight cao nhất>","count": 2 },
  { "type": "Situational", "skill": "<skill weight thứ 2>",   "count": 1 },
  { "type": "gap_probe",   "skill": "<1 skill trong gaps>",   "count": 1 },
  { "type": "closing",     "skill": null,                    "count": 1 }
]
```

**RULE:** Số câu tỷ lệ thuận với `skill_weight`. Luôn có 1 warmup đầu, 1 closing cuối. Có ít nhất 1 `gap_probe` nếu `gaps` không rỗng.

#### Bước 4 — Golden Retrieval

**RULE — KHÔNG dùng Vector DB.** Bài toán là exact-match, dataset ~300–500 dòng.

```sql
SELECT question_text, sample_answer
FROM   interview_question_bank
WHERE  category_id  = :category_id
  AND  difficulty   = :difficulty    -- enum difficulty_level
ORDER  BY created_at DESC
LIMIT  2;
```

> **SCHEMA GAP:** `interview_question_bank` thiếu `skill_id` và `target_level`. Hiện chỉ có `category_id` (FK → `job_categories`) và `difficulty` (EASY/MEDIUM/HARD). Điều này làm giảm độ chính xác của golden retrieval — hỏi SQL có thể match category rộng thay vì đúng skill. Xem Mục 17 để bổ sung.

#### Bước 5 — Generation

Gọi LLM **1 lần**, sinh **gấp đôi số câu cần** (cần 6 → sinh 12 để có dư cho bước lọc).

```
Input:  slot_map + golden examples + JD JSON + CV JSON + skill_weights
Output: JSON array [{ slot_id, id, type, text, target_skill,
                      difficulty, follow_up_hints[] }]
Temperature: 0.7
```

#### Bước 6 — Quality Filter (Hybrid Rule + LLM)

**Lý do thiết kế 2 tầng:** Gọi LLM 12 lần song song tốn chi phí ×12 và dễ dính rate-limit 429. Batch 1 lần 12 câu dễ JSON-parse-fail và context fatigue. Giải pháp: rule miễn phí trước, rồi LLM mini-batch nhỏ.

```
Tầng 1 — Rule-based (0 API call):
  Loại nếu: length < 30 hoặc > 600 ký tự
            không có dấu "?"
            target_skill không xuất hiện trong text câu hỏi
            trùng lặp với câu khác trong batch
            ngôn ngữ không phải vi/en
  Kỳ vọng: loại ~20–35% ngay, không tốn API

Tầng 2 — LLM mini-batch (chỉ cho câu đã qua tầng 1):
  Batch 4 câu/lần. Temperature = 0.
  5 tiêu chí mỗi câu: clarity, relevance, calibration, practicality, answerability
  Ngưỡng giữ lại: avg ≥ 3.5

  Rate limit 429: exponential backoff 1s → 2s → 4s (tối đa 3 retry)
  Nếu vẫn fail: rule_based_score_fallback (ước tính ~3.3, đủ ngưỡng)
                flag evaluated_by = 'rule_fallback'
                KHÔNG fallback về câu hỏi hardcode chung chung
```

#### Bước 7 — Coverage Check

```python
for slot in slot_map:
    candidates = [q for q in filtered if q.skill == slot.skill and q.type == slot.type]
    if len(candidates) < slot.count:
        # Retry CHỈ slot còn thiếu, tối đa 3 lần
        # Retry 2: nới lỏng difficulty ±1
        # Retry 3: nới lỏng question type
        # Sau 3 lần vẫn thiếu: dùng golden examples từ interview_question_bank
        #   (đây là trường hợp DUY NHẤT được dùng câu có sẵn từ bank)
```

#### Bước 8 — Assembly

```
Thứ tự câu hỏi cuối cùng:
  1. warmup
  2. core vừa phải (skill weight trung bình)
  3. core quan trọng nhất (skill weight cao nhất)
  4. gap_probe (KHÔNG đặt ở đầu)
  5. closing

Với mỗi câu: gắn follow_up_hints[] (2–3 gợi ý dùng ở Giai đoạn B)
Output: question_script → lưu vào Redis cùng session state
```

### 7.3 Phiên phỏng vấn (State Machine) — GIAI ĐOẠN B

**State Machine lưu trong Redis (TTL 2h), KHÔNG lưu chi tiết vào PostgreSQL:**

| Redis State | Vào khi | Ra khi |
|---|---|---|
| `PREPARING` | Candidate bấm bắt đầu | Script sẵn sàng. Timeout 30s → báo lỗi |
| `READY` | Script xong | Candidate bấm Start. Không start trong 15ph → `ABANDONED` |
| `ASKING` | AI gửi câu hỏi | Candidate submit HOẶC timeout |
| `EVALUATING` | Nhận answer | Quyết định follow-up hay chuyển câu (xem 7.4) |
| `FOLLOWING_UP` | Cần đào sâu | Tối đa 2 follow-up/câu. Timer KHÔNG reset |
| `TRANSITIONING` | Đủ depth / hết follow-up | Cập nhật key_facts, chuyển câu tiếp |
| `COMPLETING` | Hỏi hết script | Build transcript, lưu PostgreSQL, xóa Redis, push scoring queue |
| `COMPLETED` / `ABANDONED` | Final | `ABANDONED` vẫn lưu partial transcript, vẫn có thể score |

**DB `interview_sessions.status` mapping:**
- Redis states `PREPARING` → `COMPLETING` → tương ứng DB `IN_PROGRESS`
- Redis `COMPLETED` → DB `COMPLETED`
- Redis `ABANDONED` + timeout → DB `CANCELLED` *(schema chỉ có CANCELLED, không có ABANDONED riêng)*

**Time limits theo question type (giây):**
```
warmup: 120 | Applied: 180 | Conceptual: 120
Situational: 240 | gap_probe: 240 | closing: 60
```

### 7.4 Follow-up Logic — 2 tầng

**RULE UX:** Candidate không bao giờ biết mình đang bị hỏi follow-up hay sang câu mới. Không có label "Follow-up". Sidebar không thay đổi số câu.

```
Tầng 1 — Rule-based (0 LLM call):
  Trigger follow-up NGAY nếu:
    word_count(answer) < 30
    dùng buzzword không có giải thích đi kèm
    lạc đề hoàn toàn (không chứa keyword liên quan target_skill)
    rubric yêu cầu ví dụ cụ thể (expect_example=true) nhưng answer không có

Tầng 2 — LLM depth check (chỉ khi pass Tầng 1):
  Prompt tối giản: câu hỏi + answer (KHÔNG gửi CV/JD đầy đủ)
  Temperature = 0
  Output: {"sufficient": bool, "reason": "1 câu"}
  Nếu sufficient=false → follow-up dựa theo reason

RULE: Tối đa 2 follow-up/câu. Sau 2 lần → chuyển tiếp, ghi nhận vào transcript
      (điểm scoring sẽ phản ánh câu trả lời chưa đủ depth)
```

### 7.5 Context Management (Sliding Window)

```
Mỗi lần gọi LLM trong phiên, context gồm:
  1. System prompt cố định (~200 tokens)
  2. Câu hỏi hiện tại + follow_up_hints (KHÔNG gửi toàn bộ question_script)
  3. 4 turns gần nhất (sliding window — không gửi toàn bộ lịch sử)
  4. key_facts block (~100 tokens): facts quan trọng từ các turns trước
     (project names, tech mentioned, years exp) — cập nhật sau mỗi turn
     bằng 1 LLM call nhỏ riêng, thay thế cho gửi toàn bộ history
```

### 7.6 Voice Input (đã chốt kiến trúc)

**RULE — Voice → Text → Edit → Submit:**

```
Candidate nói → Web Speech API (chạy trong TRÌNH DUYỆT, không gọi Cloud STT)
  → text xuất hiện trong textarea (interimResults realtime preview)
  → candidate đọc lại, SỬA NẾU CẦN (quan trọng cho thuật ngữ kỹ thuật)
  → bấm Gửi → text lên server như gõ tay bình thường

Backend KHÔNG cần thay đổi gì. Server chỉ nhận field text.
Không cần WebSocket audio streaming.
Không cần TTS cho câu hỏi AI.
Fallback: gõ tay trực tiếp (luôn available, không phụ thuộc voice).
```

### 7.7 Edge Cases phiên phỏng vấn

| Edge case | Xử lý |
|---|---|
| Timeout (hết giờ câu) | Warning 30s trước. Hết giờ → auto-submit partial, `timeout=true`, `score_override.max=1/5` |
| Candidate hỏi lại câu hỏi | AI paraphrase, KHÔNG giải thích thêm (tránh lộ đáp án) |
| Mất kết nối WebSocket | Client auto-reconnect (exponential backoff). Server restore từ Redis bằng `session_id` |
| Copy-paste nghi ngờ | Phát hiện: submit < 5s cho > 200 ký tự hoặc văn phong đổi đột ngột. Flag `suspicious_flags[]` vào transcript. KHÔNG block, chỉ báo HR |
| Bỏ qua câu | Modal confirm → lưu `"[UV bỏ qua]"`, `score_override.max=1`, AI không phán xét |
| LLM API fail | Retry 1 lần sau 2s. Fail tiếp → hardcode response template theo state hiện tại (chỉ câu acknowledge chung, không phải câu hỏi chuyên môn) |

### 7.8 Scoring Pipeline — GIAI ĐOẠN C

Chạy **bất đồng bộ** sau `COMPLETING`. Candidate xem "Đang phân tích...", polling 3s.

**RULE — Cơ chế chống hallucination (tất cả 4 phải thực hiện đủ):**
1. Mỗi skill được LLM chấm **độc lập** (gọi riêng, không gộp nhiều skill)
2. Rubric cứng (score_anchors 1–5) làm neo — không để AI tự do đánh giá
3. `temperature = 0` khi scoring
4. Structured output / forced JSON schema

```
Input/output mỗi skill call:
  IN:  câu hỏi + câu trả lời (CẢ follow-up) + rubric + JD skill requirement
  OUT: { score: 1-5, evidence: "trích từ transcript",
          strengths: [], gaps: [] }

Tổng hợp:
  raw_score       = Σ(skill_score_i × skill_weight_i)
  pass_probability = calibrate(raw_score) → 0–100%   # sigmoid-style calibration
```

**Schema `interview_answers`** đã có đủ:
- `parent_answer_id UUID` (FK self → `interview_answers`) — chain follow-up
- `score_logic INT`, `score_tech INT`, `score_communication INT` — 3 chiều điểm

### 7.9 Output & Privacy

**Candidate xem được:** `pass_probability`, breakdown điểm theo skill, top 2–3 gaps, gợi ý tài liệu, toàn bộ transcript.

**RULE Privacy:** HR **KHÔNG thấy transcript mặc định**. HR chỉ thấy tổng điểm + breakdown skill. Transcript đầy đủ chỉ hiện khi candidate chủ động "share" khi nộp đơn.

---

## 8. MODULE: JOB DISCOVERY & APPLICATION

### 8.1 Tìm kiếm JD

Mở cho Guest. Filter theo keyword, `job_type`, lương, địa điểm, danh mục (`job_categories`).

**RULE:** Xem chi tiết JD → tăng `job_postings.view_count` **bất đồng bộ** (không block response).

**Schema `job_postings` đã có:** `job_code UNIQUE`, `view_count INT DEFAULT 0`, `application_count INT DEFAULT 0` (denormalized counter), `published_at`, `expires_at`, `deleted_at` (soft delete).

**`job_type` enum:** `FULL_TIME`, `PART_TIME`, `CONTRACT`, `FREELANCE`, `INTERNSHIP`

**`job_status` enum:** `DRAFT`, `PENDING_REVIEW`, `PUBLISHED`, `REJECTED`, `EXPIRED`, `CLOSED`

### 8.2 Lưu / Bỏ lưu tin

CRUD trên `user_saved_jobs` (composite PK `user_id + job_id`).

### 8.3 Ứng tuyển

```
Flow:
1. Chọn CV (pre-select CV is_primary) + cover letter (≤ 2000 ký tự, optional)
2. Kiểm tra UNIQUE(candidate_id, job_id) trên job_applications — từ chối nộp lại
3. INSERT job_applications với status = 'APPLIED'
4. Tăng job_postings.application_count += 1 (denormalized counter)
5. Trigger notification realtime đến Recruiter (Mục 13)
```

### 8.4 Theo dõi tiến độ

`application_status` enum (từ schema): `APPLIED`, `VIEWED`, `SHORTLISTED`, `INTERVIEWING`, `OFFERED`, `HIRED`, `REJECTED`, `WITHDRAWN`

Mỗi thay đổi status → ghi `application_history` (`from_status`, `to_status`, `changed_by`). Append-only, không update.

**Lưu ý so với MD cũ:** Schema có thêm `VIEWED` (Recruiter đã xem CV), `HIRED` (đã nhận), `WITHDRAWN` (ứng viên tự rút đơn). Không còn `REVIEWING` (đã đổi thành `VIEWED`).

---

## 9. MODULE: RECRUITER & ATS

### 9.1 Company Profile

Recruiter sửa thông tin công ty. **Không** cần duyệt lại (chỉ lần KYC đầu tiên mới cần Admin duyệt).

Schema `companies` có thêm: `verification_method` (enum: `BUSINESS_REGISTRATION` / `POA_AND_ID`), `verification_document_url`, `industry`, `company_size`, `description`.

### 9.2 Đăng tin (Post Job)

**Precondition:** `companies.status = 'VERIFIED'` + đủ credit/subscription.

**RULE — Skill tagging:** Recruiter **chỉ chọn từ `skills` có sẵn** (dropdown), không nhập free text. Lý do: đảm bảo dữ liệu sạch cho AI Matching (Mục 6).

Schema `job_skill_requirements`: `job_id`, `skill_id`, `is_mandatory BOOLEAN DEFAULT TRUE`.

> **SCHEMA GAP:** Thiếu `required_level INT` (mức độ thành thạo yêu cầu 1–5). Xem Mục 17.

JD sau khi tạo có `status = 'DRAFT'` → Recruiter submit → `PENDING_REVIEW` → Admin duyệt → `PUBLISHED` hoặc `REJECTED`. Recruiter có thể ẩn tin (`CLOSED`) hoặc tin tự hết hạn (`EXPIRED` khi `expires_at < now()`).

### 9.3 ATS Pipeline — Kanban

Kanban columns theo `application_status`. Kéo-thả = update status + ghi `application_history` + trigger email async (Mục 13.2).

Xem CV: inline PDF viewer. **EDGE CASE:** S3 presigned URL hết hạn → hiển thị nút "Tải về" fallback.

### 9.4 Active Sourcing

Recruiter kích hoạt từ trang JD → AI quét Talent Pool (`candidate_profiles.is_visible_to_recruiters = true`) → trả danh sách CV phù hợp dựa trên cùng cơ chế Mục 6 (semantic matching theo skill vector).

### 9.5 Analytics Dashboard

Funnel: lượt xem JD (`view_count`) → đơn ứng tuyển (`application_count`) → shortlist → offer. Dùng `job_postings` denormalized counters + query `application_history`.

---

## 10. MODULE: ADMIN OPERATIONS (BUSINESS)

### 10.1 Quản lý tài khoản

Xem danh sách `users`, kích hoạt/vô hiệu hóa, xem `user_activity_logs` chi tiết.

Schema `user_activity_logs` đã có thêm: `actor_role`, `actor_email`, `action_category` (enum: `AUTH`/`SYSTEM`/`DATA_MUTATION`/`SECURITY`), `status` (`SUCCESS`/`FAIL`).

### 10.2 Company KYC

```
Flow:
1. Admin xem danh sách companies có status = 'PENDING'
2. Xem verification_document_url (PDF)  — manual review
3. Approve → status = 'VERIFIED'
   Reject  → status = 'REJECTED', reject_reason bắt buộc
4. Ghi audit vào company_reviews (id, company_id, admin_id, status, reject_reason)
5. Gửi email kết quả tự động (Mục 13.2)
```

Schema `company_reviews` đã có với `review_status ENUM ('PENDING', 'APPROVED', 'REJECTED')`.

### 10.3 Master Data

CRUD trên `job_categories` (recursive `parent_id`), `majors`, `skill_categories`, `skills`.

`skills` có `status (skill_status ENUM 'ACTIVE'/'DEACTIVE')` — có thể vô hiệu hóa skill cũ mà không xóa.

**RULE:** `skills.status = 'DEACTIVE'` → không hiển thị trong dropdown tạo JD, không dùng cho matching mới (nhưng giữ dữ liệu cũ để không gãy FK).

### 10.4 Duyệt JD

```
Flow:
1. Hàng chờ: job_postings với status = 'PENDING_REVIEW'
2. Auto-highlight cụm từ nhạy cảm, link ngoài (rule-based)
3. Approve → status = 'PUBLISHED', set published_at = now()
   Reject  → status = 'REJECTED', ghi job_reviews với reject_reason

EDGE CASE — Race condition: Recruiter sửa JD đúng lúc Admin đang duyệt
  → Optimistic locking trên job_postings.updated_at
  → Trước khi Admin submit quyết định: check updated_at so với lúc Admin mở trang
  → Nếu lệch → báo "JD đã bị sửa, vui lòng xem lại"
```

### 10.5 Subscription & Tài chính

CRUD `subscriptions` (`name`, `price`, `duration_days`, `features_config JSONB`). Xem toàn bộ `payments`, `credit_transactions`.

---

## 11. MODULE: STAFF OPERATIONS (TECHNICAL AI OPS)

### 11.1 AI Config

- **API Keys:** Lưu mã hóa (encrypted at rest) trong `system_configs`. Không bao giờ lưu plaintext.
- **Model switch:** Cập nhật `system_configs` config_key tương ứng (vd `AI_MODEL_PRIMARY`)
- **Rate limits:** Cấu hình ngưỡng request/phút

### 11.2 System Prompts (có Versioning)

**RULE:** Khi update prompt → **KHÔNG ghi đè** `system_prompts`. Schema hiện tại chỉ có `version INT` (tăng số) nhưng KHÔNG lưu lịch sử bản cũ.

> **SCHEMA GAP:** `system_prompts` thiếu `is_active BOOLEAN` và không có bảng lịch sử. Không thể rollback khi prompt mới gây kết quả kém. Xem Mục 17.

### 11.3 Question Bank (Golden Examples)

CRUD `interview_question_bank`. Các fields hiện có: `category_id`, `difficulty`, `question_text`, `sample_answer`.

> **SCHEMA GAP:** Thiếu `skill_id` (INT FK → `skills`) và `target_level` (ENUM `FRESHER/JUNIOR/MID/SENIOR`). Không có `skill_id` thì golden retrieval ở Mục 7.2 Bước 4 chỉ match được theo category rộng, không đủ độ chính xác. Xem Mục 17.

### 11.4 Matching Rubrics

CRUD `cv_matching_rubrics` (bảng cần tạo — Mục 6.5, 17). Versioning giống `system_prompts`.

### 11.5 Audit & Chi phí AI

**`ai_api_usage_logs`** đã có trong schema: `reference_id` (polymorphic), `prompt_tokens`, `completion_tokens`, `model_name`, `cost_usd`.

**RULE cross-cutting:** Ghi `ai_api_usage_logs` sau **mọi lần gọi LLM API** ở bất kỳ module nào (6, 7, 9.4).

**`user_activity_logs`** (schema đã enhanced) đóng vai trò audit log hệ thống với `action_category = 'SECURITY'` hoặc `'DATA_MUTATION'` cho các thao tác nhạy cảm Staff/Admin.

---

## 12. MODULE: MONETIZATION

### 12.1 Hai mô hình song song

| Mô hình | Bảng |
|---|---|
| Subscription (gói tháng) | `subscriptions`, `user_subscriptions` |
| Credit (pay-as-you-go) | `user_wallets`, `credit_transactions` |

**RULE:** `user_wallets.balance INT CHECK (balance >= 0)` — không cho phép âm. Kiểm tra credit **trước khi** gọi AI.

### 12.2 Thanh toán

`payment_gateway` enum: `VNPAY`, `MOMO`, `STRIPE`, `PAYPAL`, `BANK_TRANSFER`

`payment_target_type` enum: `SUBSCRIPTION`, `WALLET_TOPUP`, `JOB_PROMOTION` *(schema đã có giá trị này — không cần bổ sung như MD cũ đề cập)*

`payment_status` enum: `PENDING`, `SUCCESS`, `FAILED`, `REFUNDED` *(lưu ý: là `SUCCESS` không phải `COMPLETED` như MD cũ)*

```
Flow:
1. Tạo payments với status = 'PENDING', gateway_transaction_id UNIQUE
   (UNIQUE constraint chống double-processing khi IPN gọi lại nhiều lần)
2. Timeout 15 phút → background job set status = 'FAILED' nếu chưa xử lý
3. IPN Webhook arrive → verify → status = 'SUCCESS'
   → credit: cộng user_wallets.balance, ghi credit_transactions loại 'TOPUP'
   → subscription: tạo user_subscriptions mới

EDGE CASE — Cổng sập sau khi trừ tiền, webhook không tới:
  Đơn giữ PENDING. Background job phát hiện → Manual Reconcile Queue
  (query: payments WHERE status='PENDING' AND created_at < NOW() - 20min)
  User liên hệ support kèm payments.id để đối soát thủ công
```

### 12.3 Credit Transaction Ledger

`credit_transaction_type` enum: `TOPUP`, `DEDUCT`, `REFUND`, `BONUS`

`credit_transactions` là **sổ cái append-only** — chỉ INSERT, không UPDATE/DELETE. `reference_id` (UUID, polymorphic) trỏ về nguồn gốc giao dịch.

---

## 13. MODULE: NOTIFICATION SYSTEM

### 13.1 In-app Notification

`notification_type` enum: `SYSTEM`, `APPLICATION`, `INTERVIEW`, `PAYMENT`, `PROMOTION`

CRUD trên `notifications`. Candidate/Recruiter đánh dấu `is_read = true`.

### 13.2 Email tự động (Background Job)

**RULE:** Mọi email PHẢI chạy qua background worker (không gọi SMTP đồng bộ trong request chính).

**Các trigger bắt buộc:**
- Đăng ký → email xác thực
- Quên mật khẩu → email reset token
- Đơn ứng tuyển mới → báo Recruiter
- Thay đổi `application_status` → báo Candidate
- Kết quả KYC → báo Recruiter
- Kết quả duyệt JD → báo Recruiter
- Thanh toán `SUCCESS` → báo user

**RULE:** Ghi `sys_email_logs` mọi lần gửi (cả thành công và thất bại). Schema `email_log_status` enum: `SENT`, `FAILED`, `PENDING`.

> **SCHEMA GAP:** Không có bảng `email_templates`. Nội dung email hiện phải hardcode trong code. Xem Mục 17.

---

## 14. NGUYÊN TẮC THIẾT KẾ AI XUYÊN SUỐT

Áp dụng cho **mọi** tính năng AI (Mục 6, 7, 9.4):

1. **AI-native** — không bolt-on (Mục 1.2)
2. **PostgreSQL exact-match, không Vector DB** cho golden examples và dataset curated nhỏ
3. **Anti-hallucination:** luôn chấm độc lập từng đơn vị, rubric cứng làm neo, `temperature=0` khi scoring
4. **Hybrid rule-based + LLM:** rule miễn phí trước, LLM chỉ cho phần cần semantic reasoning
5. **Fallback thông minh:** không hardcode nội dung chuyên môn làm fallback, chỉ template acknowledge chung
6. **Versioning bắt buộc** cho mọi cấu hình AI ảnh hưởng hành vi (`system_prompts`, `cv_matching_rubrics`)
7. **Ghi `ai_api_usage_logs`** sau mọi LLM call (cross-cutting concern)
8. **Kiểm tra quota/credit trước khi gọi AI** — không sau
9. **Dùng chung service** tính skill weight giữa Mục 6.2 và 7.2 — không viết lại logic

---

## 15. TỔNG HỢP ENUM TYPES TỪ SCHEMA

Đây là toàn bộ 24 enum đã định nghĩa trong schema — agent phải dùng đúng giá trị này:

| Enum Type | Values |
|---|---|
| `user_status` | `ACTIVE`, `INACTIVE`, `BANNED`, `PENDING_VERIFICATION` |
| `company_verification_method` | `BUSINESS_REGISTRATION`, `POA_AND_ID` |
| `company_status` | `PENDING`, `VERIFIED`, `REJECTED` |
| `review_status` | `PENDING`, `APPROVED`, `REJECTED` |
| `skill_status` | `ACTIVE`, `DEACTIVE` |
| `job_type` | `FULL_TIME`, `PART_TIME`, `CONTRACT`, `FREELANCE`, `INTERNSHIP` |
| `job_status` | `DRAFT`, `PENDING_REVIEW`, `PUBLISHED`, `REJECTED`, `EXPIRED`, `CLOSED` |
| `application_status` | `APPLIED`, `VIEWED`, `SHORTLISTED`, `INTERVIEWING`, `OFFERED`, `HIRED`, `REJECTED`, `WITHDRAWN` |
| `promotion_status` | `ACTIVE`, `EXPIRED`, `CANCELLED` |
| `difficulty_level` | `EASY`, `MEDIUM`, `HARD` |
| `interview_session_status` | `IN_PROGRESS`, `COMPLETED`, `CANCELLED` |
| `subscription_status` | `ACTIVE`, `INACTIVE` |
| `user_subscription_status` | `ACTIVE`, `EXPIRED`, `CANCELLED` |
| `payment_gateway` | `VNPAY`, `MOMO`, `STRIPE`, `PAYPAL`, `BANK_TRANSFER` |
| `payment_target_type` | `SUBSCRIPTION`, `WALLET_TOPUP`, `JOB_PROMOTION` |
| `payment_status` | `PENDING`, `SUCCESS`, `FAILED`, `REFUNDED` |
| `credit_transaction_type` | `TOPUP`, `DEDUCT`, `REFUND`, `BONUS` |
| `employment_type` | `FULL_TIME`, `PART_TIME`, `CONTRACT`, `FREELANCE` |
| `notification_type` | `SYSTEM`, `APPLICATION`, `INTERVIEW`, `PAYMENT`, `PROMOTION` |
| `email_log_status` | `SENT`, `FAILED`, `PENDING` |
| `activity_log_category` | `AUTH`, `SYSTEM`, `DATA_MUTATION`, `SECURITY` |
| `activity_log_status` | `SUCCESS`, `FAIL` |

---

## 16. TỔNG HỢP BẢNG THEO PHÂN HỆ

Tổng: **42 bảng** trong schema (38 ban đầu + 4 đã bổ sung sẵn trong schema.sql).

### Phân hệ 1 — IAM & Profiles (8 bảng)
`roles`, `permissions`, `role_permissions`, `users`, `candidate_profiles`, `email_verification_tokens`, `password_resets`, `refresh_tokens`, `user_activity_logs`

### Phân hệ 2 — Master Data & Companies (6 bảng)
`companies`, `recruiter_profiles`, `company_reviews`, `job_categories`, `majors`, `skill_categories`, `skills`

### Phân hệ 3 — Candidate Portfolio (5 bảng)
`cvs`, `user_skills`, `candidate_experiences`, `candidate_educations`, `candidate_certifications`

### Phân hệ 4 — ATS & Jobs (7 bảng)
`job_postings`, `job_skill_requirements`, `job_reviews`, `user_saved_jobs`, `job_applications`, `application_history`, `job_promotions`

### Phân hệ 5 — AI Engine (6 bảng)
`cv_job_match_scores`, `interview_question_bank`, `interview_sessions`, `interview_answers`, `interview_reports`, `learning_paths`, `ai_api_usage_logs`

### Phân hệ 6 — Finance (5 bảng)
`subscriptions`, `user_subscriptions`, `user_wallets`, `payments`, `credit_transactions`

### Phân hệ 7 — System Ops (4 bảng)
`system_configs`, `system_prompts`, `notifications`, `sys_email_logs`

---

## 17. SCHEMA GAPS — CẦN BỔ SUNG TRƯỚC KHI IMPLEMENT

Đây là danh sách các điểm schema hiện tại **chưa đủ** để implement đúng logic nghiệp vụ đã mô tả. Phải resolve trước khi code các module liên quan.

### Gap 1 — `cv_job_match_scores.job_id` là NOT NULL *(mâu thuẫn với Use Case)*

**Vấn đề:** UC-11 cho phép paste JD tự do mà không gắn job_posting. Schema có `raw_jd_text TEXT` nhưng `job_id NOT NULL` → sẽ báo lỗi khi insert.

**Cần sửa:**
```sql
ALTER TABLE cv_job_match_scores
  ALTER COLUMN job_id DROP NOT NULL;

-- Application-level constraint: ít nhất một trong hai phải có giá trị
-- CHECK không thể trên 2 nullable FK, xử lý ở tầng service
```

### Gap 2 — `cv_matching_rubrics` — bảng chưa tồn tại

Xem DDL đề xuất tại Mục 6.5.

### Gap 3 — `learning_paths.session_id` vẫn dùng, chưa có `match_score_id`

**Cần bổ sung:**
```sql
ALTER TABLE learning_paths
  ADD COLUMN match_score_id UUID REFERENCES cv_job_match_scores(id) ON DELETE SET NULL;
-- session_id giữ lại nhưng đánh dấu deprecated trong code comments
```

### Gap 4 — `system_prompts` thiếu versioning history

**Vấn đề:** Schema chỉ có `version INT` (tăng số) nhưng không lưu bản cũ → không rollback được.

**Cần sửa:**
```sql
ALTER TABLE system_prompts
  ADD COLUMN is_active BOOLEAN NOT NULL DEFAULT TRUE;
-- Khi update: INSERT bản mới (is_active=true), UPDATE bản cũ (is_active=false)
-- Không UPDATE bản hiện tại
-- Thêm Partial Unique Index:
CREATE UNIQUE INDEX idx_prompts_one_active_per_key
  ON system_prompts (prompt_key) WHERE is_active = true;
```

### Gap 5 — `interview_question_bank` thiếu `skill_id` và `target_level`

```sql
ALTER TABLE interview_question_bank
  ADD COLUMN skill_id     INT REFERENCES skills(id) ON DELETE SET NULL,
  ADD COLUMN target_level VARCHAR(20); -- 'FRESHER', 'JUNIOR', 'MID', 'SENIOR'
  -- Hoặc tạo enum riêng: CREATE TYPE seniority_level AS ENUM(...)

CREATE INDEX idx_qbank_skill_id ON interview_question_bank(skill_id);
```

### Gap 6 — `job_skill_requirements` thiếu `required_level`

```sql
ALTER TABLE job_skill_requirements
  ADD COLUMN required_level INT CHECK (required_level BETWEEN 1 AND 5);
```

### Gap 7 — `email_templates` — bảng chưa tồn tại

```sql
CREATE TABLE email_templates (
  template_key  VARCHAR(150) PRIMARY KEY,
  subject       VARCHAR(500) NOT NULL,
  body_html     TEXT NOT NULL,
  updated_by    UUID REFERENCES users(id) ON DELETE SET NULL,
  updated_at    TIMESTAMP NOT NULL DEFAULT now()
);
```

### Gap 8 — Partial Unique Indexes chưa có trong schema (cần tạo thủ công)

Schema Eraser không tự gen các Partial Unique Index. Phải chạy thủ công:

```sql
-- CV primary
CREATE UNIQUE INDEX idx_cvs_one_primary_per_user
  ON cvs (user_id) WHERE is_primary = true AND deleted_at IS NULL;

-- 1 session active/candidate
CREATE UNIQUE INDEX idx_one_active_session_per_candidate
  ON interview_sessions (candidate_id) WHERE status = 'IN_PROGRESS';

-- cv_matching_rubrics 1 active/role (khi tạo bảng mới)
CREATE UNIQUE INDEX idx_rubrics_one_active_per_role
  ON cv_matching_rubrics (role_category) WHERE is_active = true;
```

---

## 18. QUY TẮC & RÀNG BUỘC TOÀN HỆ THỐNG

1. **Soft delete:** dùng `deleted_at TIMESTAMP` — áp dụng `users`, `companies`, `job_postings`, `cvs`.
2. **Transaction bắt buộc** cho mọi thao tác "chỉ 1 record active tại 1 thời điểm": `cvs.is_primary`, `interview_sessions` `IN_PROGRESS`, `system_prompts` active, `cv_matching_rubrics` active.
3. **Versioning bắt buộc** cho `system_prompts` và `cv_matching_rubrics` — không ghi đè bản cũ.
4. **Email = background worker + `sys_email_logs`** — không gọi SMTP đồng bộ.
5. **`ai_api_usage_logs`** ghi sau mọi LLM call.
6. **Kiểm tra quota/credit trước khi gọi AI** — không sau.
7. **Skill weight service dùng chung** giữa Module 6 và Module 7.
8. **Skill tagging trong JD = dropdown từ `skills`** — không nhập text tự do.
9. **`payments.gateway_transaction_id UNIQUE`** — chống double-processing IPN.
10. **`user_wallets.balance >= 0`** (CHECK constraint) — không cho phép âm.
11. **`job_applications` UNIQUE(candidate_id, job_id)** — không cho nộp đơn trùng.
12. **`application_history` append-only** — không UPDATE, chỉ INSERT khi status đổi.
13. **`credit_transactions` append-only** — sổ cái immutable.
14. **DB `interview_session_status`** chỉ có 3 giá trị (`IN_PROGRESS/COMPLETED/CANCELLED`). Redis state machine chi tiết hơn không persist xuống DB.
15. **`job_postings.application_count`** là denormalized counter — tăng khi có đơn mới, giảm khi đơn bị WITHDRAWN.

---

## 19. OPEN ITEMS — CHƯA CHỐT

| # | Vấn đề | Action cần thiết |
|---|---|---|
| 1 | Nội dung chi tiết `cv_matching_rubrics.axes_config` theo từng role (Dev/BA/...) | Đang xây dựng riêng, sẽ nạp vào bảng sau khi schema Gap 2 được tạo |
| 2 | `interview_session_status` schema chỉ có `CANCELLED`, không có `ABANDONED` riêng | Clarify: dùng `CANCELLED` để cover cả abandoned, hay cần thêm enum value? |
| 3 | `job_skill_requirements` không có `required_level` và schema cũng không có index (Gap 6) | Resolve Gap 6 trước khi implement module 9.2 |
| 4 | UC file gốc còn lỗi ID trùng lặp (UC-72→75) và cross-reference sai | Cần file UC đã sửa trước khi implement các UC bị ảnh hưởng |

---

## 20. GLOSSARY

| Thuật ngữ | Giải thích |
|---|---|
| **Golden Examples** | Câu hỏi mẫu chất lượng cao trong `interview_question_bank`, dùng làm few-shot reference cho LLM khi sinh câu hỏi mới |
| **Slot Map** | Bản đồ xác định trước số lượng/loại/skill của câu hỏi cần sinh, trước khi gọi LLM |
| **Key Facts** | Tóm tắt ~100 tokens các thông tin quan trọng candidate đã đề cập, cập nhật sau mỗi turn, thay thế gửi toàn bộ history |
| **Sliding Window** | Giữ N=4 turns gần nhất trong context LLM |
| **Match Score** | Điểm % từ CV-JD Matching (Module 6), output của `cv_job_match_scores` |
| **Pass Probability** | Xác suất pass phỏng vấn thật (0–100%), output của Scoring Pipeline (Module 7.8) |
| **Kill-switch** | Cơ chế trong rubric matching: thiếu điều kiện cốt lõi → cap điểm bất kể trục khác cao thế nào |
| **Rubric Anchor** | Mô tả cụ thể gắn với từng mức 1–5 trong rubric, dùng làm neo để AI chấm nhất quán |
| **Active Sourcing** | Recruiter chủ động dùng AI tìm CV phù hợp trong Talent Pool |
| **AI-native vs Bolt-on** | AI-native: AI là thành phần không thể tách rời. Bolt-on: AI là lớp phủ thêm vào luồng đã hoạt động không cần AI |
| **SCHEMA GAP** | Tính năng cần thiết nhưng chưa có trong schema.sql hiện tại |
| **Partial Unique Index** | Index PostgreSQL chỉ áp dụng cho subset hàng thỏa mãn điều kiện WHERE — dùng để enforce "chỉ 1 record active" |

---

*Phiên bản này đồng bộ với `schema.sql` (590 dòng, 7 phân hệ, 24 enum types). Mọi thay đổi schema sau này phải cập nhật đồng thời vào tài liệu này.*
