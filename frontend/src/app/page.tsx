"use client"

import { useState, useEffect } from "react"
import Link from "next/link"
import { Logo } from "@/components/layout/Logo"
import { useAuthStore } from "@/store/auth.store"
import { getDashboardPath } from "@/lib/constants"
import { PublicHeader } from "@/components/layout/PublicHeader"


// Inline SVG Icon components to prevent Next.js + Turbopack compilation hang
function SearchIcon({ size = 16, className = "" }: { size?: number; className?: string }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className}>
      <circle cx="11" cy="11" r="8"></circle>
      <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
    </svg>
  )
}

function MapPinIcon({ size = 16, className = "" }: { size?: number; className?: string }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className}>
      <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path>
      <circle cx="12" cy="10" r="3"></circle>
    </svg>
  )
}

function ArrowRightIcon({ size = 16, className = "" }: { size?: number; className?: string }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className}>
      <line x1="5" y1="12" x2="19" y2="12"></line>
      <polyline points="12 5 19 12 12 19"></polyline>
    </svg>
  )
}

function UploadIcon({ size = 22, className = "" }: { size?: number; className?: string }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className}>
      <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
      <polyline points="17 8 12 3 7 8"></polyline>
      <line x1="12" y1="3" x2="12" y2="15"></line>
    </svg>
  )
}

function ZapIcon({ size = 22, className = "" }: { size?: number; className?: string }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className}>
      <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"></polygon>
    </svg>
  )
}

function TargetIcon({ size = 22, className = "" }: { size?: number; className?: string }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className}>
      <circle cx="12" cy="12" r="10"></circle>
      <circle cx="12" cy="12" r="6"></circle>
      <circle cx="12" cy="12" r="2"></circle>
    </svg>
  )
}

function ChevronLeftIcon({ size = 18, className = "" }: { size?: number; className?: string }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className}>
      <polyline points="15 18 9 12 15 6"></polyline>
    </svg>
  )
}

function ChevronRightIcon({ size = 18, className = "" }: { size?: number; className?: string }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className}>
      <polyline points="9 18 15 12 9 6"></polyline>
    </svg>
  )
}

function CheckIcon({ size = 14, className = "" }: { size?: number; className?: string }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" className={className}>
      <polyline points="20 6 9 17 4 12"></polyline>
    </svg>
  )
}

function LogOutIcon({ size = 16, className = "" }: { size?: number; className?: string }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className}>
      <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path>
      <polyline points="16 17 21 12 16 7"></polyline>
      <line x1="21" y1="12" x2="9" y2="12"></line>
    </svg>
  )
}

function LayoutDashboardIcon({ size = 16, className = "" }: { size?: number; className?: string }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className}>
      <rect x="3" y="3" width="7" height="9"></rect>
      <rect x="14" y="3" width="7" height="5"></rect>
      <rect x="14" y="12" width="7" height="9"></rect>
      <rect x="3" y="16" width="7" height="5"></rect>
    </svg>
  )
}

