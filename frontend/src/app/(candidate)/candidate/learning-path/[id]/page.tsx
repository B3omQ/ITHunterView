'use client';

import { use } from 'react';
import Link from 'next/link';
import { useLearningPath, useToggleTaskCompletion as useLearningPathToggle } from '@/hooks/useLearningPath';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button, buttonVariants } from '@/components/ui/button';
import { Progress } from '@/components/ui/progress';
import { Checkbox } from '@/components/ui/checkbox';
import { Loader2, ArrowLeft, Clock, CheckCircle2, Circle, Lock } from 'lucide-react';
import { LearningModule } from '@/types/learning-path.types';
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion";
import NewLearningPathPage from '../new/page';

export default function LearningPathDetailPage({ params }: { params: Promise<{ id: string }> }) {
  // Use React.use to unwrap the Promise for params
  const { id } = use(params);
  const { data: pathData, isLoading, isError } = useLearningPath(id);
  const toggleMutation = useLearningPathToggle();

  if (id === 'new') {
    return <NewLearningPathPage />;
  }

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
        <Link href="/candidate/learning-path" className={buttonVariants({ variant: 'default' })}>
          Back to Learning Paths
        </Link>
      </div>
    );
  }

  const path = pathData.data;
  const pathDataContent = path.pathData;
  const modules = pathDataContent.modules || [];
  
  let totalTasks = 0;
  let completedTasks = 0;
  modules.forEach((m: LearningModule) => {
    if (m.tasks && m.tasks.length > 0) {
      totalTasks += m.tasks.length;
      completedTasks += m.tasks.filter(t => t.completed).length;
    }
  });
  
  const progressPercentage = totalTasks === 0 ? 0 : Math.round((completedTasks / totalTasks) * 100);

  return (
    <div className="w-full pb-8 space-y-8">
      <div className="flex items-center gap-4">
        <Link href="/candidate/learning-path" className={buttonVariants({ variant: 'ghost', size: 'icon' })}>
          <ArrowLeft className="h-5 w-5" />
        </Link>
        <div className="flex-1">
          <div className="flex items-center gap-3 mb-1">
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
          <h1 className="text-3xl font-bold tracking-tight">{path.title}</h1>
          <p className="text-muted-foreground mt-1">
            Generated on {new Date(path.createdAt).toLocaleDateString('vi-VN')}
          </p>
        </div>
      </div>

      {pathDataContent.target_profile && (
        <Card className="bg-primary/5 border-primary/20">
          <CardHeader>
            <CardTitle className="text-xl">Target Role: {pathDataContent.target_profile.role_name}</CardTitle>
            <CardDescription className="text-primary/80">
              {pathDataContent.target_profile.description}
            </CardDescription>
          </CardHeader>
        </Card>
      )}

      {pathDataContent.gap_summary && (
        <Card>
          <CardHeader>
            <CardTitle>Skill Gap Analysis</CardTitle>
            <CardDescription>
              We identified {pathDataContent.gap_summary.total_gaps} core skill gaps for this role.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {pathDataContent.gap_summary.gaps.map((gap, idx) => (
                <div key={idx} className="flex justify-between items-center border-b pb-2 last:border-0">
                  <div>
                    <span className="font-semibold">{gap.skill_name}</span> 
                    <span className="text-xs text-muted-foreground ml-2">({gap.skill_code})</span>
                  </div>
                  <div className="flex gap-4 text-sm">
                    <span className="text-muted-foreground">Current: Lvl {gap.current_level}</span>
                    <span className="text-primary font-medium">Target: Lvl {gap.target_level}</span>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader>
          <div className="flex justify-between items-end mb-4">
            <div>
              <CardTitle>Modules Overview</CardTitle>
              <CardDescription className="mt-1">
                {completedTasks}/{totalTasks} tasks completed
              </CardDescription>
            </div>
            <div className="text-2xl font-bold text-primary">{progressPercentage}%</div>
          </div>
          <Progress value={progressPercentage} className="h-3" />
        </CardHeader>
        <CardContent>
          <Accordion className="w-full space-y-4">
            {modules.map((module: LearningModule, index: number) => {
              const isModCompleted = (m: LearningModule) => Boolean(m.completed || (m.tasks && m.tasks.length > 0 && m.tasks.every(t => t.completed)));
              const currentModCompleted = isModCompleted(module);
              const isModuleLocked = index > 0 && !isModCompleted(modules[index - 1]);
              return (
              <AccordionItem key={index} value={`module-${index}`} className={`border rounded-lg px-4 transition-colors ${currentModCompleted ? 'bg-muted/30 border-muted' : 'bg-card'} ${isModuleLocked ? 'opacity-70 grayscale-[0.5]' : ''}`}>
                <AccordionTrigger className="hover:no-underline py-4">
                  <div className="flex flex-col sm:flex-row sm:items-center text-left gap-2 sm:gap-4 w-full pr-4">
                    <div className="flex items-center gap-3">
                      <div className={`flex items-center justify-center w-8 h-8 rounded-full shrink-0 font-semibold ${currentModCompleted ? 'bg-green-100 text-green-700' : isModuleLocked ? 'bg-muted text-muted-foreground' : 'bg-primary/10 text-primary'}`}>
                        {currentModCompleted ? <CheckCircle2 className="w-5 h-5" /> : isModuleLocked ? <Lock className="w-4 h-4" /> : index + 1}
                      </div>
                      <div>
                        <div className="flex items-center gap-2">
                          <h4 className={`font-semibold text-lg ${currentModCompleted ? 'text-muted-foreground' : ''}`}>{module.title}</h4>
                        </div>
                        {module.tasks && module.tasks.length > 0 && (
                          <p className="text-xs text-muted-foreground font-normal mt-0.5">
                            {module.tasks.filter(t => t.completed).length}/{module.tasks.length} tasks completed
                          </p>
                        )}
                      </div>
                    </div>
                    
                    <div className="flex items-center gap-3 sm:ml-auto">
                      {module.sfia_target && (
                        <Badge variant="outline" className="shrink-0 text-xs bg-muted/50 border-muted">
                          {module.sfia_target.skill_code} (Lvl {module.sfia_target.from_level} → {module.sfia_target.to_level})
                        </Badge>
                      )}
                    </div>
                  </div>
                </AccordionTrigger>
                
                <AccordionContent className="pt-2 pb-6 border-t">
                  <div className="space-y-6 mt-4 pl-11">
                    <div>
                      <p className="text-foreground leading-relaxed">
                        {module.description}
                      </p>
                    </div>
                    
                    {module.tasks && module.tasks.length > 0 ? (
                      <div>
                        <h5 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider mb-3">Tasks</h5>
                        <div className="space-y-3">
                          {module.tasks.map((task, taskIdx) => {
                            const isTaskLocked = isModuleLocked || (taskIdx > 0 && !module.tasks[taskIdx - 1].completed);
                            const isUncheckDisabled = task.completed && taskIdx < module.tasks.length - 1 && module.tasks[taskIdx + 1].completed;
                            const isCheckboxDisabled = toggleMutation.isPending || isTaskLocked || isUncheckDisabled;

                            return (
                            <div key={taskIdx} className={`flex items-start gap-3 p-4 border rounded-lg transition-colors ${task.completed ? 'bg-muted/50 border-muted' : isTaskLocked ? 'bg-muted/20 opacity-70 cursor-not-allowed' : 'bg-card hover:bg-muted/20'}`}>
                              <Checkbox 
                                checked={task.completed || false} 
                                onCheckedChange={() => !isCheckboxDisabled && toggleMutation.mutate({ pathId: path.id, moduleIndex: index, taskIndex: taskIdx })}
                                disabled={isCheckboxDisabled}
                                className={`mt-1 w-5 h-5 border-2 rounded-md ${task.completed ? 'data-[state=checked]:bg-primary data-[state=checked]:border-primary' : isTaskLocked ? 'border-muted-foreground/30 bg-muted/50' : ''}`}
                              />
                              <div className="space-y-1 -mt-0.5">
                                <div className="flex items-center gap-2">
                                  <h6 className={`font-medium leading-none ${task.completed ? 'text-muted-foreground line-through decoration-muted-foreground/50' : 'text-foreground'}`}>
                                    {task.title}
                                  </h6>
                                  {isTaskLocked && <Lock className="w-3.5 h-3.5 text-muted-foreground/50" />}
                                </div>
                                <p className="text-sm text-muted-foreground leading-snug">{task.description}</p>
                              </div>
                            </div>
                            );
                          })}
                        </div>
                      </div>
                    ) : null}
                  </div>
                </AccordionContent>
              </AccordionItem>
            )})}
          </Accordion>
        </CardContent>
      </Card>
    </div>
  );
}
