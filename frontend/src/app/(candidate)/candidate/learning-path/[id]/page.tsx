'use client';

import { use } from 'react';
import Link from 'next/link';
import { useLearningPath } from '@/hooks/useLearningPath';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Loader2, ArrowLeft } from 'lucide-react';
import { LearningModule } from '@/types/learning-path.types';

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
    <div className="container mx-auto py-8 space-y-8 max-w-4xl">
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
          <div className="space-y-6">
            {path.pathData.map((module: LearningModule, index: number) => (
              <div key={index} className="border-l-2 border-primary pl-6 pb-2 relative">
                <div className="absolute w-3 h-3 bg-primary rounded-full -left-[7px] top-1.5" />
                <div className="flex items-start gap-2 flex-wrap mb-2">
                  <h4 className="font-bold text-lg">{module.title}</h4>
                  {module.gapSource && (
                    <Badge variant="secondary" className="mt-1 text-xs">
                      {module.gapSource === 'cv-jd-match'
                        ? 'CV-JD Gap'
                        : module.gapSource === 'interview'
                        ? 'Interview Gap'
                        : 'Both'}
                    </Badge>
                  )}
                </div>
                <p className="text-muted-foreground mb-3 leading-relaxed">{module.description}</p>
                <div className="flex gap-2 items-center text-sm bg-muted/50 p-3 rounded-md">
                  <span className="font-semibold shrink-0">{module.durationWeeks} Weeks</span>
                  <span className="text-muted-foreground shrink-0">•</span>
                  <span className="text-foreground">{module.skills.join(', ')}</span>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
