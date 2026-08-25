"use client"

import { useQuery, useQueryClient } from "@tanstack/react-query"
import { subscriptionService } from "@/services/subscription.service"
import { walletService } from "@/services/wallet.service"
import { useSignalR } from "@/hooks/useSignalR"
import { PublicHeader } from "@/components/layout/PublicHeader"
import { Zap as ZapIcon, Check as CheckIcon } from "lucide-react"
import { useEffect } from "react"
import Link from "next/link"
import { useTranslations } from "next-intl"

export default function PricingPage() {
  const queryClient = useQueryClient()
  const t = useTranslations("Pricing")

  const { data: subsData, isLoading: isSubsLoading } = useQuery({
    queryKey: ['public-subscriptions'],
    queryFn: () => subscriptionService.getPublicSubscriptions()
  })

  const { data: coinsData, isLoading: isCoinsLoading } = useQuery({
    queryKey: ['public-coin-packages'],
    queryFn: () => walletService.getActiveCoinPackages()
  })

  const connection = useSignalR('/hubs/notification')

  useEffect(() => {
    if (connection) {
      connection.on('ReceivePricingUpdate', () => {
        queryClient.invalidateQueries({ queryKey: ['public-subscriptions'] })
        queryClient.invalidateQueries({ queryKey: ['public-coin-packages'] })
      })
      return () => {
        connection.off('ReceivePricingUpdate')
      }
    }
  }, [connection, queryClient])

  return (
    <div className="min-h-screen flex flex-col bg-background bg-generative-grid text-foreground relative">
      <PublicHeader />
      
      <main className="flex-grow py-20">
        <section id="pricing" className="bg-muted/30">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div className="text-center mb-16">
              <span className="text-primary text-xs font-semibold uppercase tracking-wider bg-primary/10 px-3 py-1 rounded-full">
                {t('badge')}
              </span>
              <h1 className="text-3xl sm:text-5xl font-extrabold text-foreground mt-4">{t('title')}</h1>
              <p className="text-muted-foreground mt-3 text-sm sm:text-base max-w-md mx-auto">
                {t('subtitle')}
              </p>
            </div>

            {/* Pricing Grid */}
            <div className="flex flex-col gap-12 max-w-6xl mx-auto">
              {isSubsLoading || isCoinsLoading ? (
                <div className="flex justify-center items-center py-20 text-muted-foreground">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mr-3"></div>
                  {t('loading')}
                </div>
              ) : (
                <>
                  {subsData?.data && subsData.data.length > 0 && (
                    <div className="flex flex-col gap-12">
                      {(() => {
                        const candidateSubs = subsData.data.filter((s: any) => s.featuresConfig?.role?.toUpperCase() !== 'RECRUITER');
                        const recruiterSubs = subsData.data.filter((s: any) => s.featuresConfig?.role?.toUpperCase() === 'RECRUITER');

                        return (
                          <>
                            {candidateSubs.length > 0 && (
                              <div>
                                <h3 className="text-xl font-bold mb-6 text-center">{t('forCandidates')}</h3>
                                <div className="grid grid-cols-1 md:grid-cols-3 gap-8 items-stretch">
                                  {candidateSubs.map((sub: any, index: number) => {
                                    const isPopular = index === 1;
                                    return (
                                      <div key={sub.id} className={`border rounded-3xl p-8 flex flex-col justify-between relative ${isPopular ? 'bg-primary text-primary-foreground border-primary scale-105 z-10 shadow-md' : 'bg-card border-border shadow-sm'}`}>
                                        {isPopular && (
                                          <span className="absolute -top-3 left-1/2 -translate-x-1/2 bg-white text-primary text-[10px] uppercase font-extrabold px-3 py-1 rounded-full border border-primary/20 shadow-sm">
                                            {t('mostPopular')}
                                          </span>
                                        )}
                                        <div>
                                          <p className={`font-bold text-lg mb-2 ${isPopular ? 'text-white' : 'text-foreground'}`}>{sub.name}</p>
                                          <div className="flex items-baseline mb-4">
                                            <span className={`text-3xl sm:text-4xl font-extrabold ${isPopular ? 'text-white' : 'text-foreground'}`}>
                                              {sub.price.toLocaleString()}đ
                                            </span>
                                            <span className={`text-xs font-medium ml-1 ${isPopular ? 'text-primary-foreground/80' : 'text-muted-foreground'}`}>
                                              /{sub.durationDays} {t('days')}
                                            </span>
                                          </div>
                                          <p className={`text-xs mb-8 ${isPopular ? 'text-primary-foreground/80' : 'text-muted-foreground'}`}>
                                            {sub.price === 0 ? t('candidateFreeDesc') : t('candidatePaidDesc')}
                                          </p>

                                          <ul className="space-y-3.5">
                                            {sub.featuresConfig && (
                                              <>
                                                <li className={`flex items-start gap-2.5 text-xs ${isPopular ? 'text-primary-foreground/95' : 'text-muted-foreground'}`}>
                                                  <CheckIcon className={`${isPopular ? 'text-white' : 'text-primary'} mt-0.5 flex-shrink-0`} size={14} />
                                                  <span>{sub.featuresConfig.cvMatchLimit ? `${sub.featuresConfig.cvMatchLimit} ${t('cvMatches')}` : t('unlimitedCvMatches')}</span>
                                                </li>
                                                <li className={`flex items-start gap-2.5 text-xs ${isPopular ? 'text-primary-foreground/95' : 'text-muted-foreground'}`}>
                                                  <CheckIcon className={`${isPopular ? 'text-white' : 'text-primary'} mt-0.5 flex-shrink-0`} size={14} />
                                                  <span>{sub.featuresConfig.mockInterviewLimit ? `${sub.featuresConfig.mockInterviewLimit} ${t('mockInterviews')}` : t('unlimitedMockInterviews')}</span>
                                                </li>
                                                <li className={`flex items-start gap-2.5 text-xs ${isPopular ? 'text-primary-foreground/95' : 'text-muted-foreground'}`}>
                                                  <CheckIcon className={`${isPopular ? 'text-white' : 'text-primary'} mt-0.5 flex-shrink-0`} size={14} />
                                                  <span>{sub.featuresConfig.learningPathSlotLimit ? `${sub.featuresConfig.learningPathSlotLimit} ${t('learningPaths')}` : t('unlimitedLearningPaths')}</span>
                                                </li>
                                              </>
                                            )}
                                          </ul>
                                        </div>

                                        <button className={`mt-8 w-full h-11 rounded-xl font-semibold text-sm transition-all cursor-pointer ${isPopular ? 'bg-white hover:bg-white/95 text-primary shadow-sm' : 'border border-primary text-primary hover:bg-primary/5'}`}>
                                          {sub.price === 0 ? t('startFree') : t('upgrade')}
                                        </button>
                                      </div>
                                    );
                                  })}
                                </div>
                              </div>
                            )}

                            {recruiterSubs.length > 0 && (
                              <div className="mt-8">
                                <h3 className="text-xl font-bold mb-6 text-center">{t('forRecruiters')}</h3>
                                <div className="grid grid-cols-1 md:grid-cols-3 gap-8 items-stretch">
                                  {recruiterSubs.map((sub: any, index: number) => {
                                    const isPopular = index === 1;
                                    return (
                                      <div key={sub.id} className={`border rounded-3xl p-8 flex flex-col justify-between relative ${isPopular ? 'bg-indigo-600 text-white border-indigo-600 scale-105 z-10 shadow-md' : 'bg-card border-border shadow-sm'}`}>
                                        {isPopular && (
                                          <span className="absolute -top-3 left-1/2 -translate-x-1/2 bg-white text-indigo-600 text-[10px] uppercase font-extrabold px-3 py-1 rounded-full border border-indigo-600/20 shadow-sm">
                                            {t('mostPopular')}
                                          </span>
                                        )}
                                        <div>
                                          <p className={`font-bold text-lg mb-2 ${isPopular ? 'text-white' : 'text-foreground'}`}>{sub.name}</p>
                                          <div className="flex items-baseline mb-4">
                                            <span className={`text-3xl sm:text-4xl font-extrabold ${isPopular ? 'text-white' : 'text-foreground'}`}>
                                              {sub.price.toLocaleString()}đ
                                            </span>
                                            <span className={`text-xs font-medium ml-1 ${isPopular ? 'text-white/80' : 'text-muted-foreground'}`}>
                                              /{sub.durationDays} {t('days')}
                                            </span>
                                          </div>
                                          <p className={`text-xs mb-8 ${isPopular ? 'text-white/80' : 'text-muted-foreground'}`}>
                                            {sub.price === 0 ? t('recruiterFreeDesc') : t('recruiterPaidDesc')}
                                          </p>

                                          <ul className="space-y-3.5">
                                            {sub.featuresConfig && (
                                              <>
                                                <li className={`flex items-start gap-2.5 text-xs ${isPopular ? 'text-white/95' : 'text-muted-foreground'}`}>
                                                  <CheckIcon className={`${isPopular ? 'text-white' : 'text-indigo-600'} mt-0.5 flex-shrink-0`} size={14} />
                                                  <span>{sub.featuresConfig.jobSlots ? `${sub.featuresConfig.jobSlots} ${t('jobSlots')}` : t('unlimitedJobSlots')}</span>
                                                </li>
                                                <li className={`flex items-start gap-2.5 text-xs ${isPopular ? 'text-white/95' : 'text-muted-foreground'}`}>
                                                  <CheckIcon className={`${isPopular ? 'text-white' : 'text-indigo-600'} mt-0.5 flex-shrink-0`} size={14} />
                                                  <span>
                                                    {sub.featuresConfig.unlockCvLimit === 0 
                                                      ? t('noCvUnlocks') 
                                                      : sub.featuresConfig.unlockCvLimit != null && sub.featuresConfig.unlockCvLimit > 0 
                                                        ? `${sub.featuresConfig.unlockCvLimit} ${t('cvUnlocks')}` 
                                                        : t('unlimitedCvUnlocks')}
                                                  </span>
                                                </li>
                                                <li className={`flex items-start gap-2.5 text-xs ${isPopular ? 'text-white/95' : 'text-muted-foreground'}`}>
                                                  <CheckIcon className={`${isPopular ? 'text-white' : 'text-indigo-600'} mt-0.5 flex-shrink-0`} size={14} />
                                                  <span>{sub.featuresConfig.pushTopLimit ? `${sub.featuresConfig.pushTopLimit} ${t('pushTopUses')}` : t('noPushTopUses')}</span>
                                                </li>
                                              </>
                                            )}
                                          </ul>
                                        </div>

                                        <button className={`mt-8 w-full h-11 rounded-xl font-semibold text-sm transition-all cursor-pointer ${isPopular ? 'bg-white hover:bg-white/95 text-indigo-600 shadow-sm' : 'border border-indigo-600 text-indigo-600 hover:bg-indigo-600/5'}`}>
                                          {sub.price === 0 ? t('startFree') : t('upgrade')}
                                        </button>
                                      </div>
                                    );
                                  })}
                                </div>
                              </div>
                            )}
                          </>
                        );
                      })()}
                    </div>
                  )}

                  {coinsData?.data && coinsData.data.length > 0 && (
                    <div className="mt-8">
                      <h3 className="text-xl font-bold mb-6 text-center">{t('coinPackages')}</h3>
                      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-6 items-stretch">
                        {coinsData.data.map((pkg: any) => (
                          <div key={pkg.id} className="bg-card border border-border rounded-2xl p-6 flex flex-col justify-between shadow-sm hover:shadow-md transition-shadow">
                            <div>
                              <p className="font-bold text-foreground text-md mb-2">{pkg.name}</p>
                              <div className="flex items-center gap-2 mb-4">
                                <span className="text-2xl font-extrabold text-foreground">{pkg.price.toLocaleString()}đ</span>
                              </div>
                              <div className="flex items-center gap-1.5 text-amber-500 font-bold mb-6">
                                <ZapIcon size={16} className="fill-amber-500" />
                                <span>{pkg.coins.toLocaleString()} {t('coins')}</span>
                              </div>
                            </div>
                            <button className="w-full h-10 rounded-lg border border-border bg-muted/50 hover:bg-muted text-foreground font-semibold text-sm transition-all cursor-pointer">
                              {t('buyPackage')}
                            </button>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </>
              )}
            </div>
          </div>
        </section>
      </main>

      {/* Footer */}
      <footer className="bg-card border-t border-border mt-auto pt-16 pb-8">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center text-sm text-muted-foreground">
          &copy; {new Date().getFullYear()} {t('footerText')}
        </div>
      </footer>
    </div>
  )
}
