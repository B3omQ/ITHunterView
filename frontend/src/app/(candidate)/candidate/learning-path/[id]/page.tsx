'use client';

import { use } from 'react';
import Link from 'next/link';
import { useLearningPath } from '@/hooks/useLearningPath';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Loader2, ArrowLeft, Clock } from 'lucide-react';
import { LearningModule } from '@/types/learning-path.types';
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion";

export default function LearningPathDetailPage({ params }: { params: Promise<{ id: string }> }) {
  // Use React.use to unwrap the Promise for params
  const { id } = use(params);
  const { data: pathData, isLoading, isError } = useLearningPath(id);

  if (isLoading) {
    return (
      <div className="container mx-auto py-16 flex justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (isError || !pathData?.data) {
    return (
      <div className="container mx-auto py-16 text-center">
        <h2 className="text-2xl font-bold mb-4">Path Not Found</h2>
        <p className="text-muted-foreground mb-6">Could not load the learning path details.</p>
        <Link href="/candidate/learning-path" passHref>
          <Button>Back to Learning Paths</Button>
        </Link>
      </div>
    );
  }

  const path = pathData.data;

  return (
    <div className="container mx-auto py-8 space-y-8 max-w-6xl">
      <div className="flex items-center gap-4">
        <Link href="/candidate/learning-path" passHref>
          <Button variant="ghost" size="icon">
            <ArrowLeft className="h-5 w-5" />
          </Button>
        </Link>
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Learning Path Details</h1>
          <p className="text-muted-foreground mt-1">
            Generated on {new Date(path.createdAt).toLocaleDateString('vi-VN')}
          </p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Modules</CardTitle>
          <CardDescription>
            {path.pathData.length} modules • {path.pathData.reduce((acc, curr) => acc + curr.durationWeeks, 0)} weeks total
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Accordion className="w-full space-y-4">
            {path.pathData.map((module: LearningModule, index: number) => (
              <AccordionItem key={index} value={`module-${index}`} className="border rounded-lg px-4 bg-card">
                <AccordionTrigger className="hover:no-underline py-4">
                  <div className="flex flex-col sm:flex-row sm:items-center text-left gap-2 sm:gap-4 w-full pr-4">
                    <div className="flex items-center gap-3">
                      <div className="flex items-center justify-center w-8 h-8 rounded-full bg-primary/10 text-primary font-semibold shrink-0">
                        {index + 1}
                      </div>
                      <h4 className="font-semibold text-lg">{module.title}</h4>
                    </div>
                    
                    <div className="flex items-center gap-3 sm:ml-auto">
                      {module.gapSource && (
                        <Badge variant="outline" className="shrink-0 text-xs bg-muted/50 border-muted">
                          {module.gapSource === 'cv-jd-match'
                            ? 'CV-JD Gap'
                            : module.gapSource === 'interview'
                            ? 'Interview Gap'
                            : 'Both'}
                        </Badge>
                      )}
                      <div className="flex items-center gap-1.5 text-sm text-muted-foreground shrink-0 bg-muted/30 px-2.5 py-1 rounded-md">
                        <Clock className="w-4 h-4" />
                        <span className="font-medium">{module.durationWeeks} Weeks</span>
                      </div>
                    </div>
                  </div>
                </AccordionTrigger>
                
                <AccordionContent className="pt-2 pb-6 border-t">
                  <div className="space-y-6 mt-4 pl-11">
                    <div>
                      <h5 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider mb-2">Description</h5>
                      <p className="text-foreground leading-relaxed">
                        {module.description}
                      </p>
                    </div>
                    
                    <div>
                      <h5 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider mb-3">Key Topics & Tasks</h5>
                      <div className="flex flex-wrap gap-2">
                        {module.skills.map((skill, skillIdx) => (
                          <Badge 
                            key={skillIdx} 
                            variant="secondary" 
                            className="bg-primary/5 hover:bg-primary/10 text-primary border border-primary/20 px-3 py-1.5 font-medium rounded-md transition-colors"
                          >
                            {skill}
                          </Badge>
                        ))}
                      </div>
                    </div>
                  </div>
                </AccordionContent>
              </AccordionItem>
            ))}
          </Accordion>
        </CardContent>
      </Card>
    </div>
  );
}
