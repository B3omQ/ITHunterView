'use client';

import Link from 'next/link';
import { useMyLearningPaths, useDeleteLearningPath } from '@/hooks/useLearningPath';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Loader2, Plus, Trash2, Map } from 'lucide-react';

export default function LearningPathDashboard() {
  const { data: myPathsData, isLoading } = useMyLearningPaths();
  const deleteMutation = useDeleteLearningPath();

  const paths = myPathsData?.data || [];

  return (
    <div className="container mx-auto py-8 space-y-8 max-w-6xl">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Learning Paths</h1>
          <p className="text-muted-foreground mt-2">
            Manage your AI-generated learning journeys.
          </p>
        </div>
        {paths.length > 0 && (
          <Link href="/candidate/learning-path/new" passHref>
            <Button>
              <Plus className="mr-2 h-4 w-4" />
              Create New Path
            </Button>
          </Link>
        )}
      </div>

      {isLoading ? (
        <div className="flex justify-center items-center py-24">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      ) : paths.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {paths.map((path) => (
            <Card key={path.id} className="flex flex-col">
              <CardHeader className="flex flex-row items-start justify-between space-y-0 pb-2">
                <div>
                  <CardTitle className="text-xl">Generated Path</CardTitle>
                  <CardDescription className="mt-1">
                    {new Date(path.createdAt).toLocaleDateString('vi-VN')}
                  </CardDescription>
                </div>
                <Button 
                  variant="ghost" 
                  size="icon" 
                  className="text-muted-foreground hover:text-destructive -mr-2 -mt-2"
                  onClick={() => {
                    if (confirm('Are you sure you want to delete this learning path?')) {
                      deleteMutation.mutate(path.id);
                    }
                  }}
                  disabled={deleteMutation.isPending && deleteMutation.variables === path.id}
                >
                  {deleteMutation.isPending && deleteMutation.variables === path.id ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    <Trash2 className="h-4 w-4" />
                  )}
                </Button>
              </CardHeader>
              <CardContent className="flex-1 flex flex-col justify-between pt-4">
                <div className="space-y-4 mb-6">
                  <div className="flex items-center gap-4 text-sm text-muted-foreground bg-muted/50 p-3 rounded-md">
                    <div className="flex flex-col">
                      <span className="font-semibold text-foreground text-lg">{path.pathData.length}</span>
                      <span className="text-xs uppercase tracking-wider">Modules</span>
                    </div>
                    <div className="w-px h-8 bg-border"></div>
                    <div className="flex flex-col">
                      <span className="font-semibold text-foreground text-lg">
                        {path.pathData.reduce((acc, curr) => acc + curr.durationWeeks, 0)}
                      </span>
                      <span className="text-xs uppercase tracking-wider">Est. Weeks</span>
                    </div>
                  </div>
                </div>
                
                <Link href={`/candidate/learning-path/${path.id}`} passHref className="mt-auto">
                  <Button variant="outline" className="w-full">
                    View Details
                  </Button>
                </Link>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <div className="flex flex-col items-center justify-center py-24 text-center border-2 border-dashed rounded-xl bg-muted/10">
          <div className="bg-primary/10 p-4 rounded-full mb-4">
            <Map className="h-12 w-12 text-primary" />
          </div>
          <h3 className="text-2xl font-semibold tracking-tight mb-2">No learning paths yet</h3>
          <p className="text-muted-foreground max-w-md mb-8">
            Let our AI analyze your CV, mock interviews, or manual goals to generate a personalized step-by-step career path.
          </p>
          <Link href="/candidate/learning-path/new" passHref>
            <Button size="lg" className="h-12 px-8">
              <Plus className="mr-2 h-5 w-5" />
              Create Your First Path
            </Button>
          </Link>
        </div>
      )}
    </div>
  );
}
