'use client';

import Link from 'next/link';
import { useMyLearningPaths, useDeleteLearningPath } from '@/hooks/useLearningPath';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { Loader2, Plus, Trash2, Map, CheckCircle2, Clock, Circle } from 'lucide-react';

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
          {paths.map((path) => {
            const totalModules = path.pathData.length;
            const completedModules = path.pathData.filter(m => m.completed).length;
            const progressPercentage = totalModules === 0 ? 0 : Math.round((completedModules / totalModules) * 100);

            return (
            <Card key={path.id} className="flex flex-col relative overflow-hidden group">
              {progressPercentage === 100 && (
                <div className="absolute top-0 left-0 w-full h-1 bg-green-500" />
              )}
              {progressPercentage > 0 && progressPercentage < 100 && (
                <div className="absolute top-0 left-0 h-1 bg-blue-500 transition-all" style={{ width: `${progressPercentage}%` }} />
              )}

              <CardHeader className="flex flex-row items-start justify-between space-y-0 pb-2">
                <div className="flex-1 pr-4">
                  <div className="flex items-center gap-2 mb-2">
                    <Badge variant={
                      path.status === 'Completed' ? 'default' : 
                      path.status === 'In Progress' ? 'secondary' : 'outline'
                    } className={
                      path.status === 'Completed' ? 'bg-green-500 hover:bg-green-600' : 
                      path.status === 'In Progress' ? 'bg-blue-100 text-blue-700 hover:bg-blue-200 border-none' : ''
                    }>
                      {path.status === 'Completed' && <CheckCircle2 className="w-3 h-3 mr-1" />}
                      {path.status === 'In Progress' && <Clock className="w-3 h-3 mr-1" />}
                      {path.status === 'Not Started' && <Circle className="w-3 h-3 mr-1 text-muted-foreground" />}
                      {path.status}
                    </Badge>
                  </div>
                  <CardTitle className="text-xl line-clamp-2 leading-tight" title={path.title}>
                    {path.title}
                  </CardTitle>
                  <CardDescription className="mt-1">
                    Generated on {new Date(path.createdAt).toLocaleDateString('vi-VN')}
                  </CardDescription>
                </div>
                <Button 
                  variant="ghost" 
                  size="icon" 
                  className="text-muted-foreground hover:text-destructive shrink-0 opacity-0 group-hover:opacity-100 transition-opacity"
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
                <div className="space-y-5 mb-6">
                  
                  <div className="space-y-2">
                    <div className="flex justify-between text-sm">
                      <span className="text-muted-foreground">Progress</span>
                      <span className="font-medium">{progressPercentage}%</span>
                    </div>
                    <Progress value={progressPercentage} className="h-2" />
                  </div>

                  <div className="flex items-center gap-4 text-sm text-muted-foreground bg-muted/50 p-3 rounded-md">
                    <div className="flex flex-col">
                      <span className="font-semibold text-foreground text-lg">{completedModules}/{totalModules}</span>
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
            );
          })}
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
