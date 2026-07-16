import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Download, Eye, Save, Sparkles, CheckCircle2 } from 'lucide-react';
import type { AcceptedChange } from '@/hooks/useCvOptimizer';

interface OptimizerCompletionProps {
  acceptedChanges: AcceptedChange[];
  finalScore: number;
  onPreview: () => void;
  onSave: () => void;
  onDownload: () => void;
  onBack: () => void;
}

export function OptimizerCompletion({
  acceptedChanges,
  finalScore,
  onPreview,
  onSave,
  onDownload,
  onBack
}: OptimizerCompletionProps) {
  return (
    <div className="max-w-2xl mx-auto w-full py-8 px-4 animate-in fade-in zoom-in-95 duration-500">
      <div className="flex flex-col items-center text-center space-y-4 mb-8">
        <div className="h-20 w-20 bg-primary/10 text-primary rounded-full flex items-center justify-center mb-2">
          <Sparkles className="h-10 w-10" />
        </div>
        <h1 className="text-3xl font-extrabold tracking-tight">Optimization Complete!</h1>
        <p className="text-muted-foreground text-base max-w-md">
          You've successfully reviewed all AI suggestions. Your estimated matching score is now <strong className="text-foreground">{finalScore.toFixed(1)}</strong>.
        </p>
      </div>

      <Card className="shadow-lg border-primary/20">
        <CardHeader className="bg-muted/30 border-b pb-4">
          <CardTitle className="text-lg flex items-center gap-2">
            <CheckCircle2 className="h-5 w-5 text-emerald-500" />
            Summary of Changes ({acceptedChanges.length})
          </CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {acceptedChanges.length === 0 ? (
            <div className="p-8 text-center text-muted-foreground">
              You didn't accept any suggestions.
            </div>
          ) : (
            <div className="max-h-[300px] overflow-y-auto divide-y">
              {acceptedChanges.map((change, idx) => (
                <div key={idx} className="p-4 hover:bg-muted/10 transition-colors">
                  <div className="flex items-start justify-between gap-4 mb-2">
                    <span className="text-xs font-semibold uppercase text-primary tracking-wider">
                      {change.suggestion.category.replace(/_/g, ' ')}
                    </span>
                  </div>
                  <p className="text-sm text-muted-foreground mb-1 line-clamp-1 line-through opacity-70">
                    {change.suggestion.example?.before}
                  </p>
                  <p className="text-sm font-medium text-foreground line-clamp-2">
                    {change.modifiedText}
                  </p>
                </div>
              ))}
            </div>
          )}
        </CardContent>
        <div className="bg-muted/20 p-4 border-t flex flex-col sm:flex-row gap-3 justify-between">
          <Button variant="outline" onClick={onBack} className="w-full sm:w-auto">
            Back to Match Result
          </Button>
          <div className="flex flex-col sm:flex-row gap-3 w-full sm:w-auto">
            {/* These buttons are placeholders for the actual integration */}
            <Button variant="secondary" onClick={onPreview} className="gap-2">
              <Eye className="h-4 w-4" /> Preview
            </Button>
            <Button variant="outline" onClick={onDownload} className="gap-2">
              <Download className="h-4 w-4" /> Download
            </Button>
            <Button onClick={onSave} className="gap-2">
              <Save className="h-4 w-4" /> Save to My CV
            </Button>
          </div>
        </div>
      </Card>
    </div>
  );
}
