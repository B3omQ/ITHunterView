import React, { useEffect, useState } from 'react';
import { Progress } from '@/components/ui/progress';
import { ArrowLeft, Target } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useRouter } from 'next/navigation';

interface OptimizerHeaderProps {
  currentStep: number;
  totalSteps: number;
  currentScore: number;
  progressPercent: number;
  onBack: () => void;
}

export function OptimizerHeader({
  currentStep,
  totalSteps,
  currentScore,
  progressPercent,
  onBack
}: OptimizerHeaderProps) {
  // Animate score change
  const [displayScore, setDisplayScore] = useState(currentScore);

  useEffect(() => {
    let animationFrame: number;
    const animateScore = () => {
      setDisplayScore((prev) => {
        if (Math.abs(prev - currentScore) < 0.5) return currentScore;
        return prev + (currentScore - prev) * 0.1;
      });
      if (Math.abs(displayScore - currentScore) > 0.5) {
        animationFrame = requestAnimationFrame(animateScore);
      }
    };
    animationFrame = requestAnimationFrame(animateScore);
    return () => cancelAnimationFrame(animationFrame);
  }, [currentScore, displayScore]);

  return (
    <div className="bg-card border-b sticky top-0 z-10 w-full shadow-sm">
      <div className="max-w-4xl mx-auto px-4 py-4">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-4">
            <Button variant="ghost" size="icon" onClick={onBack} className="h-8 w-8">
              <ArrowLeft className="h-4 w-4" />
            </Button>
            <div>
              <h2 className="text-lg font-bold tracking-tight">Optimize CV</h2>
              <p className="text-xs text-muted-foreground font-medium">
                Gợi ý {Math.min(currentStep, totalSteps)}/{totalSteps}
              </p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <div className="text-right">
              <p className="text-xs text-muted-foreground uppercase font-semibold tracking-wider">
                Matching Score
              </p>
              <div className="flex items-center justify-end gap-1.5 text-primary">
                <Target className="h-4 w-4" />
                <span className="text-xl font-bold font-mono">
                  {displayScore.toFixed(1)}
                </span>
              </div>
            </div>
          </div>
        </div>

        <Progress value={progressPercent} className="h-1.5 bg-muted/50" />
      </div>
    </div>
  );
}
