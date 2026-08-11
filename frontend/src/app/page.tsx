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
  LayoutDashboard as LayoutDashboardIcon,
  Sparkles as SparklesIcon
} from "lucide-react"
import { jobService } from "@/services/job.service"
import { JobCard } from "@/components/shared/JobCard"
import type { JobCardDto } from "@/types/job.types"
import { useTranslations } from "next-intl"

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
  const t = useTranslations("Home")
  const [searchTitle, setSearchTitle] = useState("")
  const [searchLoc, setSearchLoc] = useState("")
  const [mounted, setMounted] = useState(false)
  const [locationOpen, setLocationOpen] = useState(false)
  const [selectedIndex, setSelectedIndex] = useState(0)

  const filteredLocations = LOCATIONS.filter(loc => {
    if (!searchLoc) return true;
    return removeAccents(loc).includes(removeAccents(searchLoc));
  });

  const [featuredJobs, setFeaturedJobs] = useState<JobCardDto[]>([])
  const [loadingFeatured, setLoadingFeatured] = useState(true)

  useEffect(() => {
    setMounted(true)
    jobService.getFeaturedTopJobs(6)
      .then((res) => {
        if (res.data) setFeaturedJobs(res.data)
      })
      .catch((err) => console.error("Error fetching featured top jobs:", err))
      .finally(() => setLoadingFeatured(false))
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
          <span>{t('badge')}</span>
          <span>🇻🇳</span>
        </div>

        {/* Headline */}
        <h1 className="text-5xl sm:text-6xl md:text-7xl font-extrabold text-foreground tracking-tight max-w-4xl mx-auto leading-tight relative z-10">
          {t('heroTitle1')} <br className="hidden sm:block" />
          <span className="text-muted-foreground font-light">—</span>{" "}
          <span className="bg-gradient-to-r from-blue-600 to-cyan-400 bg-clip-text text-transparent animate-typing">{t('heroTitle2')}</span>
        </h1>

        <p className="mt-5 text-base sm:text-lg md:text-xl text-muted-foreground max-w-2xl mx-auto leading-relaxed">
          {t('heroSubtitle')}
        </p>

        {/* Search Bar */}
        <div className="mt-12 max-w-4xl mx-auto bg-white/90 backdrop-blur-md border border-border/80 shadow-lg shadow-black/5 rounded-2xl md:rounded-full p-2 flex flex-col md:flex-row gap-2 items-stretch md:items-center relative z-20 transition-all hover:shadow-xl hover:border-primary/40">
          <div className="flex-1 flex items-center gap-2.5 px-4 min-w-0 border-b md:border-b-0 md:border-r border-border/60 pb-2.5 md:pb-0 group">
            <SearchIcon className="text-muted-foreground flex-shrink-0 group-focus-within:text-primary transition-colors" size={18} />
            <Input
              type="text"
              placeholder={t('searchPlaceholder')}
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
              placeholder={t('locationPlaceholder')}
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
                    <div className="py-6 text-center text-sm text-muted-foreground">{t('noLocation')}</div>
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
            <span>{t('searchBtn')}</span>
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
              <p className="text-2xl font-bold text-foreground">{t('aiMatchTitle')}</p>
              <p className="text-sm text-muted-foreground mt-1">{t('aiMatchDesc')}</p>
            </div>
          </div>

          {/* Card 2 */}
          <div className="glass-panel p-5 rounded-2xl flex flex-col gap-3 hover:-translate-y-1 transition-transform duration-300 group cursor-default">
            <div className="w-10 h-10 rounded-xl bg-emerald-500/10 text-emerald-600 flex items-center justify-center group-hover:scale-110 transition-transform">
              <TargetIcon size={20} />
            </div>
            <div>
              <p className="text-2xl font-bold text-foreground">{t('mocksTitle')}</p>
              <p className="text-sm text-muted-foreground mt-1">{t('mocksDesc')}</p>
            </div>
          </div>

          {/* Card 3 */}
          <div className="glass-panel p-5 rounded-2xl flex flex-col gap-3 hover:-translate-y-1 transition-transform duration-300 group cursor-default">
            <div className="w-10 h-10 rounded-xl bg-purple-500/10 text-purple-600 flex items-center justify-center group-hover:scale-110 transition-transform">
              <CheckIcon size={20} />
            </div>
            <div>
              <p className="text-2xl font-bold text-foreground flex items-center gap-2">
                {t('jobsTitle')}
                <span className="flex h-2.5 w-2.5 relative">
                  <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-purple-400 opacity-75"></span>
                  <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-purple-500"></span>
                </span>
              </p>
              <p className="text-sm text-muted-foreground mt-1">{t('jobsDesc')}</p>
            </div>
          </div>
        </div>
      </section>

      <hr className="border-border w-full" />

      {/* Featured Top Jobs Section (Dưới Hero Section) */}
      <section className="py-16 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 w-full relative z-10">
        <div className="flex flex-col md:flex-row md:items-end justify-between mb-10 gap-4">
          <div className="text-left">
            <div className="inline-flex items-center gap-1.5 px-3.5 py-1 rounded-full text-xs font-extrabold bg-gradient-to-r from-amber-500/20 via-orange-500/20 to-rose-500/20 text-orange-600 dark:text-orange-400 border border-orange-500/30 shadow-sm mb-3 uppercase tracking-wide">
              <SparklesIcon className="h-3.5 w-3.5 fill-orange-500" />
              {t('featuredTopJobs')}
            </div>
            <h2 className="text-3xl sm:text-4xl md:text-5xl font-black text-foreground tracking-tight flex items-center gap-3">
              {t('featuredTitle')}
              <span className="inline-flex h-3 w-3 relative">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-orange-400 opacity-75"></span>
                <span className="relative inline-flex rounded-full h-3 w-3 bg-gradient-to-tr from-amber-500 to-orange-600"></span>
              </span>
            </h2>
            <p className="text-muted-foreground mt-2 text-sm sm:text-base md:text-lg max-w-2xl">
              {t('featuredDesc')}
            </p>
          </div>
          <Link
            href="/jobs"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-full bg-zinc-100 dark:bg-zinc-800/80 hover:bg-amber-500 hover:text-white dark:hover:bg-amber-600 text-sm font-bold text-zinc-800 dark:text-zinc-200 transition-all shadow-sm shrink-0 group"
          >
            <span>{t('exploreAllJobs')}</span>
            <ArrowRightIcon size={16} className="group-hover:translate-x-1.5 transition-transform" />
          </Link>
        </div>

        {loadingFeatured ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {[1, 2, 3].map((item) => (
              <div key={item} className="h-64 rounded-2xl bg-zinc-100 dark:bg-zinc-800/50 animate-pulse border border-zinc-200/50 dark:border-zinc-700/50" />
            ))}
          </div>
        ) : featuredJobs.length === 0 ? (
          <div className="glass-panel text-center py-16 px-6 rounded-2xl border border-dashed border-zinc-300 dark:border-zinc-700 shadow-inner">
            <SparklesIcon className="h-10 w-10 text-zinc-400 mx-auto mb-3 opacity-50" />
            <p className="text-muted-foreground text-base font-medium">{t('noFeaturedJobs')}</p>
            <p className="text-xs text-zinc-400 mt-1">{t('beTheFirst')}</p>
            <Link href="/jobs" className="mt-5 inline-block text-sm text-primary font-bold px-6 py-2 bg-primary/10 rounded-full hover:bg-primary hover:text-white transition-colors">{t('exploreAllJobs')}</Link>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 items-stretch">
            {featuredJobs.map((job) => (
              <div key={job.id} className="transition-all duration-300 hover:-translate-y-2 h-full">
                <JobCard job={{ ...job, isPushedTop: true }} />
              </div>
            ))}
          </div>
        )}
      </section>

      <hr className="border-border w-full" />

      {/* How It Works Section */}
      <section id="mock-interview" className="py-20 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 w-full">
        <div className="text-center mb-16">
          <span className="text-primary text-xs font-semibold uppercase tracking-wider bg-primary/10 px-3 py-1 rounded-full">
            {t('howItWorksLabel')}
          </span>
          <h2 className="text-3xl sm:text-4xl font-extrabold text-foreground mt-4">{t('howItWorksTitle')}</h2>
          <p className="text-muted-foreground mt-3 text-sm sm:text-base max-w-md mx-auto">
            {t('howItWorksSubtitle')}
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
            <p className="text-xs uppercase font-bold text-primary tracking-wider mb-2">{t('step1Label')}</p>
            <h3 className="text-lg font-bold text-foreground mb-2">{t('step1Title')}</h3>
            <p className="text-sm text-muted-foreground leading-relaxed max-w-xs">
              {t('step1Desc')}
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
            <p className="text-xs uppercase font-bold text-primary tracking-wider mb-2">{t('step2Label')}</p>
            <h3 className="text-lg font-bold text-foreground mb-2">{t('step2Title')}</h3>
            <p className="text-sm text-muted-foreground leading-relaxed max-w-xs">
              {t('step2Desc')}
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
            <p className="text-xs uppercase font-bold text-primary tracking-wider mb-2">{t('step3Label')}</p>
            <h3 className="text-lg font-bold text-foreground mb-2">{t('step3Title')}</h3>
            <p className="text-sm text-muted-foreground leading-relaxed max-w-xs">
              {t('step3Desc')}
            </p>
          </div>
        </div>
      </section>

      {/* Removed Pricing Section. Now at /pricing */}

      {/* Footer */}
      <footer className="bg-card border-t border-border mt-auto pt-16 pb-8">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 md:grid-cols-5 gap-8 mb-12">
            {/* Logo and Description */}
            <div className="md:col-span-2 space-y-4">
              <Logo size="md" href="/" />
              <p className="text-xs text-muted-foreground max-w-sm leading-relaxed">
                {t('footerDesc')}
              </p>
            </div>

            {/* Links Columns */}
            <div className="space-y-4">
              <h4 className="text-xs font-bold text-foreground uppercase tracking-wider">{t('platform')}</h4>
              <ul className="space-y-2.5 text-xs text-muted-foreground">
                <li><Link href="/#" className="hover:text-primary transition-colors">{t('browseJobs')}</Link></li>
                <li><Link href="/#" className="hover:text-primary transition-colors">{t('mockInterviews')}</Link></li>
                <li><Link href="/#" className="hover:text-primary transition-colors">{t('cvBuilder')}</Link></li>
              </ul>
            </div>

            <div className="space-y-4">
              <h4 className="text-xs font-bold text-foreground uppercase tracking-wider">{t('company')}</h4>
              <ul className="space-y-2.5 text-xs text-muted-foreground">
                <li><Link href="/#" className="hover:text-primary transition-colors">{t('aboutUs')}</Link></li>
                <li><Link href="/#" className="hover:text-primary transition-colors">{t('blog')}</Link></li>
                <li><Link href="/#" className="hover:text-primary transition-colors">{t('press')}</Link></li>
              </ul>
            </div>

            <div className="space-y-4">
              <h4 className="text-xs font-bold text-foreground uppercase tracking-wider">{t('resources')}</h4>
              <ul className="space-y-2.5 text-xs text-muted-foreground">
                <li><Link href="/#" className="hover:text-primary transition-colors">{t('interviewPrep')}</Link></li>
                <li><Link href="/#" className="hover:text-primary transition-colors">{t('salaryInsights')}</Link></li>
                <li><Link href="/#" className="hover:text-primary transition-colors">{t('careerRoadmap')}</Link></li>
              </ul>
            </div>
          </div>

          <div className="pt-8 border-t border-border/60 flex flex-col sm:flex-row items-center justify-between gap-4 text-[11px] text-muted-foreground">
            <p>&copy; {new Date().getFullYear()} ITHunterView. {t('allRightsReserved')}</p>
            <div className="flex items-center gap-6">
              <Link href="/#" className="hover:text-primary transition-colors">{t('privacyPolicy')}</Link>
              <Link href="/#" className="hover:text-primary transition-colors">{t('termsOfService')}</Link>
              <Link href="/#" className="hover:text-primary transition-colors">{t('cookieSettings')}</Link>
            </div>
          </div>
        </div>
      </footer>


    </div>
  )
}
