### 1.2 `permissions`
Sinh theo ma trận `action` × `resource`. Action tối thiểu: `create`, `read`, `update`, `delete`, `approve`, `reject`. Resource tối thiểu: `job`, `company`, `cv`, `user`, `application`, `interview`, `payment`, `system_config`.

Ví dụ tối thiểu cần có:
- `create:job`, `update:job`, `delete:job`, `approve:job`, `reject:job`
- `create:company`, `approve:company`, `reject:company`
- `read:cv`, `delete:cv`
- `read:application`, `update:application`
- `read:payment`
- `update:system_config`

### 1.3 `role_permissions`
- `ADMIN`: tất cả permission có `approve`, `reject`, `update:system_config`, và toàn quyền `read`/`delete` trên mọi resource.
- `RECRUITER`: `create:job`, `update:job`, `delete:job` (chỉ job của mình — ràng buộc ở tầng app, không ở DB), `read:application`, `update:application`, `read:cv`.
- `CANDIDATE`: `read:job`, `create:application`, `create:cv`, `delete:cv` (của mình).

### 1.4 `job_categories` (có cây cha-con qua `parent_id`)
Cấp 1 (parent_id NULL):
- Software Development
- DevOps & Infrastructure
- Data & AI
- QA/Testing
- IT Support

Danh sách phân rã Cấp 2 cho job_categories
1. Software Development (ID Cha: 1)
Nhóm này tập trung vào lập trình, phát triển logic ứng dụng.

Frontend Development (React, Angular, Vue, HTML/CSS...)

Backend Development (.NET, Java, Node.js, Python, Go...)

Fullstack Development (Kết hợp cả Front & Back)

Mobile Development (iOS, Android, Flutter, React Native)

Embedded & IoT Development (C/C++, Lập trình nhúng)

Game Development (Unity, Unreal Engine, C++)

2. DevOps & Infrastructure (ID Cha: 2)
Nhóm tập trung vào vận hành, triển khai hệ thống và điện toán đám mây.

DevOps Engineering (CI/CD, Docker, Kubernetes)

Cloud Engineering (AWS, Azure, GCP Architect/Engineer)

System Administration (Quản trị hệ thống Linux/Windows)

Database Administration (DBA) (Quản trị, tối ưu SQL/NoSQL)

Cybersecurity & Security Operations (SecOps) (An ninh mạng, bảo mật)

3. Data & AI (ID Cha: 3)
Nhóm xử lý dữ liệu và phát triển các mô hình trí tuệ nhân tạo (đây là phần cốt lõi bổ trợ cho AI Engine của bạn).

Data Engineering (Xây dựng pipeline, ETL, Big Data)

Data Analytics & Business Intelligence (BI) (Phân tích dữ liệu, làm dashboard)

Data Science (Nghiên cứu dữ liệu, thống kê chuyên sâu)

Machine Learning / Deep Learning Engineering (Huấn luyện các mô hình AI)

AI Product / Prompt Engineering (Ứng dụng và tối ưu Generative AI, LLMs)

4. QA/Testing (ID Cha: 4)
Nhóm kiểm thử và đảm bảo chất lượng phần mềm.

Manual Testing (Kiểm thử thủ công, viết testcase)

Automation Testing (Viết script test tự động: Selenium, Cypress...)

Performance / Security Testing (Kiểm thử hiệu năng, độ chịu tải, bảo mật ứng dụng)

5. IT Support (ID Cha: 5)
Nhóm hỗ trợ kỹ thuật và vận hành phần cứng/mạng nội bộ.

Helpdesk / IT Support (Hỗ trợ kỹ thuật phần cứng, phần mềm cho user)

Network Engineering (Thiết lập, quản trị hạ tầng mạng LAN/WAN, Cisco...)

Technical Support (Tier 2/3) (Hỗ trợ kỹ thuật chuyên sâu cho sản phẩm/khách hàng)

> Agent: tạo slug từ name (lowercase, dấu gạch ngang), ví dụ `information-technology`, `software-development`.

### 1.5 `skill_categories`
- Programming Language
- Framework & Library
- Database
- DevOps & Cloud
- Soft Skill
- Language (ngoại ngữ)
- Tool & Design

### 1.6 `skills` (gắn `category_id`)
- Programming Language: JavaScript, TypeScript, Python, Java, Go, C#, PHP
- Framework & Library: React, Node.js, Spring Boot, Django, .NET, Vue.js, NestJS
- Database: PostgreSQL, MySQL, MongoDB, Redis, Elasticsearch
- DevOps & Cloud: Docker, Kubernetes, AWS, GCP, CI/CD, Terraform
- Soft Skill: Communication, Teamwork, Problem Solving, Leadership, Time Management
- Language: English, Japanese, Korean
- Tool & Design: Figma, Photoshop, Jira, Git

Tất cả `status = 'ACTIVE'`.

### 1.7 `majors`
- Computer Science (CS)
- Software Engineering (SE)
- Information Systems (IS)
- Business Administration (BA)
- Information Security / Cyber Security
- Finance & Banking (FIN)
- Computer Networks and Data Communication

### 1.8 `subscriptions`
| name | price | duration_days | features_config (JSONB) | status |
|---|---|---|---|---|
| Free | 0 | 36500 | `{"max_active_jobs": 1, "ai_interview_credits": 2, "cv_match_per_month": 5}` | ACTIVE |
| Premium | 299000 | 30 | `{"max_active_jobs": 10, "ai_interview_credits": 20, "cv_match_per_month": 50}` | ACTIVE |
| Enterprise | 1499000 | 30 | `{"max_active_jobs": -1, "ai_interview_credits": -1, "cv_match_per_month": -1}` | ACTIVE |