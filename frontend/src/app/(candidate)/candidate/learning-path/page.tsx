'use client';

import Link from 'next/link';
import { useMyLearningPaths, useDeleteLearningPath } from '@/hooks/useLearningPath';
import { Button, buttonVariants } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { CardSkeleton } from '@/components/shared/CardSkeleton';
import { Loader2, Plus, Trash2, Map, Sparkles, ArrowRight, BookOpen, CheckCircle2 } from 'lucide-react';
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
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";

export default function LearningPathDashboard() {
  const { data: myPathsData, isLoading } = useMyLearningPaths();
  const deleteMutation = useDeleteLearningPath();

  const paths = myPathsData?.data || [];

  return (
    <div className="w-full pb-8 space-y-8">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Learning Paths</h1>
          <p className="text-muted-foreground mt-2">
            Manage your AI-generated learning journeys.
          </p>
        </div>
        {paths.length > 0 && (
          paths.length >= 3 ? (
            <Tooltip>
              <TooltipTrigger render={<span className="cursor-not-allowed" />} onClick={(e) => e.preventDefault()}>
                <span className="pointer-events-none">
                  <Button disabled className="bg-muted text-muted-foreground cursor-not-allowed border-none shadow-none hover:bg-muted">
                    <Plus className="mr-1 h-4 w-4" />
                    Create New Path
                    <Sparkles className="mr-2 h-4 w-4 ml-1" />
                  </Button>
                </span>
              </TooltipTrigger>
              <TooltipContent>
                <p>You have reached the maximum of 3 learning paths.</p>
              </TooltipContent>
            </Tooltip>
          ) : (
            <Link href="/candidate/learning-path/new" className={buttonVariants({ variant: 'default', className: "bg-gradient-to-r from-blue-600 to-blue-400 hover:from-blue-700 hover:to-blue-500 text-white shadow-lg shadow-blue-500/25 transition-all" })}>
              <Plus className="mr-1 h-4 w-4" />
              Create New Path
              <Sparkles className="mr-2 h-4 w-4 ml-1" />
            </Link>
          )
        )}
      </div>

      {isLoading ? (
        <div className="flex flex-col gap-3">
          {[1, 2, 3].map((n) => <CardSkeleton key={n} />)}
        </div>
      ) : paths.length > 0 ? (
        <div className="flex flex-col gap-4">
          {paths.map((path) => {
            let totalTasks = 0;
            let completedTasks = 0;
            const modules = path.pathData?.modules || [];
            modules.forEach((m: any) => {
              if (m.tasks && m.tasks.length > 0) {
                totalTasks += m.tasks.length;
                completedTasks += m.tasks.filter((t: any) => t.completed).length;
              }
            });
            const progressPercentage = totalTasks === 0 ? 0 : Math.round((completedTasks / totalTasks) * 100);

            return (
              <Card key={path.id} className="group hover:border-primary/50 transition-colors">
                <CardContent className="p-4 flex items-center gap-3">
                  {/* Left: Status Icon */}
                  <div className={`shrink-0 w-11 h-11 rounded-lg flex items-center justify-center ${
                    path.status === 'Completed'
                      ? 'bg-emerald-500/10 text-emerald-500'
                      : path.status === 'In Progress'
                        ? 'bg-blue-500/10 text-blue-500'
                        : 'bg-muted text-slate-400'
                  }`}>
                    <Map className="w-5 h-5" />
                  </div>

                  {/* Center: Info */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 min-w-0 mb-0.5">
                      <Link href={`/candidate/learning-path/${path.id}`} passHref>
                        <span
                          className="font-semibold text-base text-foreground group-hover:text-primary transition-colors truncate cursor-pointer"
                          title={path.title}
                        >
                          {path.title}
                        </span>
                      </Link>
                      <Badge className={`shrink-0 text-[10px] px-1.5 py-0 border-none font-semibold ${
                        path.status === 'Completed'
                          ? 'bg-emerald-500/10 text-emerald-700'
                          : path.status === 'In Progress'
                            ? 'bg-blue-500/10 text-blue-700'
                            : 'bg-muted text-muted-foreground'
                      }`}>
                        {path.status}
                      </Badge>
                    </div>
                    <div className="flex items-center gap-3 text-xs text-muted-foreground mb-2">
                      <span className="flex items-center gap-1">
                        <BookOpen className="h-3 w-3" />
                        {modules.length} module{modules.length !== 1 ? 's' : ''}
                      </span>
                      <span className="flex items-center gap-1">
                        <CheckCircle2 className="h-3 w-3 text-emerald-500" />
                        {completedTasks}/{totalTasks} tasks
                      </span>
                    </div>
                    <div className="flex items-center gap-2">
                      <Progress value={progressPercentage} className="flex-1 [&_[data-slot=progress-track]]:h-1.5 [&_[data-slot=progress-track]]:bg-muted/60" />
                      <span className="font-semibold text-primary text-xs w-8 text-right">{progressPercentage}%</span>
                    </div>
                  </div>

                  {/* Right: Actions */}
                  <div className="flex items-center gap-1 shrink-0">
                    <Dialog>
                      <DialogTrigger
                        render={
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 text-muted-foreground hover:text-destructive hover:bg-destructive/10 rounded-lg transition-colors opacity-0 group-hover:opacity-100"
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
                    <ArrowRight className="h-4 w-4 text-primary transform group-hover:translate-x-1 transition-transform" />
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
          <Link href="/candidate/learning-path/new" className={buttonVariants({ size: 'lg', className: "h-12 px-8 bg-gradient-to-r from-blue-600 to-blue-400 hover:from-blue-700 hover:to-blue-500 text-white shadow-lg shadow-blue-500/25 transition-all" })}>
            <Plus className="mr-1 h-5 w-5" />
            <Sparkles className="mr-2 h-5 w-5" />
            Create Your First Path
          </Link>
        </div>
      )}
    </div>
  );
}
