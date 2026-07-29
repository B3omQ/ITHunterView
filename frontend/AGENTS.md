<!-- BEGIN:nextjs-agent-rules -->
# This is NOT the Next.js you know

This version has breaking changes — APIs, conventions, and file structure may all differ from your training data. Read the relevant guide in `node_modules/next/dist/docs/` before writing any code. Heed deprecation notices.
<!-- END:nextjs-agent-rules -->

## Frontend workflow

- Inherit all repository rules from `../AGENTS.md`.
- Use `npm ci` after lockfile changes; do not mix npm with another package manager.
- Run `npm run check:quick` while iterating.
- Run `npm run check` before completion. Global lint currently has recorded
  legacy debt, so lint every changed file directly and do not add violations.
- Keep strict TypeScript. Do not introduce `any` or disable compiler/lint rules.
- Add Vitest tests beside source as `*.test.ts` or `*.test.tsx`.

## Boundaries

- Pages and layouts live in `src/app/` and follow the existing role route groups.
- HTTP calls belong in `src/services/`; shared server-state orchestration belongs
  in `src/hooks/`.
- Reuse types from `src/types/`, TanStack Query for server state, and the existing
  Zustand stores for local client state.
- Prefer existing shared components and `src/components/ui/` shadcn primitives.
- Follow `../.agents/Frontend/DESIGN.md`; do not add a second design system.
