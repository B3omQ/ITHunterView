// (public) layout — không có auth guard
// Trang trong (public) KHÔNG import hook/service yêu cầu auth token

export default function PublicLayout({ children }: { children: React.ReactNode }) {
  return <>{children}</>;
}
