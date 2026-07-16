import { useState, useMemo } from 'react';
import { useMutation } from '@tanstack/react-query';
import { optimizeService, ApplySuggestionPayload } from '@/services/optimize.service';
import type { MatchingOutput, ImprovementSuggestion } from '@/types/cv.types';
import { toast } from 'sonner';

export interface AcceptedChange {
  suggestion: ImprovementSuggestion;
  modifiedText: string;
}

export function useCvOptimizer(matchOutput: MatchingOutput | null, sessionId: string | null) {
  // 1. Filter and sort valid suggestions
  const validSuggestions = useMemo(() => {
    if (!matchOutput?.improvements) return [];
    
    // Only keep suggestions that have example before/after
    const withExamples = matchOutput.improvements.filter(
      imp => imp.example?.before && imp.example?.after
    );
    
    // Sort by priority to create quick wins
    const priorityWeight: Record<string, number> = { high: 3, medium: 2, low: 1 };
    return withExamples.sort((a, b) => 
      (priorityWeight[b.priority] || 0) - (priorityWeight[a.priority] || 0)
    );
  }, [matchOutput]);

  // 2. Base score and theoretical max
  const baseScore = matchOutput?.jdFit?.score || 0;
  // If base score is e.g. 70, the theoretical max could be 95 after all optimizations.
  const theoreticalMax = Math.min(baseScore + (validSuggestions.length * 3), 98); 
  const availablePoints = theoreticalMax - baseScore;
  
  // Calculate points per suggestion based on priority
  const totalWeight = validSuggestions.reduce((sum, s) => {
    return sum + (s.priority === 'high' ? 3 : s.priority === 'medium' ? 2 : 1);
  }, 0);

  // 3. State
  const [currentIndex, setCurrentIndex] = useState(0);
  const [currentScore, setCurrentScore] = useState(baseScore);
  const [acceptedChanges, setAcceptedChanges] = useState<AcceptedChange[]>([]);
  const [isManuallyCompleted, setIsManuallyCompleted] = useState(false);

  // Derived state
  const isComplete = isManuallyCompleted || (validSuggestions.length > 0 && currentIndex >= validSuggestions.length);
  const currentSuggestion = validSuggestions[currentIndex];
  const progressPercent = validSuggestions.length > 0 
    ? Math.round((currentIndex / validSuggestions.length) * 100) 
    : 100;

  // 4. Handlers
  const handleAccept = (modifiedText: string) => {
    if (!currentSuggestion) return;
    
    // Add to accepted list
    setAcceptedChanges(prev => [
      ...prev,
      { suggestion: currentSuggestion, modifiedText }
    ]);
    
    // Increase score based on priority weight
    if (totalWeight > 0) {
      const weight = currentSuggestion.priority === 'high' ? 3 : currentSuggestion.priority === 'medium' ? 2 : 1;
      const pointGained = (weight / totalWeight) * availablePoints;
      setCurrentScore(prev => Math.min(prev + pointGained, theoreticalMax));
    }

    if (sessionId && currentSuggestion.example?.before && modifiedText) {
      optimizeService.applySuggestion(sessionId, `sugg-${currentIndex}`, {
        action: modifiedText !== currentSuggestion.example.after ? 'edit' : 'accept',
        editedText: modifiedText,
        originalText: currentSuggestion.example.before,
        suggestedText: currentSuggestion.example.after
      }).catch(err => {
         console.error('Failed to apply suggestion:', err);
         toast.error('Failed to sync change with server, but continuing locally.');
      });
    }
    
    advance();
  };

  const handleSkip = () => {
    if (sessionId && currentSuggestion) {
      optimizeService.applySuggestion(sessionId, `sugg-${currentIndex}`, { action: 'skip' })
        .catch(console.error);
    }
    advance();
  };

  const advance = () => {
    if (currentIndex < validSuggestions.length - 1) {
      setCurrentIndex(prev => prev + 1);
    } else {
      setIsManuallyCompleted(true);
    }
  };

  return {
    state: {
      validSuggestions,
      currentIndex,
      currentSuggestion,
      currentScore,
      progressPercent,
      isComplete,
      acceptedChanges,
    },
    handlers: {
      handleAccept,
      handleSkip,
    }
  };
}
