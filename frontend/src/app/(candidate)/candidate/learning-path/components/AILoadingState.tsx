'use client';

import { useState, useEffect } from 'react';
import { CheckCircle2, Circle, Loader2 } from 'lucide-react';

const MESSAGES = [
  "Booting AI core...",
  "Analyzing candidate profile...",
  "Cross-referencing market skills...",
  "Structuring curriculum...",
  "Gathering recommended modules...",
  "Finalizing learning path...",
];

export default function AILoadingState() {
  const [messageIndex, setMessageIndex] = useState(0);
  const [activeStep, setActiveStep] = useState(0);

  // Cycle messages
  useEffect(() => {
    const messageInterval = setInterval(() => {
      setMessageIndex((prev) => (prev + 1) % MESSAGES.length);
    }, 2500);
    return () => clearInterval(messageInterval);
  }, []);

  // Advance pipeline steps artificially
  useEffect(() => {
    const stepInterval = setInterval(() => {
      setActiveStep((prev) => {
        if (prev >= 2) return 2; // Stay at last step
        return prev + 1;
      });
    }, 5000); // Advance step every 5s
    return () => clearInterval(stepInterval);
  }, []);

  const steps = [
    { label: "Analyzing Profile" },
    { label: "Structuring Curriculum" },
    { label: "Finalizing Path" }
  ];

  return (
    <div className="flex flex-col items-center justify-center py-10 px-4 min-h-[400px] w-full animate-in fade-in duration-500 overflow-hidden">

      {/* Compact Orb and Rings */}
      <div className="relative flex items-center justify-center w-32 h-32 mb-8">
        {/* Outer Ring */}
        <div className="absolute inset-0 rounded-full border border-primary/20 border-t-primary/60 border-l-primary/60 animate-[spin_8s_linear_infinite]" />

        {/* Inner Ring */}
        <div className="absolute inset-2 rounded-full border border-blue-500/20 border-b-blue-500/60 border-r-blue-500/60 animate-[spin_4s_linear_infinite_reverse]" />

        {/* Core Orb */}
        <div className="absolute inset-5 bg-gradient-to-tr from-primary to-blue-400 rounded-full blur-sm opacity-80 animate-pulse" />
        <div className="absolute inset-6 bg-background rounded-full shadow-[0_0_20px_rgba(59,130,246,0.5)] flex items-center justify-center z-10">
          <SparklesIcon className="w-6 h-6 text-primary animate-pulse" />
        </div>
      </div>

      {/* Dynamic Text */}
      <div className="h-6 mb-8">
        <p className="text-lg font-medium text-foreground text-center animate-pulse transition-all duration-300">
          {MESSAGES[messageIndex]}
        </p>
      </div>

      {/* Compact 3-Step Pipeline Card */}
      <div className="w-full max-w-sm bg-card border rounded-xl p-5 shadow-sm">
        <h3 className="font-semibold text-xs uppercase tracking-wider text-muted-foreground mb-4 text-center">
          Generation Pipeline
        </h3>

        <div className="space-y-3">
          {steps.map((step, index) => {
            const isCompleted = activeStep > index;
            const isActive = activeStep === index;

            return (
              <div
                key={index}
                className={`flex items-center gap-3 p-3 rounded-lg border transition-all duration-300 ${isActive ? 'bg-primary/5 border-primary/20 shadow-sm scale-[1.02]' :
                    isCompleted ? 'bg-card border-border/50 opacity-70' :
                      'bg-muted/30 border-transparent opacity-40'
                  }`}
              >
                {/* Icon Marker */}
                <div className="flex-shrink-0 flex items-center justify-center w-8 h-8 rounded-full bg-background border shadow-sm transition-colors duration-500">
                  {isCompleted ? (
                    <CheckCircle2 className="w-4 h-4 text-green-500" />
                  ) : isActive ? (
                    <div className="w-2.5 h-2.5 bg-primary rounded-full animate-ping" />
                  ) : (
                    <Circle className="w-3 h-3 text-muted-foreground/50" />
                  )}
                </div>

                {/* Content */}
                <div className="flex items-center gap-2">
                  {isActive && <Loader2 className="w-3.5 h-3.5 text-primary animate-spin shrink-0" />}
                  <h4 className={`font-medium text-sm ${isActive ? 'text-primary' : isCompleted ? 'text-foreground' : 'text-muted-foreground'}`}>
                    {step.label}
                  </h4>
                </div>
              </div>
            );
          })}
        </div>
      </div>

    </div>
  );
}

function SparklesIcon(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg
      {...props}
      xmlns="http://www.w3.org/2000/svg"
      width="24"
      height="24"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M9.937 15.5A2 2 0 0 0 8.5 14.063l-6.135-1.582a.5.5 0 0 1 0-.962L8.5 9.936A2 2 0 0 0 9.937 8.5l1.582-6.135a.5.5 0 0 1 .963 0L14.063 8.5A2 2 0 0 0 15.5 9.937l6.135 1.581a.5.5 0 0 1 0 .964L15.5 14.063a2 2 0 0 0-1.437 1.437l-1.582 6.135a.5.5 0 0 1-.963 0z" />
    </svg>
  );
}