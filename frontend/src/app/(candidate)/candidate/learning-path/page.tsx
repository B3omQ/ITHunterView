'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { useMyLearningPaths, useDeleteLearningPath } from '@/hooks/useLearningPath';
import { Button, buttonVariants } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { CardSkeleton } from '@/components/shared/CardSkeleton';
import { EmptyState } from '@/components/shared/EmptyState';
import { Loader2, Plus, Trash2, Map, Sparkles, ArrowRight, BookOpen, CheckCircle2, MoreHorizontal, Eye, Play } from 'lucide-react';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
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
  const [pathToDelete, setPathToDelete] = useState<string | null>(null);

  const handleConfirmDelete = () => {
    if (pathToDelete) {
      deleteMutation.mutate(pathToDelete);
      setPathToDelete(null);
    }
  };

  const paths = myPathsData?.data || [];

  return (
    <div className="w-full pb-8 space-y-6">
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
                <CardContent className="flex flex-col gap-3">
                  <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                    <div className="flex items-center gap-3 flex-1 min-w-0">
                      {/* Left: Status Icon */}
                      <Image src="/images/mascotAvatarLearning.png" alt="Mascot" width={44} height={44} className={`w-11 h-11 rounded-lg shrink-0 object-cover border bg-white dark:bg-slate-900 ${
                        path.status === 'Completed'
                          ? 'border-emerald-200 dark:border-emerald-800 ring-1 ring-emerald-500/20'
                          : path.status === 'In Progress'
                            ? 'border-blue-200 dark:border-blue-800 ring-1 ring-blue-500/20'
                            : 'border-slate-200 dark:border-slate-800'
                      }`} />

                      {/* Center: Info */}
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 min-w-0">
                          <Link href={`/candidate/learning-path/${path.id}`} passHref>
                            <span
                              className="font-medium text-base text-foreground group-hover:text-primary transition-colors line-clamp-1 leading-snug cursor-pointer"
                              title={path.title}
                            >
                              {path.title}
                            </span>
                          </Link>
                        </div>
                        <div className="flex items-center gap-4 flex-wrap mt-1 text-sm text-slate-600">
                          <div className="flex items-center">
                            <Badge className={`shrink-0 text-xs px-2 py-0.5 border-none font-medium ${
                              path.status === 'Completed'
                                ? 'bg-emerald-500/10 text-emerald-700'
                                : path.status === 'In Progress'
                                  ? 'bg-blue-500/10 text-blue-700'
                                  : 'bg-slate-200/70 text-slate-700 dark:bg-slate-800 dark:text-slate-400'
                            }`}>
                              {path.status}
                            </Badge>
                          </div>
                          <span className="flex items-center gap-1.5">
                            <BookOpen className="h-4 w-4 shrink-0 text-slate-400" />
                            {modules.length} module{modules.length !== 1 ? 's' : ''}
                          </span>
                          <span className="flex items-center gap-1.5">
                            <CheckCircle2 className="h-4 w-4 shrink-0 text-emerald-500" />
                            {completedTasks}/{totalTasks} tasks
                          </span>
                        </div>
                      </div>
                    </div>

                    {/* Right: Progress & Actions */}
                    <div className="flex flex-col sm:flex-row items-end sm:items-center gap-4 sm:gap-8 shrink-0 mt-2 md:mt-0">
                      
                      {/* Progress Bar */}
                      <div className="flex items-center gap-2 w-full sm:w-40">
                        <Progress 
                          value={progressPercentage} 
                          className="flex-1 [&_[data-slot=progress-track]]:h-2.5 [&_[data-slot=progress-track]]:bg-slate-200 dark:[&_[data-slot=progress-track]]:bg-slate-800" 
                        />
                        <span className="font-semibold text-slate-700 text-sm w-9 text-right">{progressPercentage}%</span>
                      </div>

                      {/* Actions */}
                      <div className="flex items-center gap-2 shrink-0">
                      <Link 
                        href={`/candidate/learning-path/${path.id}`}
                        className={buttonVariants({ variant: 'outline', size: 'sm', className: 'gap-1.5 h-9' })}
                      >
                        {path.status === 'In Progress' ? (
                          <><Play className="w-4 h-4 fill-current" /> Continue</>
                        ) : (
                          <><Eye className="w-4 h-4" /> View Path</>
                        )}
                      </Link>

                      <Popover>
                        <PopoverTrigger className="inline-flex items-center justify-center h-9 w-9 text-slate-500 hover:text-foreground shrink-0 border border-transparent hover:border-border hover:bg-muted/50 rounded-lg transition-colors focus-visible:outline-hidden focus-visible:ring-1 focus-visible:ring-ring">
                          <MoreHorizontal className="h-4 w-4" />
                        </PopoverTrigger>
                        <PopoverContent align="end" className="w-48 p-1">
                          <div className="flex flex-col">
                            <Button 
                              variant="ghost" 
                              className="w-full justify-start gap-2 h-9 text-rose-600 hover:text-rose-700 hover:bg-rose-50"
                              onClick={() => setPathToDelete(path.id)}
                            >
                              <Trash2 className="h-4 w-4" />
                              <span>Delete Path</span>
                            </Button>
                          </div>
                        </PopoverContent>
                      </Popover>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
            );
          })}
        </div>
      ) : (
        <EmptyState 
          title="No learning paths yet" 
          description="Let our AI analyze your CV, mock interviews, or manual goals to generate a personalized step-by-step career path."
          imageUrl="/images/emptyLearningPath.png"
        >
          <Link href="/candidate/learning-path/new">
            <Button className="mt-4 bg-gradient-to-r from-blue-600 to-blue-400 hover:from-blue-700 hover:to-blue-500 text-white shadow-lg shadow-blue-500/25 transition-all">
              <Plus className="mr-1 h-4 w-4" />
              Create Your First Path
              <Sparkles className="mr-2 h-4 w-4 ml-1" />
            </Button>
          </Link>
        </EmptyState>
      )}

      {/* Delete Confirmation Dialog */}
      <Dialog open={!!pathToDelete} onOpenChange={(open) => !open && setPathToDelete(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Learning Path?</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete this learning path? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="mt-4 flex flex-col sm:flex-row gap-2 justify-end">
            <Button variant="outline" onClick={() => setPathToDelete(null)} disabled={deleteMutation.isPending}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={handleConfirmDelete} disabled={deleteMutation.isPending}>
              {deleteMutation.isPending ? 'Deleting...' : 'Delete'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
