import React from 'react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Button } from '@/components/ui/button';
import { UploadCloud, FileText, Trash2, Loader2, Check } from 'lucide-react';
import { useTranslations } from "next-intl";

interface CvSelectionPanelProps {
  cvTab: string;
  setCvTab: (val: string) => void;
  cvFile: File | null;
  cvFileName: string;
  cvText: string;
  setCvText: (val: string) => void;
  selectedCvId: string;
  setSelectedCvId: (val: string) => void;
  isUploading: boolean;
  myCvs: any[];
  isLoadingCvs: boolean;
  handleFileChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  handleDragOver: (e: React.DragEvent) => void;
  handleDrop: (e: React.DragEvent) => void;
  handleRemoveFile: () => void;
}

export function CvSelectionPanel({
  cvTab,
  setCvTab,
  cvFile,
  cvFileName,
  cvText,
  setCvText,
  selectedCvId,
  setSelectedCvId,
  isUploading,
  myCvs,
  isLoadingCvs,
  handleFileChange,
  handleDragOver,
  handleDrop,
  handleRemoveFile,
}: CvSelectionPanelProps) {
  const t = useTranslations("CandidateCVMatching");

  return (
    <div className="flex flex-col space-y-3">
      <Label className="text-base font-semibold">{t("cvSelectionTitle")}</Label>
      <Tabs value={cvTab} onValueChange={setCvTab} className="w-full">
        <TabsList className="grid w-full grid-cols-3">
          <TabsTrigger value="upload">{t("cvSelectionUploadTab")}</TabsTrigger>
          <TabsTrigger value="paste">{t("cvSelectionPasteTab")}</TabsTrigger>
          <TabsTrigger value="saved">{t("cvSelectionSavedTab")}</TabsTrigger>
        </TabsList>

        {/* Tab: Upload File */}
        <TabsContent value="upload" className="mt-4">
          {!cvFile ? (
            <div 
              onDragOver={handleDragOver}
              onDrop={handleDrop}
              className="border-2 border-dashed border-input rounded-lg p-8 text-center hover:bg-muted/40 transition cursor-pointer flex flex-col items-center justify-center min-h-[220px]"
            >
              <input 
                type="file" 
                id="cv-upload-input" 
                className="hidden" 
                accept=".pdf,.docx,.txt"
                onChange={handleFileChange}
              />
              <label htmlFor="cv-upload-input" className="cursor-pointer flex flex-col items-center justify-center w-full h-full">
                <UploadCloud className="h-10 w-10 text-muted-foreground mb-4" />
                <span className="text-sm font-medium" dangerouslySetInnerHTML={{ __html: t.raw("cvSelectionDragDrop") }}></span>
                <span className="text-xs text-muted-foreground mt-2">{t("cvSelectionFileSupport")}</span>
              </label>
            </div>
          ) : (
            <div className="border border-input rounded-lg p-6 flex items-center justify-between bg-muted/20 min-h-[120px]">
              <div className="flex items-center space-x-4">
                <div className="p-3 bg-primary/10 rounded-md text-primary">
                  <FileText className="h-6 w-6" />
                </div>
                <div className="flex flex-col max-w-[280px]">
                  <span className="font-medium text-sm truncate">{cvFileName}</span>
                  {isUploading ? (
                    <span className="text-xs text-muted-foreground flex items-center gap-1.5 mt-1">
                      <Loader2 className="h-3 w-3 animate-spin text-primary" />
                      {t("cvSelectionExtracting")}
                    </span>
                  ) : (
                    <span className="text-xs text-emerald-600 font-medium flex items-center gap-1.5 mt-1">
                      <Check className="h-3.5 w-3.5" />
                      {t("cvSelectionUploadSuccess")}
                    </span>
                  )}
                </div>
              </div>
              <Button 
                variant="ghost" 
                size="icon" 
                onClick={handleRemoveFile} 
                disabled={isUploading}
                className="text-muted-foreground hover:text-destructive"
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            </div>
          )}
        </TabsContent>

        {/* Tab: Paste Text */}
        <TabsContent value="paste" className="mt-4">
          <Textarea
            placeholder={t("cvSelectionPastePlaceholder")}
            className="min-h-[220px] font-sans resize-none"
            value={cvText}
            onChange={(e) => setCvText(e.target.value)}
          />
        </TabsContent>

        {/* Tab: My Saved CVs */}
        <TabsContent value="saved" className="mt-4">
          <div className="space-y-4">
            <Label className="text-xs text-muted-foreground">{t("cvSelectionSavedLabel")}</Label>
            {isLoadingCvs ? (
              <div className="flex items-center justify-center p-8 border rounded-lg bg-muted/10">
                <Loader2 className="h-6 w-6 animate-spin text-primary" />
              </div>
            ) : myCvs.length === 0 ? (
              <div className="text-center p-8 border border-dashed rounded-lg">
                <p className="text-sm text-muted-foreground">{t("cvSelectionNoSaved")}</p>
              </div>
            ) : (
              <Select value={selectedCvId} onValueChange={(val) => setSelectedCvId(val || '')}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder={t("cvSelectionSelectPlaceholder")}>
                    {selectedCvId 
                      ? (myCvs.find((c) => c.id === selectedCvId)?.fileName || 
                         (myCvs.find((c) => c.id === selectedCvId) 
                           ? `Resume - ${new Date(myCvs.find((c) => c.id === selectedCvId).createdAt).toLocaleDateString()}` 
                           : undefined))
                      : undefined}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  {myCvs.map((cv) => (
                      <SelectItem key={cv.id} value={cv.id}>
                        {cv.fileName || `Resume - ${new Date(cv.createdAt).toLocaleDateString()}`}
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
