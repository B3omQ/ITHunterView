"use client"

import { useState, useEffect } from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import { Logo } from "@/components/layout/Logo"
import { useAuthStore } from "@/store/auth.store"
import { getDashboardPath } from "@/lib/constants"
import { PublicHeader } from "@/components/layout/PublicHeader"
import { Command, CommandEmpty, CommandGroup, CommandItem, CommandList } from "@/components/ui/command"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import {
  Search as SearchIcon,
  MapPin as MapPinIcon,
  ArrowRight as ArrowRightIcon,
  Upload as UploadIcon,
  Zap as ZapIcon,
  Target as TargetIcon,
  ChevronLeft as ChevronLeftIcon,
  ChevronRight as ChevronRightIcon,
  ChevronDown as ChevronDownIcon,
  Check as CheckIcon,
  LogOut as LogOutIcon,
  LayoutDashboard as LayoutDashboardIcon
} from "lucide-react"

const LOCATIONS = [
  "Hồ Chí Minh", "Hà Nội", "Đà Nẵng", "Cần Thơ", "Hải Phòng",
  "An Giang", "Bà Rịa - Vũng Tàu", "Bắc Giang", "Bắc Kạn", "Bạc Liêu", "Bắc Ninh", "Bến Tre", "Bình Định", "Bình Dương", "Bình Phước", "Bình Thuận", "Cà Mau", "Cao Bằng", "Đắk Lắk", "Đắk Nông", "Điện Biên", "Đồng Nai", "Đồng Tháp", "Gia Lai", "Hà Giang", "Hà Nam", "Hà Tĩnh", "Hải Dương", "Hậu Giang", "Hòa Bình", "Hưng Yên", "Khánh Hòa", "Kiên Giang", "Kon Tum", "Lai Châu", "Lâm Đồng", "Lạng Sơn", "Lào Cai", "Long An", "Nam Định", "Nghệ An", "Ninh Bình", "Ninh Thuận", "Phú Thọ", "Phú Yên", "Quảng Bình", "Quảng Nam", "Quảng Ngãi", "Quảng Ninh", "Quảng Trị", "Sóc Trăng", "Sơn La", "Tây Ninh", "Thái Bình", "Thái Nguyên", "Thanh Hóa", "Thừa Thiên Huế", "Tiền Giang", "Trà Vinh", "Tuyên Quang", "Vĩnh Long", "Vĩnh Phúc", "Yên Bái",
  "International", "Others"
];

const removeAccents = (str: string) => {
  return str.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
};

// Icons are imported from lucide-react

