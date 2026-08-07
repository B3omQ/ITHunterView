import React from 'react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Loader2 } from 'lucide-react';
import { useTranslations } from "next-intl";

interface JdSelectionPanelProps {
  jdTab: string;
  setJdTab: (val: string) => void;
  jdText: string;
  setJdText: (val: string) => void;
  selectedJobId: string;
  setSelectedJobId: (val: string) => void;
  savedJobs: any[];
  isLoadingJobs: boolean;
}

export function JdSelectionPanel({
  jdTab,
  setJdTab,
  jdText,
  setJdText,
  selectedJobId,
  setSelectedJobId,
  savedJobs,
  isLoadingJobs,
}: JdSelectionPanelProps) {
  const t = useTranslations("CandidateCVMatching");

  return (
    <div className="flex flex-col space-y-3">
      <Label className="text-base font-semibold">{t("jdSelectionTitle")}</Label>
      <Tabs value={jdTab} onValueChange={setJdTab} className="w-full">
        <TabsList className="grid w-full grid-cols-2">
          <TabsTrigger value="paste">{t("jdSelectionPasteTab")}</TabsTrigger>
          <TabsTrigger value="saved">{t("jdSelectionSavedTab")}</TabsTrigger>
        </TabsList>

        {/* Tab: Paste JD Text */}
        <TabsContent value="paste" className="mt-4">
          <Textarea
            placeholder={t("jdSelectionPastePlaceholder")}
            className="min-h-[220px] font-sans resize-none"
            value={jdText}
            onChange={(e) => setJdText(e.target.value)}
          />
        </TabsContent>

        {/* Tab: Saved Jobs */}
        <TabsContent value="saved" className="mt-4">
          <div className="space-y-4">
            <Label className="text-xs text-muted-foreground">{t("jdSelectionSavedLabel")}</Label>
            {isLoadingJobs ? (
              <div className="flex items-center justify-center p-8 border rounded-lg bg-muted/10">
                <Loader2 className="h-6 w-6 animate-spin text-primary" />
              </div>
            ) : savedJobs.length === 0 ? (
              <div className="text-center p-8 border border-dashed rounded-lg">
                <p className="text-sm text-muted-foreground">{t("jdSelectionNoSaved")}</p>
              </div>
            ) : (
              <Select value={selectedJobId} onValueChange={(val) => setSelectedJobId(val || '')}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder={t("jdSelectionSelectPlaceholder")}>
                    {selectedJobId 
                      ? (() => {
                          const matched = savedJobs.find(j => j.jobId === selectedJobId);
                          return matched ? `${matched.title} - ${matched.companyName}` : undefined;
                        })()
                      : undefined}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  {savedJobs.map((job) => (
                      <SelectItem key={job.jobId} value={job.jobId}>
                        {job.title} - {job.companyName}
                      </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
}
