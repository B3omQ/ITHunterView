'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useGetMyCvs } from '@/hooks/useCv';
import { useOptimizeCv, useCvOptimizationHistory } from '@/hooks/useCvOptimizer';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Loader2, ExternalLink } from 'lucide-react';
import Link from 'next/link';

export default function CvOptimizerPage() {
  const router = useRouter();
  const { data: cvsResponse, isLoading: isLoadingCvs } = useGetMyCvs();
  const { data: historyResponse, isLoading: isLoadingHistory } = useCvOptimizationHistory();
  const optimizeMutation = useOptimizeCv();

  const [selectedCvId, setSelectedCvId] = useState<string>('');
  const [targetJdText, setTargetJdText] = useState<string>('');
  const [progressIndex, setProgressIndex] = useState<number>(0);

  const PROGRESS_MESSAGES = [
    "Khởi tạo kết nối...",
    "Đang đọc và bóc tách dữ liệu CV...",
    "Đang tải Job Description (nếu có)...",
    "Đang gửi dữ liệu cho AI phân tích...",
    "AI đang phân tích các kỹ năng và từ khóa...",
    "Chuyên gia AI đang soạn thảo các gợi ý cải thiện...",
    "Sắp xong rồi, đang định dạng lại báo cáo...",
    "Hoàn tất quá trình, xin vui lòng đợi thêm chút nữa..."
  ];

  useEffect(() => {
    let interval: NodeJS.Timeout;
    if (optimizeMutation.isPending) {
      setProgressIndex(0);
      interval = setInterval(() => {
        setProgressIndex((prev) => Math.min(prev + 1, PROGRESS_MESSAGES.length - 1));
      }, 8000); // Update message every 8 seconds
    }
    return () => clearInterval(interval);
  }, [optimizeMutation.isPending]);

  const handleOptimize = async () => {
    if (!selectedCvId) return;
    
    try {
      const response = await optimizeMutation.mutateAsync({
        cvId: selectedCvId,
        targetJdText: targetJdText.trim() ? targetJdText : undefined,
      });
      if (response?.data?.id) {
        router.push(`/candidate/cv-optimizer/${response.data.id}`);
      }
    } catch (error) {
      console.error('Failed to optimize CV:', error);
    }
  };

  const cvs = cvsResponse?.data || [];
  const history = historyResponse?.data || [];

  return (
    <div className="container mx-auto py-8 space-y-8 max-w-5xl">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">CV Optimizer</h1>
        <p className="text-muted-foreground mt-2">
          Enhance your CV using our AI expert. Tailor it to a specific Job Description or follow general ATS best practices.
        </p>
      </div>

      <div className="grid md:grid-cols-2 gap-8">
        <Card>
          <CardHeader>
            <CardTitle>Optimize CV</CardTitle>
            <CardDescription>Select a CV and optionally paste a JD.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">Select CV</label>
              {isLoadingCvs ? (
                <div className="h-10 flex items-center"><Loader2 className="animate-spin h-4 w-4" /></div>
              ) : (
                <Select value={selectedCvId} onValueChange={(val) => val && setSelectedCvId(val)}>
                  <SelectTrigger>
                    <SelectValue placeholder="Select a CV to optimize" />
                  </SelectTrigger>
                  <SelectContent>
                    {cvs.map((cv) => (
                      <SelectItem key={cv.id} value={cv.id}>
                        {cv.fileName} {cv.isPrimary ? '(Primary)' : ''}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Target Job Description (Optional)</label>
              <Textarea 
                placeholder="Paste the Job Description here to get tailored feedback..." 
                className="h-32 resize-none"
                value={targetJdText}
                onChange={(e) => setTargetJdText(e.target.value)}
              />
            </div>

            <Button 
              className="w-full" 
              onClick={handleOptimize} 
              disabled={!selectedCvId || optimizeMutation.isPending}
            >
              {optimizeMutation.isPending ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" /> {PROGRESS_MESSAGES[progressIndex]}
                </>
              ) : (
                'Optimize My CV'
              )}
            </Button>
            
            {optimizeMutation.isError && (
              <p className="text-sm text-destructive mt-2">Failed to optimize CV. Please try again.</p>
            )}
          </CardContent>
        </Card>

        <div>
          <h2 className="text-xl font-semibold mb-4">Past Optimizations</h2>
          {isLoadingHistory ? (
            <div className="flex justify-center p-8"><Loader2 className="animate-spin" /></div>
          ) : history.length === 0 ? (
            <div className="text-center p-8 border rounded-lg border-dashed text-muted-foreground">
              No optimization history yet.
            </div>
          ) : (
            <div className="space-y-4">
              {history.map((opt) => (
                <Card key={opt.id}>
                  <CardHeader className="p-4">
                    <div className="flex justify-between items-start">
                      <div>
                        <CardTitle className="text-base">
                          {opt.targetJdText ? 'Tailored Optimization' : 'General Optimization'}
                        </CardTitle>
                        <CardDescription>
                          {new Date(opt.createdAt).toLocaleDateString()}
                        </CardDescription>
                      </div>
                      <Link href={`/candidate/cv-optimizer/${opt.id}`}>
                        <Button variant="outline" size="sm">
                          View Report <ExternalLink className="ml-2 h-3 w-3" />
                        </Button>
                      </Link>
                    </div>
                  </CardHeader>
                </Card>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
