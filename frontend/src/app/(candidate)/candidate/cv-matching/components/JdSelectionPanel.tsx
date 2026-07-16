import React from 'react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Loader2 } from 'lucide-react';

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
  return (
    <div className="flex flex-col space-y-3">
      <Label className="text-base font-semibold">Select Job Description (JD)</Label>
      <Tabs value={jdTab} onValueChange={setJdTab} className="w-full">
        <TabsList className="grid w-full grid-cols-2">
          <TabsTrigger value="paste">Paste JD Text</TabsTrigger>
          <TabsTrigger value="saved">From Saved Jobs</TabsTrigger>
        </TabsList>

        {/* Tab: Paste JD Text */}
        <TabsContent value="paste" className="mt-4">
          <Textarea
            placeholder="Paste the Job Description requirements here..."
            className="min-h-[220px] font-sans resize-none"
            value={jdText}
            onChange={(e) => setJdText(e.target.value)}
          />
        </TabsContent>

        {/* Tab: Saved Jobs */}
        <TabsContent value="saved" className="mt-4">
          <div className="space-y-4">
            <Label className="text-xs text-muted-foreground">Select one of your bookmarked job postings</Label>
            {isLoadingJobs ? (
              <div className="flex items-center justify-center p-8 border rounded-lg bg-muted/10">
                <Loader2 className="h-6 w-6 animate-spin text-primary" />
              </div>
            ) : savedJobs.length === 0 ? (
              <div className="text-center p-8 border border-dashed rounded-lg">
                <p className="text-sm text-muted-foreground">No saved jobs found.</p>
              </div>
            ) : (
              <Select value={selectedJobId} onValueChange={(val) => setSelectedJobId(val || '')}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select a job" />
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
