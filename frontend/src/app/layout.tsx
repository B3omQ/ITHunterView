import type { Metadata } from "next"
import { Inter } from "next/font/google"
import "./globals.css"
import ReactQueryProvider from "@/providers/ReactQueryProvider"

const inter = Inter({
  variable: "--font-inter",
  subsets: ["latin"],
  weight: ["300", "400", "500", "600", "700", "800"],
})

export const metadata: Metadata = {
  title: "ITHunterView – AI-Powered Tech Recruitment",
  description:
    "Connect top tech talent with leading companies using AI-powered matching and interview assistance.",
  keywords: "IT recruitment, tech jobs, AI interview, software developer jobs",
}

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" className={`${inter.variable} h-full`}>
      <body className="min-h-full flex flex-col antialiased">
        <ReactQueryProvider>{children}</ReactQueryProvider>
      </body>
    </html>
  )
}