export default function Home() {
  const { user, logout } = useAuthStore()
  const [searchTitle, setSearchTitle] = useState("")
  const [searchLoc, setSearchLoc] = useState("")
  const [mounted, setMounted] = useState(false)

  useEffect(() => {
    setMounted(true)
  }, [])

  const featuredJobs = [
    {
      id: 1,
      title: "Frontend Developer",
      company: "VNG Corporation",
      logoColor: "bg-orange-500",
      logoText: "V",
      salary: "$800 – $1,400/mo",
      location: "Ho Chi Minh City",
      tags: ["React", "TypeScript"],
      badge: { text: "Hot", color: "bg-red-500/10 text-red-500 border border-red-500/20" },
    },
    {
      id: 2,
      title: "Backend Engineer (Java)",
      company: "FPT Software",
      logoColor: "bg-blue-600",
      logoText: "F",
      salary: "$700 – $1,200/mo",
      location: "Hanoi",
      tags: ["Java", "Spring Boot"],
      badge: { text: "New", color: "bg-green-500/10 text-green-500 border border-green-500/20" },
    },
    {
      id: 3,
      title: "Data Analyst",
      company: "Grab Vietnam",
      logoColor: "bg-emerald-600",
      logoText: "G",
      salary: "$900 – $1,600/mo",
      location: "Ho Chi Minh City",
      tags: ["SQL", "Python"],
      badge: null,
    },
    {
      id: 4,
      title: "Mobile Developer (iOS)",
      company: "MoMo",
      logoColor: "bg-pink-600",
      logoText: "M",
      salary: "$1,000 – $1,800/mo",
      location: "Ho Chi Minh City",
      tags: ["Swift", "SwiftUI"],
      badge: { text: "Urgent", color: "bg-amber-500/10 text-amber-500 border border-amber-500/20" },
    },
    {
      id: 5,
      title: "DevOps Engineer",
      company: "Tiki",
      logoColor: "bg-sky-500",
      logoText: "T",
      salary: "$1,100 – $2,000/mo",
      location: "Hanoi",
      tags: ["Docker", "Kubernetes"],
      badge: null,
    },
  ]

  const popularSearches = ["Frontend Developer", "Data Analyst", "Mobile Dev", "DevOps"]

  return (
    <div className="min-h-screen flex flex-col bg-background text-foreground overflow-x-hidden">
      {/* Header */}
      <PublicHeader />

      {/* Hero Section */}
      <section className="relative pt-20 pb-16 md:pt-28 md:pb-24 px-4 sm:px-6 lg:px-8 max-w-7xl mx-auto w-full text-center">
        {/* Badge */}
        <div className="inline-flex items-center gap-1.5 bg-primary/10 text-primary text-xs font-semibold px-4 py-1.5 rounded-full mb-6">
          <span>#1 IT Job Platform for Fresh IT Graduates in Vietnam</span>
          <span>🇻🇳</span>
        </div>

        {/* Headline */}
        <h1 className="text-4xl sm:text-5xl md:text-6xl font-extrabold text-foreground tracking-tight max-w-4xl mx-auto leading-tight">
          Land your first IT job — <span className="text-primary">faster.</span>
        </h1>

        <p className="mt-5 text-base sm:text-lg md:text-xl text-muted-foreground max-w-2xl mx-auto leading-relaxed">
          Smart job matching, AI-powered CV optimization, and real mock interviews to get you hired at top tech companies.
        </p>

        {/* Search Bar */}
        <div className="mt-10 max-w-3xl mx-auto bg-card border border-border rounded-2xl p-2.5 shadow-md flex flex-col md:flex-row gap-2.5 items-stretch md:items-center">
          <div className="flex-1 flex items-center gap-2.5 px-3 min-w-0 border-b md:border-b-0 md:border-r border-border pb-2.5 md:pb-0">
            <SearchIcon className="text-muted-foreground flex-shrink-0" size={18} />
            <input
              type="text"
              placeholder="Job title, keywords, company..."
              value={searchTitle}
              onChange={(e) => setSearchTitle(e.target.value)}
              className="w-full bg-transparent border-0 outline-none text-sm placeholder:text-muted-foreground py-1 text-foreground"
            />
          </div>

          <div className="flex-1 flex items-center gap-2.5 px-3 min-w-0 pb-2.5 md:pb-0">
            <MapPinIcon className="text-muted-foreground flex-shrink-0" size={18} />
            <input
              type="text"
              placeholder="Location (Hanoi, Ho Chi Minh...)"
              value={searchLoc}
              onChange={(e) => setSearchLoc(e.target.value)}
              className="w-full bg-transparent border-0 outline-none text-sm placeholder:text-muted-foreground py-1 text-foreground"
            />
          </div>

          <button className="h-11 px-6 rounded-xl bg-primary hover:bg-primary/95 text-primary-foreground font-semibold text-sm flex items-center justify-center gap-2 transition-all cursor-pointer">
            <SearchIcon size={16} />
            <span>Search Jobs</span>
          </button>
        </div>

        {/* Popular Tags */}
        <div className="mt-6 flex flex-wrap items-center justify-center gap-2.5 text-xs">
          <span className="text-muted-foreground font-medium">Popular:</span>
          {popularSearches.map((tag) => (
            <button
              key={tag}
              onClick={() => setSearchTitle(tag)}
              className="px-3 py-1 rounded-lg border border-border bg-card hover:border-primary/50 text-muted-foreground hover:text-primary transition-all cursor-pointer"
            >
              {tag}
            </button>
          ))}
        </div>

        {/* Statistics highlights */}
        <div className="mt-12 flex flex-wrap items-center justify-center gap-4 text-xs font-semibold">
          <span className="bg-primary/5 text-primary border border-primary/10 px-4 py-2 rounded-xl">10,000+ Jobs</span>
          <span className="bg-primary/5 text-primary border border-primary/10 px-4 py-2 rounded-xl">500+ Companies</span>
          <span className="bg-primary/5 text-primary border border-primary/10 px-4 py-2 rounded-xl">AI-Powered Matching</span>
        </div>
      </section>

      {/* Featured Jobs Section */}
      <section id="jobs" className="py-20 bg-muted/30 border-y border-border">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-end justify-between mb-10">
            <div>
              <h2 className="text-3xl font-extrabold tracking-tight text-foreground">Featured Jobs</h2>
              <p className="text-muted-foreground mt-2 text-sm sm:text-base">
                Hand-picked opportunities for fresh IT graduates
              </p>
            </div>
            {/* Navigation Arrows */}
            <div className="flex items-center gap-2">
              <button className="w-10 h-10 rounded-full border border-border bg-white flex items-center justify-center text-muted-foreground hover:text-foreground hover:border-primary transition-all cursor-pointer">
                <ChevronLeftIcon size={18} />
              </button>
              <button className="w-10 h-10 rounded-full border border-border bg-white flex items-center justify-center text-muted-foreground hover:text-foreground hover:border-primary transition-all cursor-pointer">
                <ChevronRightIcon size={18} />
              </button>
            </div>
          </div>

          {/* Jobs Grid */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
            {featuredJobs.map((job) => (
              <div
                key={job.id}
                className="bg-card border border-border hover:border-primary/30 rounded-2xl p-6 shadow-sm hover:shadow-md transition-all group flex flex-col justify-between"
              >
                <div>
                  <div className="flex items-start justify-between gap-4 mb-4">
                    {/* Company Logo representation */}
                    <div className={`w-12 h-12 rounded-xl ${job.logoColor} text-white font-extrabold flex items-center justify-center shadow-sm`}>
                      {job.logoText}
                    </div>

                    {/* Badge */}
                    {job.badge && (
                      <span className={`text-[10px] uppercase font-bold px-2 py-0.5 rounded-full ${job.badge.color}`}>
                        {job.badge.text}
                      </span>
                    )}
                  </div>

                  <p className="text-xs text-muted-foreground font-medium mb-1">{job.company}</p>
                  <h3 className="text-lg font-bold text-foreground group-hover:text-primary transition-colors line-clamp-1">
                    {job.title}
                  </h3>

                  {/* Tech stack */}
                  <div className="flex flex-wrap gap-1.5 mt-3">
                    {job.tags.map((tag) => (
                      <span key={tag} className="text-xs px-2.5 py-0.5 rounded-md bg-muted text-muted-foreground border border-border/50">
                        {tag}
                      </span>
                    ))}
                  </div>
                </div>

                <div className="mt-6 pt-4 border-t border-border/50 flex items-center justify-between text-xs">
                  <div className="space-y-1">
                    <p className="font-bold text-foreground">{job.salary}</p>
                    <p className="text-muted-foreground flex items-center gap-1">
                      <MapPinIcon size={12} /> {job.location}
                    </p>
                  </div>
                  <Link href={`/jobs/${job.id}`} className="text-primary font-semibold flex items-center gap-1 hover:gap-1.5 transition-all">
                    <span>View Job</span>
                    <ArrowRightIcon size={14} />
                  </Link>
                </div>
              </div>
            ))}
          </div>

          <div className="mt-12 text-center">
            <Link
              href="/jobs"
              className="inline-flex items-center gap-1.5 text-sm font-semibold text-primary hover:text-primary/80 transition-colors"
            >
              <span>Browse all 10,000+ jobs</span>
              <ArrowRightIcon size={14} />
            </Link>
          </div>
        </div>
      </section>

      {/* How It Works Section */}
      <section id="mock-interview" className="py-20 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 w-full">
        <div className="text-center mb-16">
          <span className="text-primary text-xs font-semibold uppercase tracking-wider bg-primary/10 px-3 py-1 rounded-full">
            Simple 3-step process
          </span>
          <h2 className="text-3xl sm:text-4xl font-extrabold text-foreground mt-4">How It Works</h2>
          <p className="text-muted-foreground mt-3 text-sm sm:text-base max-w-md mx-auto">
            From fresh-grad to hired — in as few as 4 weeks.
          </p>
        </div>

        {/* Steps container */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-10 md:gap-6 relative">
          {/* Connector Line for Desktop */}
          <div className="hidden md:block absolute top-[28%] left-[16%] right-[16%] h-0.5 bg-border -z-10" />

          {/* Step 1 */}
          <div className="flex flex-col items-center text-center p-4">
            <div className="w-14 h-14 rounded-full bg-primary flex items-center justify-center text-white mb-5 shadow-lg shadow-indigo-500/20 relative">
              <UploadIcon size={22} />
              <span className="absolute -top-1.5 -right-1.5 w-6 h-6 rounded-full bg-white border-2 border-primary text-primary text-xs font-bold flex items-center justify-center">
                1
              </span>
            </div>
            <p className="text-xs uppercase font-bold text-primary tracking-wider mb-2">Step 01</p>
            <h3 className="text-lg font-bold text-foreground mb-2">Upload CV</h3>
            <p className="text-sm text-muted-foreground leading-relaxed max-w-xs">
              Upload your CV and let our AI analyze your skills, experience, and career goals in seconds.
            </p>
          </div>

          {/* Step 2 */}
          <div className="flex flex-col items-center text-center p-4">
            <div className="w-14 h-14 rounded-full bg-primary flex items-center justify-center text-white mb-5 shadow-lg shadow-indigo-500/20 relative">
              <ZapIcon size={22} />
              <span className="absolute -top-1.5 -right-1.5 w-6 h-6 rounded-full bg-white border-2 border-primary text-primary text-xs font-bold flex items-center justify-center">
                2
              </span>
            </div>
            <p className="text-xs uppercase font-bold text-primary tracking-wider mb-2">Step 02</p>
            <h3 className="text-lg font-bold text-foreground mb-2">Match & Optimize</h3>
            <p className="text-sm text-muted-foreground leading-relaxed max-w-xs">
              Get matched with the best IT jobs and receive AI-powered suggestions to improve your CV for each role.
            </p>
          </div>

          {/* Step 3 */}
          <div className="flex flex-col items-center text-center p-4">
            <div className="w-14 h-14 rounded-full bg-primary flex items-center justify-center text-white mb-5 shadow-lg shadow-indigo-500/20 relative">
              <TargetIcon size={22} />
              <span className="absolute -top-1.5 -right-1.5 w-6 h-6 rounded-full bg-white border-2 border-primary text-primary text-xs font-bold flex items-center justify-center">
                3
              </span>
            </div>
            <p className="text-xs uppercase font-bold text-primary tracking-wider mb-2">Step 03</p>
            <h3 className="text-lg font-bold text-foreground mb-2">Practice & Apply</h3>
            <p className="text-sm text-muted-foreground leading-relaxed max-w-xs">
              Sharpen your skills with real mock interviews, get feedback, and apply with confidence.
            </p>
          </div>
        </div>
      </section>

      {/* Pricing Section */}
      <section id="pricing" className="py-20 bg-muted/30 border-t border-border">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-16">
            <span className="text-primary text-xs font-semibold uppercase tracking-wider bg-primary/10 px-3 py-1 rounded-full">
              Transparent pricing
            </span>
            <h2 className="text-3xl sm:text-4xl font-extrabold text-foreground mt-4">Plans for every stage</h2>
            <p className="text-muted-foreground mt-3 text-sm sm:text-base max-w-md mx-auto">
              Start for free, upgrade when you&apos;re ready to accelerate.
            </p>
          </div>

          {/* Pricing Grid */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-8 items-stretch max-w-6xl mx-auto">
            {/* Free Plan */}
            <div className="bg-card border border-border rounded-3xl p-8 flex flex-col justify-between shadow-sm relative">
              <div>
                <p className="font-bold text-foreground text-lg mb-2">Free</p>
                <div className="flex items-baseline mb-4">
                  <span className="text-3xl sm:text-4xl font-extrabold text-foreground">0đ</span>
                  <span className="text-muted-foreground text-xs font-medium ml-1">/lifetime</span>
                </div>
                <p className="text-xs text-muted-foreground mb-8">Get started with the basics. No credit card required.</p>

                <ul className="space-y-3.5">
                  {[
                    "Browse up to 20 jobs/day",
                    "1 CV template",
                    "Basic job match score",
                    "Community forums",
                  ].map((feat) => (
                    <li key={feat} className="flex items-start gap-2.5 text-xs text-muted-foreground">
                      <CheckIcon className="text-primary mt-0.5 flex-shrink-0" size={14} />
                      <span>{feat}</span>
                    </li>
                  ))}
                </ul>
              </div>

              <button className="mt-8 w-full h-11 rounded-xl border border-primary text-primary hover:bg-primary/5 font-semibold text-sm transition-all cursor-pointer">
                Start Free
              </button>
            </div>

            {/* Pro Plan */}
            <div className="bg-primary text-primary-foreground border border-primary rounded-3xl p-8 flex flex-col justify-between shadow-md relative scale-105 z-10">
              <span className="absolute -top-3 left-1/2 -translate-x-1/2 bg-white text-primary text-[10px] uppercase font-extrabold px-3 py-1 rounded-full border border-primary/20 shadow-sm">
                Most Popular
              </span>
              <div>
                <p className="font-bold text-lg mb-2 text-white">Pro</p>
                <div className="flex items-baseline mb-4">
                  <span className="text-3xl sm:text-4xl font-extrabold text-white">199,000đ</span>
                  <span className="text-primary-foreground/80 text-xs font-medium ml-1">/month</span>
                </div>
                <p className="text-xs text-primary-foreground/80 mb-8">Everything you need to land your first tech job faster.</p>

                <ul className="space-y-3.5">
                  {[
                    "Unlimited job browsing",
                    "AI CV optimization",
                    "10 mock interviews/month",
                    "Interview feedback reports",
                    "Priority job alerts",
                  ].map((feat) => (
                    <li key={feat} className="flex items-start gap-2.5 text-xs text-primary-foreground/95">
                      <CheckIcon className="text-white mt-0.5 flex-shrink-0" size={14} />
                      <span>{feat}</span>
                    </li>
                  ))}
                </ul>
              </div>

              <button className="mt-8 w-full h-11 rounded-xl bg-white hover:bg-white/95 text-primary font-bold text-sm transition-all shadow-sm cursor-pointer">
                Start 7-day Trial
              </button>
            </div>

            {/* Career Boost Plan */}
            <div className="bg-card border border-border rounded-3xl p-8 flex flex-col justify-between shadow-sm relative">
              <div>
                <p className="font-bold text-foreground text-lg mb-2">Career Boost</p>
                <div className="flex items-baseline mb-4">
                  <span className="text-3xl sm:text-4xl font-extrabold text-foreground">399,000đ</span>
                  <span className="text-muted-foreground text-xs font-medium ml-1">/month</span>
                </div>
                <p className="text-xs text-muted-foreground mb-8">Dedicated support and advanced tools for serious job seekers.</p>

                <ul className="space-y-3.5">
                  {[
                    "Everything in Pro",
                    "Unlimited mock interviews",
                    "1-on-1 career coaching",
                    "Resume review by experts",
                    "LinkedIn profile optimization",
                  ].map((feat) => (
                    <li key={feat} className="flex items-start gap-2.5 text-xs text-muted-foreground">
                      <CheckIcon className="text-primary mt-0.5 flex-shrink-0" size={14} />
                      <span>{feat}</span>
                    </li>
                  ))}
                </ul>
              </div>

              <button className="mt-8 w-full h-11 rounded-xl border border-primary text-primary hover:bg-primary/5 font-semibold text-sm transition-all cursor-pointer">
                Get Career Boost
              </button>
            </div>
          </div>

          <div className="mt-12 text-center">
            <Link
              href="/pricing"
              className="inline-flex items-center gap-1.5 text-sm font-semibold text-primary hover:text-primary/80 transition-colors"
            >
              <span>View All Plans</span>
              <ArrowRightIcon size={14} />
            </Link>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="bg-card border-t border-border mt-auto pt-16 pb-8">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 md:grid-cols-5 gap-8 mb-12">
            {/* Logo and Description */}
            <div className="md:col-span-2 space-y-4">
              <Logo size="md" href="/" />
              <p className="text-xs text-muted-foreground max-w-sm leading-relaxed">
                Vietnam&apos;s smartest platform to help fresh IT graduates land their dream first job — faster, with AI-powered matching and mock interviews.
              </p>
              {/* Social Icons Placeholder */}
              <div className="flex items-center gap-3 pt-2">
                <span className="w-8 h-8 rounded-lg bg-muted flex items-center justify-center text-muted-foreground text-xs font-bold hover:text-primary transition-colors cursor-pointer">fb</span>
                <span className="w-8 h-8 rounded-lg bg-muted flex items-center justify-center text-muted-foreground text-xs font-bold hover:text-primary transition-colors cursor-pointer">git</span>
                <span className="w-8 h-8 rounded-lg bg-muted flex items-center justify-center text-muted-foreground text-xs font-bold hover:text-primary transition-colors cursor-pointer">ln</span>
              </div>
            </div>

            {/* Links Columns */}
            <div className="space-y-4">
              <h4 className="text-xs font-bold text-foreground uppercase tracking-wider">Platform</h4>
              <ul className="space-y-2.5 text-xs text-muted-foreground">
                <li><Link href="#jobs" className="hover:text-primary transition-colors">Browse Jobs</Link></li>
                <li><Link href="#mock-interview" className="hover:text-primary transition-colors">Mock Interviews</Link></li>
                <li><Link href="/cv-builder" className="hover:text-primary transition-colors">CV Builder</Link></li>
              </ul>
            </div>

            <div className="space-y-4">
              <h4 className="text-xs font-bold text-foreground uppercase tracking-wider">Company</h4>
              <ul className="space-y-2.5 text-xs text-muted-foreground">
                <li><Link href="/about" className="hover:text-primary transition-colors">About Us</Link></li>
                <li><Link href="/blog" className="hover:text-primary transition-colors">Blog</Link></li>
                <li><Link href="/press" className="hover:text-primary transition-colors">Press</Link></li>
              </ul>
            </div>

            <div className="space-y-4">
              <h4 className="text-xs font-bold text-foreground uppercase tracking-wider">Resources</h4>
              <ul className="space-y-2.5 text-xs text-muted-foreground">
                <li><Link href="/guides" className="hover:text-primary transition-colors">Interview Prep Guide</Link></li>
                <li><Link href="/salaries" className="hover:text-primary transition-colors">Salary Insights</Link></li>
                <li><Link href="/roadmap" className="hover:text-primary transition-colors">IT Career Roadmap</Link></li>
              </ul>
            </div>
          </div>

          <div className="pt-8 border-t border-border/60 flex flex-col sm:flex-row items-center justify-between gap-4 text-[11px] text-muted-foreground">
            <p>&copy; {new Date().getFullYear()} ITHunterView. All rights reserved.</p>
            <div className="flex items-center gap-6">
              <Link href="/privacy" className="hover:text-primary transition-colors">Privacy Policy</Link>
              <Link href="/terms" className="hover:text-primary transition-colors">Terms of Service</Link>
              <Link href="/cookies" className="hover:text-primary transition-colors">Cookie Settings</Link>
            </div>
          </div>
        </div>
      </footer>
    </div>
  )
}
