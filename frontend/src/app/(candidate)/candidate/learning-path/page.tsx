'use client';

import Link from 'next/link';
import { DotLottieReact } from '@lottiefiles/dotlottie-react';
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
    <div className="container mx-auto py-8 space-y-8 max-w-6xl">
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
              <TooltipTrigger className="cursor-not-allowed" onClick={(e) => e.preventDefault()}>
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
            <Link href="/candidate/learning-path/new" passHref>
              <Button className="bg-gradient-to-r from-blue-600 to-blue-400 hover:from-blue-700 hover:to-blue-500 text-white shadow-lg shadow-blue-500/25 transition-all">
                <Plus className="mr-1 h-4 w-4" />
                Create New Path
                <Sparkles className="mr-2 h-4 w-4 ml-1" />
              </Button>
            </Link>
          )
        )}
      </div>

      {isLoading ? (
        <div className="flex justify-center items-center py-24">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      ) : paths.length > 0 ? (
        <div className="grid grid-cols-1 lg:grid-cols-10 gap-8 lg:gap-12 w-full">
          <div className="col-span-1 lg:col-span-6 flex flex-col gap-4">
            {paths.map((path) => {
              let totalTasks = 0;
              let completedTasks = 0;
              path.pathData.forEach((m: any) => {
                if (m.tasks && m.tasks.length > 0) {
                  totalTasks += m.tasks.length;
                  completedTasks += m.tasks.filter((t: any) => t.completed).length;
                }
              });
              const progressPercentage = totalTasks === 0 ? 0 : Math.round((completedTasks / totalTasks) * 100);

              return (
                <Card key={path.id} className="flex flex-col relative overflow-hidden group rounded-[20px] border-none shadow-[0_10px_40px_rgba(0,0,0,0.04)] hover:shadow-[0_15px_50px_rgba(0,0,0,0.06)] transition-all duration-300 bg-card p-1">

                  <CardHeader className="flex flex-col items-start justify-between space-y-0 pb-1 pt-3 px-4 relative">
                    <Dialog>
                      <DialogTrigger
                        render={
                          <Button
                            variant="ghost"
                            size="icon"
                            className="absolute top-2 right-2 text-muted-foreground hover:text-destructive shrink-0 opacity-0 group-hover:opacity-100 transition-opacity h-8 w-8"
                            disabled={deleteMutation.isPending && deleteMutation.variables === path.id}
                          />
                        }
                      >
                        {deleteMutation.isPending && deleteMutation.variables === path.id ? (
                          <Loader2 className="h-3 w-3 animate-spin" />
                        ) : (
                          <Trash2 className="h-3 w-3" />
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

                    <div className="w-full pr-8">
                      <div className="flex items-center gap-2 mb-2">
                        <div className="bg-primary/10 p-1.5 rounded-lg flex-shrink-0">
                          <Map className="w-3.5 h-3.5 text-primary" />
                        </div>
                        <Badge className={`font-semibold px-2 py-0.5 border-none shadow-none text-[10px] rounded-md ${path.status === 'Completed' ? 'bg-[#E6F4EA] text-[#137333] hover:bg-[#CEEAD6]' :
                          path.status === 'In Progress' ? 'bg-[#E6F0FF] text-[#0052CC] hover:bg-[#CCE0FF]' :
                            'bg-[#F3F4F6] text-gray-700 hover:bg-gray-200'
                          }`}>
                          {path.status}
                        </Badge>
                      </div>
                      <Link href={`/candidate/learning-path/${path.id}`} passHref>
                        <CardTitle className="text-base font-extrabold tracking-tight line-clamp-2 leading-snug hover:text-primary transition-colors cursor-pointer" title={path.title}>
                          {path.title}
                        </CardTitle>
                      </Link>
                    </div>
                  </CardHeader>
                  <CardContent className="flex-1 flex flex-col justify-end pt-3 pb-3 px-4">
                    <div className="flex items-center gap-3 mt-auto">
                      <Progress value={progressPercentage} className="flex-1 [&_[data-slot=progress-track]]:h-4 [&_[data-slot=progress-track]]:bg-muted/60" />
                      <span className="font-bold text-primary text-sm w-8 text-right">{progressPercentage}%</span>
                    </div>
                  </CardContent>
                </Card>
              );
            })}
          </div>
          <div className="hidden lg:block col-span-1 lg:col-span-4">
            <div className="sticky top-8 flex flex-col items-center justify-center p-8">
              <div className="relative w-full max-w-md aspect-square flex items-center justify-center">
                <DotLottieReact
                  src="/images/ai-animation.json"
                  loop
                  autoplay
                  speed={0.25}
                  className="w-full h-full drop-shadow-2xl"
                />
              </div>
            </div>
          </div>
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
            <Button size="lg" className="h-12 px-8 bg-gradient-to-r from-blue-600 to-blue-400 hover:from-blue-700 hover:to-blue-500 text-white shadow-lg shadow-blue-500/25 transition-all">
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
