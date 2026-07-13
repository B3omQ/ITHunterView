'use client';

import { useCvOptimizationById } from '@/hooks/useCvOptimizer';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { ArrowLeft, CheckCircle2, XCircle, AlertCircle, Loader2 } from 'lucide-react';
import Link from 'next/link';
import { useParams } from 'next/navigation';

export default function CvOptimizerResultPage() {
  const params = useParams();
  const sessionId = params?.sessionId as string;
  const { data: response, isLoading, isError } = useCvOptimizationById(sessionId);

  if (isLoading) {
    return (
      <div className="flex h-[50vh] items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
        <span className="ml-2">Loading optimization report...</span>
      </div>
    );
  }

  if (isError || !response?.data) {
    return (
      <div className="container mx-auto py-8">
        <div className="bg-destructive/10 text-destructive p-4 rounded-md flex items-center">
          <AlertCircle className="mr-2" /> Report not found or an error occurred.
        </div>
        <Link href="/candidate/cv-optimizer">
          <Button variant="outline" className="mt-4">
            <ArrowLeft className="mr-2 h-4 w-4" /> Back to Optimizer
          </Button>
        </Link>
      </div>
    );
  }

  const report = response.data;
  const feedback = report.feedbackData;

  return (
    <div className="container mx-auto py-8 space-y-6 max-w-5xl">
      <div className="flex items-center space-x-4">
        <Link href="/candidate/cv-optimizer">
          <Button variant="outline" size="icon">
            <ArrowLeft className="h-4 w-4" />
          </Button>
        </Link>
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Optimization Report</h1>
          <p className="text-muted-foreground mt-1">
            Generated on {new Date(report.createdAt).toLocaleString()}
          </p>
        </div>
      </div>

      <div className="grid md:grid-cols-3 gap-6">
        <div className="md:col-span-1 space-y-6">
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-lg">ATS Score</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-4xl font-bold text-primary">{feedback.overallScore || 0}/100</div>
              <p className="text-sm text-muted-foreground mt-1">Estimated by AI expert</p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-lg flex items-center">
                <CheckCircle2 className="mr-2 h-5 w-5 text-green-500" /> Strengths
              </CardTitle>
            </CardHeader>
            <CardContent>
              <ul className="list-disc pl-5 space-y-1 text-sm">
                {feedback.strengths?.map((s, i) => (
                  <li key={i}>{s}</li>
                )) || <li className="text-muted-foreground list-none">No notable strengths.</li>}
              </ul>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-lg flex items-center">
                <XCircle className="mr-2 h-5 w-5 text-red-500" /> Weaknesses
              </CardTitle>
            </CardHeader>
            <CardContent>
              <ul className="list-disc pl-5 space-y-1 text-sm">
                {feedback.weaknesses?.map((w, i) => (
                  <li key={i}>{w}</li>
                )) || <li className="text-muted-foreground list-none">No notable weaknesses.</li>}
              </ul>
            </CardContent>
          </Card>
        </div>

        <div className="md:col-span-2 space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Missing Keywords</CardTitle>
              <CardDescription>Consider adding these keywords to pass ATS filters.</CardDescription>
            </CardHeader>
            <CardContent className="flex flex-wrap gap-2">
              {feedback.missingKeywords?.map((kw, i) => (
                <Badge key={i} variant="secondary">{kw}</Badge>
              )) || <span className="text-sm text-muted-foreground">No critical keywords missing.</span>}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Suggested Edits</CardTitle>
              <CardDescription>Rewritten bullet points for maximum impact.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              {feedback.suggestedEdits?.map((edit, i) => (
                <div key={i} className="space-y-2 border-b pb-4 last:border-0 last:pb-0">
                  <Badge>{edit.section}</Badge>
                  <div className="grid md:grid-cols-2 gap-4 mt-2">
                    <div className="bg-destructive/10 p-3 rounded text-sm text-destructive border border-destructive/20">
                      <div className="font-semibold mb-1">Original:</div>
                      {edit.originalText}
                    </div>
                    <div className="bg-green-500/10 p-3 rounded text-sm text-green-700 border border-green-500/20">
                      <div className="font-semibold mb-1">Suggested:</div>
                      {edit.suggestedText}
                    </div>
                  </div>
                  <p className="text-sm text-muted-foreground mt-2"><span className="font-medium">Why:</span> {edit.reason}</p>
                </div>
              )) || <div className="text-sm text-muted-foreground">No specific edits suggested.</div>}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
