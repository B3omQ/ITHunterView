-- ==========================================================
--  RECRUITMENT / ATS / AI INTERVIEW PLATFORM - DATABASE SCHEMA
--  Dialect: PostgreSQL
--  Generated from Eraser.io ERD
-- ==========================================================

-- ==========================================================
-- 0. EXTENSIONS
-- ==========================================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto"; -- cho gen_random_uuid() nếu cần

-- ==========================================================
-- 0.1 ENUM TYPES
-- ==========================================================
CREATE TYPE user_status AS ENUM ('ACTIVE', 'INACTIVE', 'BANNED', 'PENDING_VERIFICATION');

CREATE TYPE company_verification_method AS ENUM ('BUSINESS_REGISTRATION', 'POA_AND_ID');
CREATE TYPE company_status AS ENUM ('PENDING', 'VERIFIED', 'REJECTED');

CREATE TYPE review_status AS ENUM ('PENDING', 'APPROVED', 'REJECTED');

CREATE TYPE skill_status AS ENUM ('ACTIVE', 'DEACTIVE');

CREATE TYPE job_type AS ENUM ('FULL_TIME', 'PART_TIME', 'CONTRACT', 'FREELANCE', 'INTERNSHIP');
CREATE TYPE job_status AS ENUM ('DRAFT', 'PENDING_REVIEW', 'PUBLISHED', 'REJECTED', 'EXPIRED', 'CLOSED');

CREATE TYPE application_status AS ENUM (
  'APPLIED', 'VIEWED', 'SHORTLISTED', 'INTERVIEWING',
  'OFFERED', 'HIRED', 'REJECTED', 'WITHDRAWN'
);

CREATE TYPE promotion_status AS ENUM ('ACTIVE', 'EXPIRED', 'CANCELLED');

CREATE TYPE difficulty_level AS ENUM ('EASY', 'MEDIUM', 'HARD');
CREATE TYPE interview_session_status AS ENUM ('IN_PROGRESS', 'COMPLETED', 'CANCELLED');

CREATE TYPE subscription_status AS ENUM ('ACTIVE', 'INACTIVE');
CREATE TYPE user_subscription_status AS ENUM ('ACTIVE', 'EXPIRED', 'CANCELLED');

CREATE TYPE payment_gateway AS ENUM ('VNPAY', 'MOMO', 'STRIPE', 'PAYPAL', 'BANK_TRANSFER');
CREATE TYPE payment_target_type AS ENUM ('SUBSCRIPTION', 'WALLET_TOPUP', 'JOB_PROMOTION');
CREATE TYPE payment_status AS ENUM ('PENDING', 'SUCCESS', 'FAILED', 'REFUNDED');

CREATE TYPE credit_transaction_type AS ENUM ('TOPUP', 'DEDUCT', 'REFUND', 'BONUS');

CREATE TYPE employment_type AS ENUM ('FULL_TIME', 'PART_TIME', 'CONTRACT', 'FREELANCE');

CREATE TYPE notification_type AS ENUM ('SYSTEM', 'APPLICATION', 'INTERVIEW', 'PAYMENT', 'PROMOTION');

CREATE TYPE email_log_status AS ENUM ('SENT', 'FAILED', 'PENDING');

CREATE TYPE activity_log_category AS ENUM ('AUTH', 'SYSTEM', 'DATA_MUTATION', 'SECURITY');
CREATE TYPE activity_log_status AS ENUM ('SUCCESS', 'FAIL');


-- ==========================================================
-- 1. IAM & PROFILES
-- ==========================================================

