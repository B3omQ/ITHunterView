# AI Mock Interview Walkthrough

We have successfully implemented the AI Mock Interview chat feature, enabling candidates to practice their interviews with a turn-based AI chatbot, including dynamic AI model switching (Gemini, OpenAI, Claude).

## Changes Made

### 1. Database & Domain Layer
- **`InterviewSessions` Entity**: Modified [InterviewSessions.cs](file:///c:/Users/LAPTOP/OneDrive/Documents/GitHub/ITHunterView_backup/backend/ITHunterview.Domain/Entities/InterviewSessions.cs) to add the `ai_provider` property.
- **Migration**: Added and successfully applied the EF migration `AddAiProviderToInterviewSessions` to append the `ai_provider` column to the `interview_sessions` PostgreSQL database table.

### 2. Core AI Service Overload
- **`IAiService`**: Overloaded the `GenerateTextAsync` method signature to support passing an optional `providerName`.
- **`AiService`**: Implemented the signature to resolve either the default system-wide active AI provider or the session-specific chosen provider.

### 3. Repository Layer
- **`IInterviewSessionRepository`**: Interface & implementation to handle CRUD queries for candidate mock interview sessions.
- **`IInterviewAnswerRepository`**: Interface & implementation to handle conversation threads and fetch active turn records.

### 4. Application DTOs & Use Case
- **Interview DTOs**: Created request/response payloads (`CreateInterviewSessionDto`, `InterviewSessionDto`, `InterviewAnswerDto`, `SubmitReplyDto`, etc.).
- **`InterviewUseCase`**: Completed logic to:
  - Create session, retrieve matching parsed CV details and Job posting JD as prompt context.
  - Submit user reply, evaluate technical/logical/communication capabilities via AI, parse results dynamically from structured JSON output, and queue the next question.
  - Manage live model switching.
  - End sessions.

### 5. Web API Controller
- **`InterviewController`**: Handled authentication routing (Policy: `CandidateOnly`), input validation, and mapped HTTP methods to Use Case methods.

### 6. Frontend Integration
- **Types**: Added [interview.types.ts](file:///c:/Users/LAPTOP/OneDrive/Documents/GitHub/ITHunterView_backup/frontend/src/types/interview.types.ts).
- **Service**: Added [interview.service.ts](file:///c:/Users/LAPTOP/OneDrive/Documents/GitHub/ITHunterView_backup/frontend/src/services/interview.service.ts).
- **Hooks**: Added TanStack Query wrapper hooks in [useInterview.ts](file:///c:/Users/LAPTOP/OneDrive/Documents/GitHub/ITHunterView_backup/frontend/src/hooks/useInterview.ts).
- **Main List Page**: Implemented a glassy, modern dashboard in [page.tsx](file:///c:/Users/LAPTOP/OneDrive/Documents/GitHub/ITHunterView_backup/frontend/src/app/(candidate)/candidate/interview/page.tsx) to list history, select settings (CV, JD, Difficulty, AI Model), and launch sessions.
- **Active Session Chat Page**: Created a premium turn-based chat in [page.tsx](file:///c:/Users/LAPTOP/OneDrive/Documents/GitHub/ITHunterView_backup/frontend/src/app/(candidate)/candidate/interview/[sessionId]/page.tsx) rendering:
  - Sidebar with dynamic model selector, difficulty indicators, CV references, and complete session triggers.
  - Thread bubbles representing AI interviewer questions and candidate text replies.
  - Score progress bars (Logic, Tech, Communication) accompanied by detailed critique cards.
  - Keyboard listener, auto-scroll behaviors, and loading status effects.

## Verification & Validation

### Automated Checks
- Backend compiles perfectly: `0 errors`.
- Frontend Next.js build compilation.

### Manual Testing Flow
1. Start application services using `run.bat`.
2. Go to **AI Mock Interview** section in candidate view.
3. Click "Start New Session", configure difficulty and provider.
4. Interact with the chat box, answering the AI's technical questions.
5. Review AI's score evaluation and next question.
6. Toggle the selected AI model provider dropdown to switch providers (e.g. Gemini to Claude) during the interview.
7. Click "End Interview" to finalize the session.
