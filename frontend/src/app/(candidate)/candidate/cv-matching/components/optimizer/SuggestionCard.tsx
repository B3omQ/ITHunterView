import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardFooter } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import { Check, Edit3, SkipForward, AlertCircle, ArrowRight } from 'lucide-react';
import type { ImprovementSuggestion } from '@/types/cv.types';

interface SuggestionCardProps {
  suggestion: ImprovementSuggestion;
  onAccept: (modifiedText: string) => void;
  onSkip: () => void;
}

export function SuggestionCard({ suggestion, onAccept, onSkip }: SuggestionCardProps) {
  const [isEditing, setIsEditing] = useState(false);
  const [editText, setEditText] = useState('');

  // Reset local state when suggestion changes
  useEffect(() => {
    setIsEditing(false);
    setEditText(suggestion.example?.after || '');
  }, [suggestion]);

  if (!suggestion.example) return null;

  const handleAcceptClick = () => {
    onAccept(isEditing ? editText : suggestion.example!.after);
  };

  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case 'high': return 'bg-red-100 text-red-700 border-red-200 dark:bg-red-900/30 dark:text-red-400';
      case 'medium': return 'bg-amber-100 text-amber-700 border-amber-200 dark:bg-amber-900/30 dark:text-amber-400';
      case 'low': return 'bg-blue-100 text-blue-700 border-blue-200 dark:bg-blue-900/30 dark:text-blue-400';
      default: return 'bg-muted text-muted-foreground';
    }
  };

  return (
    <Card className="w-full shadow-md border-muted max-w-3xl mx-auto overflow-hidden animate-in slide-in-from-bottom-4 duration-500">
      {/* Category header */}
      <div className="bg-muted/30 px-6 py-3 border-b flex items-center justify-between">
        <Badge variant="outline" className="uppercase tracking-wider text-[10px] font-semibold">
          {suggestion.category.replace(/_/g, ' ')}
        </Badge>
        <Badge variant="outline" className={`capitalize text-[10px] ${getPriorityColor(suggestion.priority)}`}>
          {suggestion.priority} Impact
        </Badge>
      </div>

      <CardContent className="p-6 space-y-6">
        {/* Explanation */}
        <div className="flex items-start gap-3 bg-primary/5 p-4 rounded-lg border border-primary/10">
          <AlertCircle className="h-5 w-5 text-primary shrink-0 mt-0.5" />
          <div className="space-y-1">
            <h4 className="font-semibold text-sm text-foreground">{suggestion.issue}</h4>
            <p className="text-sm text-muted-foreground leading-relaxed">{suggestion.action}</p>
          </div>
        </div>

        {/* Text Diffing Area */}
        <div className="space-y-4">
          <div className="space-y-2">
            <label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Original Text</label>
            <div className="p-4 bg-muted/20 border rounded-lg text-sm text-muted-foreground line-through decoration-red-500/50 decoration-2">
              {suggestion.example.before}
            </div>
          </div>

          <div className="flex justify-center">
            <ArrowRight className="h-5 w-5 text-muted-foreground/50 rotate-90 sm:rotate-0" />
          </div>

          <div className="space-y-2">
            <label className="text-xs font-semibold text-primary uppercase tracking-wider">AI Suggestion</label>
            
            {isEditing ? (
              <Textarea 
                value={editText}
                onChange={(e) => setEditText(e.target.value)}
                className="min-h-[120px] font-sans text-sm focus-visible:ring-primary"
                autoFocus
              />
            ) : (
              <div className="p-4 bg-primary/5 border border-primary/20 rounded-lg text-sm text-foreground shadow-sm">
                {suggestion.example.after}
              </div>
            )}
          </div>
        </div>
      </CardContent>

      <CardFooter className="bg-muted/10 px-6 py-4 flex flex-col sm:flex-row items-center justify-between gap-3 border-t">
        <Button 
          variant="ghost" 
          onClick={onSkip}
          className="w-full sm:w-auto text-muted-foreground hover:text-foreground"
        >
          <SkipForward className="h-4 w-4 mr-2" />
          Skip
        </Button>
        
        <div className="flex flex-col sm:flex-row w-full sm:w-auto gap-3">
          <Button 
            variant="outline" 
            onClick={() => setIsEditing(!isEditing)}
            className="w-full sm:w-auto"
          >
            <Edit3 className="h-4 w-4 mr-2" />
            {isEditing ? "Cancel Edit" : "Edit Manually"}
          </Button>
          
          <Button 
            onClick={handleAcceptClick}
            className="w-full sm:w-auto bg-primary hover:bg-primary/90 gap-2"
          >
            <Check className="h-4 w-4" />
            Accept {isEditing ? "Edits" : "Suggestion"}
          </Button>
        </div>
      </CardFooter>
    </Card>
  );
}
