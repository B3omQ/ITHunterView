'use client';

import Link from 'next/link';
import { useMyLearningPaths, useDeleteLearningPath } from '@/hooks/useLearningPath';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { Loader2, Plus, Trash2, Map, CheckCircle2, Clock, Circle, Sparkles } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  DialogClose,
} from '@/components/ui/dialog';

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
            <Button className="bg-gradient-to-r from-blue-600 to-emerald-400 hover:from-blue-700 hover:to-emerald-500 text-white shadow-lg shadow-blue-500/25 transition-all">
              <Plus className="mr-1 h-4 w-4" />
              Create New Path
              <Sparkles className="mr-2 h-4 w-4" />
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
              <Card key={path.id} className="flex flex-col relative overflow-hidden group rounded-[20px] border-none shadow-[0_10px_40px_rgba(0,0,0,0.04)] hover:shadow-[0_15px_50px_rgba(0,0,0,0.06)] transition-all duration-300 bg-card p-2">

                <CardHeader className="flex flex-row items-start justify-between space-y-0 pb-2">
                  <div className="flex-1 pr-4">
                    <div className="flex items-center gap-3 mb-4">
                      <div className="bg-primary/10 p-1.5 rounded-lg flex-shrink-0">
                        <Map className="w-4 h-4 text-primary" />
                      </div>
                      <Badge className={`font-medium px-3 py-1 border-none shadow-none text-xs rounded-lg ${path.status === 'Completed' ? 'bg-[#E6F4EA] text-[#137333] hover:bg-[#CEEAD6]' :
                        path.status === 'In Progress' ? 'bg-[#E6F0FF] text-[#0052CC] hover:bg-[#CCE0FF]' :
                          'bg-[#F3F4F6] text-gray-700 hover:bg-gray-200'
                        }`}>
                        {path.status}
                      </Badge>
                    </div>
                    <Link href={`/candidate/learning-path/${path.id}`} passHref>
                      <CardTitle className="text-lg font-extrabold tracking-tight line-clamp-2 leading-snug hover:text-primary transition-colors cursor-pointer" title={path.title}>
                        {path.title}
                      </CardTitle>
                    </Link>
                  </div>
                  <Dialog>
                    <DialogTrigger
                      render={
                        <Button
                          variant="ghost"
                          size="icon"
                          className="text-muted-foreground hover:text-destructive shrink-0 opacity-0 group-hover:opacity-100 transition-opacity"
                          disabled={deleteMutation.isPending && deleteMutation.variables === path.id}
                        />
                      }
                    >
                      {deleteMutation.isPending && deleteMutation.variables === path.id ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <Trash2 className="h-4 w-4" />
                      )}
                    </DialogTrigger>
                    <DialogContent>
                      <DialogHeader>
                        <DialogTitle>Delete Learning Path</DialogTitle>
                        <DialogDescription>
                          Are you sure you want to delete this learning path? This action cannot be undone.
                        </DialogDescription>
                      </DialogHeader>
                      <DialogFooter>
                        <DialogClose render={<Button variant="outline" />}>Cancel</DialogClose>
                        <DialogClose render={<Button variant="destructive" onClick={() => deleteMutation.mutate(path.id)} />}>Delete</DialogClose>
                      </DialogFooter>
                    </DialogContent>
                  </Dialog>
                </CardHeader>
                <CardContent className="flex-1 flex flex-col justify-end pt-8 pb-4">
                  <div className="space-y-4 mt-auto">
                    <div className="flex justify-between items-end text-sm">
                      <div className="flex flex-col">
                        <span className="text-muted-foreground text-[10px] uppercase tracking-wider font-bold mb-1">Progress</span>
                        <span className="font-bold text-foreground text-sm leading-none">{completedModules} / {totalModules} Modules</span>
                      </div>
                      <span className="font-bold text-primary text-base leading-none">{progressPercentage}%</span>
                    </div>
                    <Progress value={progressPercentage} className="h-2.5 rounded-full bg-muted/60" />

                    <div className="pt-3 border-t border-border/40 mt-4">
                      <p className="text-[11px] text-muted-foreground/60 font-medium tracking-wide">
                        Generated on {new Date(path.createdAt).toLocaleDateString('vi-VN')}
                      </p>
                    </div>
                  </div>
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
            <Button size="lg" className="h-12 px-8 bg-gradient-to-r from-blue-600 to-emerald-400 hover:from-blue-700 hover:to-emerald-500 text-white shadow-lg shadow-blue-500/25 transition-all">
              <Plus className="mr-1 h-5 w-5" />
              <Sparkles className="mr-2 h-5 w-5" />
              Create Your First Path
            </Button>
          </Link>
        </div>
      )}
    </div>
  );
}