CREATE TABLE roles (
  id              SERIAL PRIMARY KEY,
  name            VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE permissions (
  id              SERIAL PRIMARY KEY,
  action          VARCHAR(100) NOT NULL,
  resource        VARCHAR(100) NOT NULL,
  UNIQUE (action, resource)
);

CREATE TABLE role_permissions (
  role_id         INT NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
  permission_id   INT NOT NULL REFERENCES permissions(id) ON DELETE CASCADE,
  PRIMARY KEY (role_id, permission_id)
);

CREATE TABLE users (
  id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  email           VARCHAR(255) NOT NULL UNIQUE,
  password_hash   VARCHAR(255) NOT NULL,
  role_id         INT REFERENCES roles(id) ON DELETE SET NULL,
  status          user_status NOT NULL DEFAULT 'PENDING_VERIFICATION',
  created_at      TIMESTAMP NOT NULL DEFAULT now(),
  updated_at      TIMESTAMP NOT NULL DEFAULT now(),
  deactive_at     TIMESTAMP
);

CREATE TABLE candidate_profiles (
  id                          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id                     UUID NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
  first_name                  VARCHAR(100),
  last_name                   VARCHAR(100),
  phone                       VARCHAR(30),
  location                    VARCHAR(255),
  about_me                    VARCHAR(2000),
  avatar_url                  VARCHAR(500),
  github_url                  VARCHAR(500),
  linkedIn_url                VARCHAR(500),
  portfolio_url               VARCHAR(500),
  is_visible_to_recruiters    BOOLEAN NOT NULL DEFAULT TRUE
);

-- companies table needs to exist before recruiter_profiles FK -> defined in section 2,
-- so we forward-declare recruiter_profiles after companies (see below, reordered).

CREATE TABLE email_verification_tokens (
  id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  token           VARCHAR(255) NOT NULL,
  expires_at      TIMESTAMP NOT NULL,
  is_used         BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE password_resets (
  id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  token           VARCHAR(255) NOT NULL,
  expires_at      TIMESTAMP NOT NULL,
  is_used         BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE refresh_tokens (
  id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  token           VARCHAR(500) NOT NULL,
  expires_at      TIMESTAMP NOT NULL,
  is_revoked      BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE user_activity_logs (
  id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id             UUID REFERENCES users(id) ON DELETE SET NULL,
  actor_role          VARCHAR(100),
  action_category     activity_log_category NOT NULL,
  actor_email         VARCHAR(255),
  action              VARCHAR(255) NOT NULL,
  status              activity_log_status NOT NULL,
  ip_address          VARCHAR(64),
  user_agent          VARCHAR(500),
  created_at          TIMESTAMP NOT NULL DEFAULT now()
);

-- ==========================================================
-- 2. MASTER DATA & COMPANIES
-- ==========================================================

CREATE TABLE companies (
  id                          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  name                        VARCHAR(255) NOT NULL,
  tax_code                    VARCHAR(50),
  headquarters_address        VARCHAR(500),
  industry                    VARCHAR(255),
  company_size                VARCHAR(50),
  description                 TEXT,
  website                     VARCHAR(500),
  logo_url                    VARCHAR(500),
  verification_method         company_verification_method,
  verification_document_url   VARCHAR(500),
  status                      company_status NOT NULL DEFAULT 'PENDING',
  created_by                  UUID REFERENCES users(id) ON DELETE SET NULL,
  updated_by                  UUID REFERENCES users(id) ON DELETE SET NULL,
  created_at                  TIMESTAMP NOT NULL DEFAULT now(),
  updated_at                  TIMESTAMP NOT NULL DEFAULT now(),
  deleted_at                  TIMESTAMP
);

CREATE TABLE recruiter_profiles (
  id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id             UUID NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
  company_id          UUID REFERENCES companies(id) ON DELETE SET NULL,
  full_name           VARCHAR(255),
  phone               VARCHAR(30),
  position_title      VARCHAR(255),
  avatar_url          VARCHAR(500)
);

CREATE TABLE company_reviews (
  id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  company_id      UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
  admin_id        UUID REFERENCES users(id) ON DELETE SET NULL,
  status          review_status NOT NULL DEFAULT 'PENDING',
  reject_reason   TEXT,
  created_at      TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE job_categories (
  id              SERIAL PRIMARY KEY,
  parent_id       INT REFERENCES job_categories(id) ON DELETE SET NULL,
  name            VARCHAR(255) NOT NULL,
  slug            VARCHAR(255) NOT NULL UNIQUE,
  created_by      UUID REFERENCES users(id) ON DELETE SET NULL,
  updated_by      UUID REFERENCES users(id) ON DELETE SET NULL
);

CREATE TABLE majors (
  id              SERIAL PRIMARY KEY,
  parent_id       INT REFERENCES majors(id) ON DELETE SET NULL,
  name            VARCHAR(255) NOT NULL,
  code            VARCHAR(50) UNIQUE,
  created_by      UUID REFERENCES users(id) ON DELETE SET NULL,
  updated_by      UUID REFERENCES users(id) ON DELETE SET NULL,
  deleted_at      TIMESTAMP
);

CREATE TABLE skill_categories (
  id              SERIAL PRIMARY KEY,
  name            VARCHAR(255) NOT NULL,
  created_by      UUID REFERENCES users(id) ON DELETE SET NULL,
  updated_by      UUID REFERENCES users(id) ON DELETE SET NULL
);

CREATE TABLE skills (
  id              SERIAL PRIMARY KEY,
  category_id     INT REFERENCES skill_categories(id) ON DELETE SET NULL,
  name            VARCHAR(255) NOT NULL,
  status          skill_status NOT NULL DEFAULT 'ACTIVE',
  created_by      UUID REFERENCES users(id) ON DELETE SET NULL,
  updated_by      UUID REFERENCES users(id) ON DELETE SET NULL
);

-- ==========================================================
-- 3. CANDIDATE PORTFOLIO
-- ==========================================================

CREATE TABLE cvs (
  id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  file_url        VARCHAR(500) NOT NULL,
  file_name       VARCHAR(255),
  file_size       INT,
  file_type       VARCHAR(20),
  is_primary      BOOLEAN NOT NULL DEFAULT FALSE,
  parsed_data     JSONB,
  created_at      TIMESTAMP NOT NULL DEFAULT now(),
  updated_at      TIMESTAMP NOT NULL DEFAULT now(),
  deleted_at      TIMESTAMP
);

CREATE TABLE user_skills (
  user_id             UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  skill_id            INT NOT NULL REFERENCES skills(id) ON DELETE CASCADE,
  proficiency_level   INT,
  PRIMARY KEY (user_id, skill_id)
);

CREATE TABLE candidate_experiences (
  id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id             UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  title               VARCHAR(255) NOT NULL,
  company_name        VARCHAR(255),
  company_id          UUID REFERENCES companies(id) ON DELETE SET NULL,
  location            VARCHAR(255),
  employment_type     employment_type,
  start_date          DATE,
  end_date            DATE,
  is_current          BOOLEAN NOT NULL DEFAULT FALSE,
  description         TEXT,
  created_at          TIMESTAMP NOT NULL DEFAULT now(),
  updated_at          TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE candidate_educations (
  id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id             UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  degree              VARCHAR(100),
  major_id            INT REFERENCES majors(id) ON DELETE SET NULL,
  institution_name    VARCHAR(255),
  gpa                 DECIMAL(4,2),
  max_gpa             DECIMAL(4,2),
  start_date          DATE,
  end_date            DATE,
  description         TEXT,
  created_at          TIMESTAMP NOT NULL DEFAULT now(),
  updated_at          TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE candidate_certifications (
  id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id                 UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  name                    VARCHAR(255) NOT NULL,
  issuing_organization    VARCHAR(255),
  issue_date              DATE,
  expiration_date         DATE,
  credential_url          VARCHAR(500),
  created_at              TIMESTAMP NOT NULL DEFAULT now()
);

-- ==========================================================
-- 4. ATS & JOBS
-- ==========================================================

CREATE TABLE job_postings (
  id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  job_code            VARCHAR(50) NOT NULL UNIQUE,
  recruiter_id        UUID NOT NULL REFERENCES users(id) ON DELETE SET NULL,
  company_id          UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
  category_id         INT REFERENCES job_categories(id) ON DELETE SET NULL,
  title               VARCHAR(255) NOT NULL,
  description         TEXT,
  responsibilities    TEXT,
  requirements        TEXT,
  benefits            TEXT,
  min_salary          DECIMAL(14,2),
  max_salary          DECIMAL(14,2),
  currency            VARCHAR(10),
  location            VARCHAR(255),
  job_type            job_type,
  status              job_status NOT NULL DEFAULT 'DRAFT',
  application_count   INT NOT NULL DEFAULT 0,
  view_count          INT NOT NULL DEFAULT 0,
  published_at        TIMESTAMP,
  expires_at          TIMESTAMP,
  created_at          TIMESTAMP NOT NULL DEFAULT now(),
  updated_at          TIMESTAMP NOT NULL DEFAULT now(),
  deleted_at          TIMESTAMP
);

CREATE TABLE job_skill_requirements (
  job_id          UUID NOT NULL REFERENCES job_postings(id) ON DELETE CASCADE,
  skill_id        INT NOT NULL REFERENCES skills(id) ON DELETE CASCADE,
  is_mandatory    BOOLEAN NOT NULL DEFAULT TRUE,
  PRIMARY KEY (job_id, skill_id)
);

CREATE TABLE job_reviews (
  id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  job_id          UUID NOT NULL REFERENCES job_postings(id) ON DELETE CASCADE,
  admin_id        UUID REFERENCES users(id) ON DELETE SET NULL,
  status          review_status NOT NULL DEFAULT 'PENDING',
  reject_reason   TEXT,
  created_at      TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE user_saved_jobs (
  user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  job_id          UUID NOT NULL REFERENCES job_postings(id) ON DELETE CASCADE,
  created_at      TIMESTAMP NOT NULL DEFAULT now(),
  PRIMARY KEY (user_id, job_id)
);

CREATE TABLE job_applications (
  id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  candidate_id    UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  job_id          UUID NOT NULL REFERENCES job_postings(id) ON DELETE CASCADE,
  cv_id           UUID REFERENCES cvs(id) ON DELETE SET NULL,
  cover_letter    TEXT,
  status          application_status NOT NULL DEFAULT 'APPLIED',
  created_at      TIMESTAMP NOT NULL DEFAULT now(),
  updated_at      TIMESTAMP NOT NULL DEFAULT now(),
  UNIQUE (candidate_id, job_id)
);

CREATE TABLE application_history (
  id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  application_id      UUID NOT NULL REFERENCES job_applications(id) ON DELETE CASCADE,
  from_status         VARCHAR(50),
  to_status           VARCHAR(50),
  changed_by          UUID REFERENCES users(id) ON DELETE SET NULL,
  created_at          TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE job_promotions (
  id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  job_id          UUID NOT NULL REFERENCES job_postings(id) ON DELETE CASCADE,
  start_date      TIMESTAMP NOT NULL,
  end_date        TIMESTAMP NOT NULL,
  status          promotion_status NOT NULL DEFAULT 'ACTIVE'
);

-- ==========================================================
-- 5. AI ENGINE
-- ==========================================================

CREATE TABLE cv_job_match_scores (
  id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  cv_id           UUID NOT NULL REFERENCES cvs(id) ON DELETE CASCADE,
  job_id          UUID NOT NULL REFERENCES job_postings(id) ON DELETE CASCADE,
  raw_jd_text     TEXT,
  match_score     DECIMAL(5,2),
  match_details   JSONB,
  updated_at      TIMESTAMP NOT NULL DEFAULT now(),
  UNIQUE (cv_id, job_id)
);

CREATE TABLE interview_question_bank (
  id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  category_id     INT REFERENCES job_categories(id) ON DELETE SET NULL,
  difficulty      difficulty_level,
  question_text   TEXT NOT NULL,
  sample_answer   TEXT,
  created_by      UUID REFERENCES users(id) ON DELETE SET NULL,
  updated_by      UUID REFERENCES users(id) ON DELETE SET NULL
);

CREATE TABLE interview_sessions (
  id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  candidate_id        UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  job_id              UUID REFERENCES job_postings(id) ON DELETE SET NULL,
  cv_id               UUID REFERENCES cvs(id) ON DELETE SET NULL,
  difficulty_level    difficulty_level,
  status              interview_session_status NOT NULL DEFAULT 'IN_PROGRESS',
  started_at          TIMESTAMP,
  ended_at            TIMESTAMP
);

CREATE TABLE interview_answers (
  id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  session_id              UUID NOT NULL REFERENCES interview_sessions(id) ON DELETE CASCADE,
  question_id             UUID REFERENCES interview_question_bank(id) ON DELETE SET NULL,
  parent_answer_id        UUID REFERENCES interview_answers(id) ON DELETE SET NULL,
  question_text           TEXT,
  audio_url               VARCHAR(500),
  candidate_transcript    TEXT,
  ai_feedback             TEXT,
  score_logic             INT,
  score_tech              INT,
  score_communication     INT,
  created_at              TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE interview_reports (
  id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  session_id          UUID NOT NULL UNIQUE REFERENCES interview_sessions(id) ON DELETE CASCADE,
  total_score         DECIMAL(5,2),
  overall_feedback    TEXT
);

CREATE TABLE learning_paths (
  id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  candidate_id    UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  session_id      UUID UNIQUE REFERENCES interview_sessions(id) ON DELETE SET NULL,
  path_data       JSONB,
  created_at      TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE ai_api_usage_logs (
  id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  reference_id        UUID, -- polymorphic: id của entity liên quan (interview_session, cv_job_match_scores...)
  prompt_tokens       INT,
  completion_tokens   INT,
  model_name          VARCHAR(100),
  cost_usd            DECIMAL(10,4),
  created_at          TIMESTAMP NOT NULL DEFAULT now()
);

-- ==========================================================
-- 6. FINANCE & BILLING
-- ==========================================================

CREATE TABLE subscriptions (
  id                  SERIAL PRIMARY KEY,
  name                VARCHAR(100) NOT NULL,
  price               DECIMAL(12,2) NOT NULL,
  duration_days       INT NOT NULL,
  features_config     JSONB,
  status              subscription_status NOT NULL DEFAULT 'ACTIVE',
  created_by          UUID REFERENCES users(id) ON DELETE SET NULL,
  updated_by          UUID REFERENCES users(id) ON DELETE SET NULL
);

CREATE TABLE user_subscriptions (
  id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  sub_id          INT NOT NULL REFERENCES subscriptions(id) ON DELETE RESTRICT,
  start_date      TIMESTAMP NOT NULL,
  end_date        TIMESTAMP NOT NULL,
  status          user_subscription_status NOT NULL DEFAULT 'ACTIVE'
);

CREATE TABLE user_wallets (
  id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id         UUID NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
  balance         INT NOT NULL DEFAULT 0,
  updated_at      TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE payments (
  id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id                 UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  amount                  DECIMAL(14,2) NOT NULL,
  currency                VARCHAR(10),
  credits_granted         INT,
  payment_gateway         payment_gateway,
  gateway_transaction_id  VARCHAR(255),
  target_type             payment_target_type,
  target_id               UUID, -- polymorphic: id của subscription/wallet topup/job_promotion
  status                  payment_status NOT NULL DEFAULT 'PENDING',
  created_at              TIMESTAMP NOT NULL DEFAULT now(),
  updated_at              TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE credit_transactions (
  id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  wallet_id           UUID NOT NULL REFERENCES user_wallets(id) ON DELETE CASCADE,
  amount              INT NOT NULL,
  transaction_type    credit_transaction_type NOT NULL,
  reference_id        UUID, -- polymorphic: id của payment, job_promotion...
  description         TEXT,
  created_at          TIMESTAMP NOT NULL DEFAULT now()
);

-- ==========================================================
-- 7. SYSTEM OPS
-- ==========================================================

CREATE TABLE system_configs (
  config_key      VARCHAR(150) PRIMARY KEY,
  config_value    TEXT,
  description     TEXT,
  updated_by      UUID REFERENCES users(id) ON DELETE SET NULL
);

CREATE TABLE system_prompts (
  prompt_key      VARCHAR(150) PRIMARY KEY,
  content         TEXT NOT NULL,
  version         INT NOT NULL DEFAULT 1,
  updated_by      UUID REFERENCES users(id) ON DELETE SET NULL
);

CREATE TABLE notifications (
  id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  title           VARCHAR(255),
  message         TEXT,
  type            notification_type,
  is_read         BOOLEAN NOT NULL DEFAULT FALSE,
  created_at      TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE sys_email_logs (
  id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  reference_type  VARCHAR(100),
  reference_id    UUID, -- polymorphic
  to_email        VARCHAR(255) NOT NULL,
  status          email_log_status NOT NULL DEFAULT 'PENDING',
  error_message   TEXT,
  created_at      TIMESTAMP NOT NULL DEFAULT now()
);

-- ==========================================================
-- INDEXES (cho các khóa ngoại tra cứu thường xuyên)
-- ==========================================================
CREATE INDEX idx_users_role_id ON users(role_id);
CREATE INDEX idx_candidate_profiles_user_id ON candidate_profiles(user_id);
CREATE INDEX idx_recruiter_profiles_user_id ON recruiter_profiles(user_id);
CREATE INDEX idx_recruiter_profiles_company_id ON recruiter_profiles(company_id);
CREATE INDEX idx_email_verification_tokens_user_id ON email_verification_tokens(user_id);
CREATE INDEX idx_password_resets_user_id ON password_resets(user_id);
CREATE INDEX idx_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX idx_user_activity_logs_user_id ON user_activity_logs(user_id);

CREATE INDEX idx_company_reviews_company_id ON company_reviews(company_id);
CREATE INDEX idx_job_categories_parent_id ON job_categories(parent_id);
CREATE INDEX idx_skills_category_id ON skills(category_id);

CREATE INDEX idx_cvs_user_id ON cvs(user_id);
CREATE INDEX idx_candidate_experiences_user_id ON candidate_experiences(user_id);
CREATE INDEX idx_candidate_educations_user_id ON candidate_educations(user_id);
CREATE INDEX idx_candidate_educations_major_id ON candidate_educations(major_id);
CREATE INDEX idx_candidate_certifications_user_id ON candidate_certifications(user_id);

CREATE INDEX idx_job_postings_recruiter_id ON job_postings(recruiter_id);
CREATE INDEX idx_job_postings_company_id ON job_postings(company_id);
CREATE INDEX idx_job_postings_category_id ON job_postings(category_id);
CREATE INDEX idx_job_postings_status ON job_postings(status);
CREATE INDEX idx_job_reviews_job_id ON job_reviews(job_id);
CREATE INDEX idx_job_applications_candidate_id ON job_applications(candidate_id);
CREATE INDEX idx_job_applications_job_id ON job_applications(job_id);
CREATE INDEX idx_application_history_application_id ON application_history(application_id);
CREATE INDEX idx_job_promotions_job_id ON job_promotions(job_id);

CREATE INDEX idx_cv_job_match_scores_cv_id ON cv_job_match_scores(cv_id);
CREATE INDEX idx_cv_job_match_scores_job_id ON cv_job_match_scores(job_id);
CREATE INDEX idx_interview_sessions_candidate_id ON interview_sessions(candidate_id);
CREATE INDEX idx_interview_sessions_job_id ON interview_sessions(job_id);
CREATE INDEX idx_interview_answers_session_id ON interview_answers(session_id);
CREATE INDEX idx_interview_answers_question_id ON interview_answers(question_id);
CREATE INDEX idx_learning_paths_candidate_id ON learning_paths(candidate_id);

CREATE INDEX idx_user_subscriptions_user_id ON user_subscriptions(user_id);
CREATE INDEX idx_user_subscriptions_sub_id ON user_subscriptions(sub_id);
CREATE INDEX idx_payments_user_id ON payments(user_id);
CREATE INDEX idx_credit_transactions_wallet_id ON credit_transactions(wallet_id);

CREATE INDEX idx_notifications_user_id ON notifications(user_id);

-- ==========================================================
-- END OF SCHEMA
-- ==========================================================
