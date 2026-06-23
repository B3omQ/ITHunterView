import { PublicHeader } from "@/components/layout/PublicHeader";

// (public) layout — không có auth guard
// Trang trong (public) KHÔNG import hook/service yêu cầu auth token

export default function PublicLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen flex flex-col bg-background text-foreground overflow-x-hidden">
      <PublicHeader />
      <main className="flex-1">
        {children}
      </main>
    </div>
  );
}