export default function Home() {
  const router = useRouter()
  const { user, logout } = useAuthStore()
  const [searchTitle, setSearchTitle] = useState("")
  const [searchLoc, setSearchLoc] = useState("")
  const [mounted, setMounted] = useState(false)
  const [locationOpen, setLocationOpen] = useState(false)
  const [selectedIndex, setSelectedIndex] = useState(0)

  const filteredLocations = LOCATIONS.filter(loc => {
    if (!searchLoc) return true;
    return removeAccents(loc).includes(removeAccents(searchLoc));
  });

  useEffect(() => {
    setMounted(true)
  }, [])

  const handleSearch = () => {
    const params = new URLSearchParams()
    if (searchTitle.trim()) params.append("query", searchTitle.trim())
    if (searchLoc.trim()) params.append("location", searchLoc.trim())

    const queryString = params.toString()
    router.push(queryString ? `/jobs?${queryString}` : '/jobs')
  }





  return (
    <div className="min-h-screen flex flex-col bg-background bg-generative-grid text-foreground relative">
      {/* Header */}
      <PublicHeader />

      {/* Hero Section */}
      <section className="relative pt-20 pb-16 md:pt-28 md:pb-24 px-4 sm:px-6 lg:px-8 max-w-7xl mx-auto w-full text-center">
        {/* Decorative background blobs */}
        <div className="absolute top-20 left-10 w-72 h-72 bg-blue-400 rounded-full mix-blend-multiply filter blur-3xl opacity-20 animate-blob hidden md:block"></div>
        <div className="absolute top-20 right-10 w-72 h-72 bg-cyan-400 rounded-full mix-blend-multiply filter blur-3xl opacity-20 animate-blob animation-delay-2000 hidden md:block"></div>
        <div className="absolute -bottom-8 left-1/2 -translate-x-1/2 w-72 h-72 bg-indigo-400 rounded-full mix-blend-multiply filter blur-3xl opacity-20 animate-blob animation-delay-4000 hidden md:block"></div>

        {/* Badge */}
        <div className="inline-flex items-center gap-2 glass-panel text-foreground text-xs font-semibold px-4 py-2 rounded-full mb-8 shadow-sm hover:scale-105 transition-transform cursor-default relative z-10">
          <span className="w-2 h-2 rounded-full bg-green-500 animate-pulse"></span>
          <span>#1 IT Job Platform for Fresh IT Graduates in Vietnam</span>
          <span>🇻🇳</span>
        </div>

        {/* Headline */}
        <h1 className="text-5xl sm:text-6xl md:text-7xl font-extrabold text-foreground tracking-tight max-w-4xl mx-auto leading-tight relative z-10">
          Land your first IT job <br className="hidden sm:block" />
          <span className="text-muted-foreground font-light">—</span>{" "}
          <span className="bg-gradient-to-r from-blue-600 to-cyan-400 bg-clip-text text-transparent animate-typing">faster.</span>
        </h1>

        <p className="mt-5 text-base sm:text-lg md:text-xl text-muted-foreground max-w-2xl mx-auto leading-relaxed">
          Smart job matching, AI-powered CV optimization, and real mock interviews to get you hired at top tech companies.
        </p>

        {/* Search Bar */}
        <div className="mt-12 max-w-4xl mx-auto bg-white/90 backdrop-blur-md border border-border/80 shadow-lg shadow-black/5 rounded-2xl md:rounded-full p-2 flex flex-col md:flex-row gap-2 items-stretch md:items-center relative z-20 transition-all hover:shadow-xl hover:border-primary/40">
          <div className="flex-1 flex items-center gap-2.5 px-4 min-w-0 border-b md:border-b-0 md:border-r border-border/60 pb-2.5 md:pb-0 group">
            <SearchIcon className="text-muted-foreground flex-shrink-0 group-focus-within:text-primary transition-colors" size={18} />
            <Input
              type="text"
              placeholder="Job title, keywords, company..."
              value={searchTitle}
              onChange={(e) => setSearchTitle(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
              className="w-full border-0 bg-transparent shadow-none focus-visible:ring-0 p-1 md:text-sm placeholder:text-muted-foreground"
            />
          </div>

          <div className="flex-1 flex items-center gap-2.5 px-4 min-w-0 pb-2.5 md:pb-0 relative">
            <MapPinIcon className="text-muted-foreground flex-shrink-0" size={18} />
            <Input
              type="text"
              placeholder="Location (Hanoi, Ho Chi Minh...)"
              value={searchLoc}
              onChange={(e) => {
                setSearchLoc(e.target.value);
                setLocationOpen(true);
                setSelectedIndex(0);
              }}
              onFocus={() => setLocationOpen(true)}
              onBlur={() => {
                setTimeout(() => setLocationOpen(false), 200);
              }}
              onKeyDown={(e) => {
                if (locationOpen) {
                  if (e.key === 'ArrowDown') {
                    e.preventDefault();
                    setSelectedIndex((prev) => Math.min(prev + 1, filteredLocations.length - 1));
                  } else if (e.key === 'ArrowUp') {
                    e.preventDefault();
                    setSelectedIndex((prev) => Math.max(prev - 1, 0));
                  } else if (e.key === 'Enter') {
                    e.preventDefault();
                    if (filteredLocations[selectedIndex]) {
                      setSearchLoc(filteredLocations[selectedIndex]);
                      setLocationOpen(false);
                    } else {
                      handleSearch();
                    }
                  } else if (e.key === 'Escape') {
                    setLocationOpen(false);
                  }
                } else if (e.key === 'Enter') {
                  handleSearch();
                }
              }}
              className="w-full border-0 bg-transparent shadow-none focus-visible:ring-0 p-1 md:text-sm placeholder:text-muted-foreground"
            />
            {locationOpen && (
              <div className="absolute top-full left-0 right-0 mt-4 z-50">
                <div className="border border-border shadow-lg rounded-xl overflow-hidden bg-popover text-popover-foreground flex flex-col max-h-[300px]">
                  {filteredLocations.length === 0 ? (
                    <div className="py-6 text-center text-sm text-muted-foreground">No location found.</div>
                  ) : (
                    <div className="p-1.5 overflow-y-auto">
                      {filteredLocations.map((loc, idx) => (
                        <div
                          key={loc}
                          onMouseDown={(e) => {
                            e.preventDefault();
                            setSearchLoc(loc);
                            setLocationOpen(false);
                          }}
                          className={`rounded-md cursor-pointer px-3 py-2 text-sm flex items-center ${selectedIndex === idx ? 'bg-accent text-accent-foreground' : 'hover:bg-accent hover:text-accent-foreground'}`}
                        >
                          {loc}
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            )}
          </div>

          <Button
            onClick={handleSearch}
            className="h-10 px-8 rounded-xl md:rounded-full font-semibold flex items-center justify-center gap-2 shrink-0 shadow-md hover:shadow-lg transition-all"
          >
            <SearchIcon size={16} />
            <span>Search</span>
          </Button>
        </div>



        {/* Bento Grid Highlights */}
        <div className="mt-16 max-w-4xl mx-auto grid grid-cols-1 sm:grid-cols-3 gap-5 relative z-10 text-left">
          {/* Card 1 */}
          <div className="glass-panel p-5 rounded-2xl flex flex-col gap-3 hover:-translate-y-1 transition-transform duration-300 group cursor-default">
            <div className="w-10 h-10 rounded-xl bg-blue-500/10 text-blue-600 flex items-center justify-center group-hover:scale-110 transition-transform">
              <ZapIcon size={20} />
            </div>
            <div>
              <p className="text-2xl font-bold text-foreground">AI Match</p>
              <p className="text-sm text-muted-foreground mt-1">Smart ranking & fit scoring for every job</p>
            </div>
          </div>
          
          {/* Card 2 */}
          <div className="glass-panel p-5 rounded-2xl flex flex-col gap-3 hover:-translate-y-1 transition-transform duration-300 group cursor-default">
            <div className="w-10 h-10 rounded-xl bg-emerald-500/10 text-emerald-600 flex items-center justify-center group-hover:scale-110 transition-transform">
              <TargetIcon size={20} />
            </div>
            <div>
              <p className="text-2xl font-bold text-foreground">100+ Mocks</p>
              <p className="text-sm text-muted-foreground mt-1">Real-world technical interview prep</p>
            </div>
          </div>
          
          {/* Card 3 */}
          <div className="glass-panel p-5 rounded-2xl flex flex-col gap-3 hover:-translate-y-1 transition-transform duration-300 group cursor-default">
            <div className="w-10 h-10 rounded-xl bg-purple-500/10 text-purple-600 flex items-center justify-center group-hover:scale-110 transition-transform">
              <CheckIcon size={20} />
            </div>
            <div>
              <p className="text-2xl font-bold text-foreground flex items-center gap-2">
                10k+ Jobs
                <span className="flex h-2.5 w-2.5 relative">
                  <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-purple-400 opacity-75"></span>
                  <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-purple-500"></span>
                </span>
              </p>
              <p className="text-sm text-muted-foreground mt-1">From 500+ top tech companies</p>
            </div>
          </div>
        </div>
      </section>


      <hr className="border-border w-full" />

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

      {/* Floating AI Copilot */}
      <div className="fixed bottom-6 right-6 z-50 flex items-center gap-3">
        <div className="glass-panel hidden sm:flex items-center px-4 py-2 rounded-full text-xs font-semibold text-foreground shadow-lg cursor-pointer hover:bg-white/80 transition-colors">
          Need help? Ask our AI
        </div>
        <button className="w-14 h-14 bg-foreground text-background rounded-full shadow-2xl flex items-center justify-center hover:scale-110 transition-transform hover:shadow-primary/20 relative group">
          <span className="absolute -top-1 -right-1 flex h-3 w-3">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-primary opacity-75"></span>
            <span className="relative inline-flex rounded-full h-3 w-3 bg-primary border-2 border-background"></span>
          </span>
          <ZapIcon size={24} className="group-hover:animate-bounce" />
        </button>
      </div>
    </div>
  )
}
