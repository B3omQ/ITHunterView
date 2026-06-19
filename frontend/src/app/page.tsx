import Link from "next/link"

export default function Home() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center p-24 bg-background">
      <h1 className="text-4xl font-bold tracking-tight text-foreground">ITHunterView</h1>
      <p className="mt-4 text-lg text-muted-foreground mb-8">
        AI-Powered Tech Recruitment Platform
      </p>
      <Link href="/login" className="btn-primary px-8 py-3 rounded-xl font-semibold">
        Go to Login
      </Link>
    </main>
  )
}
