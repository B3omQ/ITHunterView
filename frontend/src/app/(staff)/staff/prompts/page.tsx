'use client';

import { usePrompts } from '@/hooks/use-prompts';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { format } from 'date-fns';
import { Loader2, Settings2 } from 'lucide-react';
import Link from 'next/link';
import { APP_ROUTES } from '@/lib/constants';

export default function PromptsPage() {
  const { data, isLoading, isError } = usePrompts(1, 20);

  return (
    <div className="max-w-7xl mx-auto pt-6 pb-10 px-4 md:px-8 space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Prompt Management</h1>
        <p className="text-muted-foreground mt-2">
          Manage system prompts, AI configurations, and switch active versions.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>System Prompts</CardTitle>
          <CardDescription>
            List of all available prompts used by the LLM throughout the application.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="flex justify-center p-8">
              <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
            </div>
          ) : isError ? (
            <div className="text-center text-destructive p-8">Failed to load prompts</div>
          ) : (
            <div className="rounded-md border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Prompt Key</TableHead>
                    <TableHead>Active Version</TableHead>
                    <TableHead>Last Updated</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data?.data?.items?.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={4} className="text-center text-muted-foreground py-8">
                        No prompts found
                      </TableCell>
                    </TableRow>
                  )}
                  {data?.data?.items?.map((prompt) => (
                    <TableRow key={prompt.id}>
                      <TableCell className="font-medium">
                        {prompt.promptKey}
                        {prompt.description && (
                          <span className="block text-xs text-muted-foreground font-normal mt-1">
                            {prompt.description}
                          </span>
                        )}
                      </TableCell>
                      <TableCell>
                        {prompt.activeVersionTag ? (
                          <Badge variant="default" className="bg-green-600/10 text-green-700 hover:bg-green-600/20 border-green-600/20">
                            {prompt.activeVersionTag}
                          </Badge>
                        ) : (
                          <Badge variant="outline" className="text-muted-foreground">
                            No Active Version
                          </Badge>
                        )}
                      </TableCell>
                      <TableCell className="text-muted-foreground text-sm">
                        {prompt.updatedAt ? format(new Date(prompt.updatedAt), 'MMM dd, yyyy HH:mm') : format(new Date(prompt.createdAt), 'MMM dd, yyyy HH:mm')}
                      </TableCell>
                      <TableCell className="text-right">
                        <Link href={`${APP_ROUTES.STAFF.PROMPTS}/${prompt.id}`}>
                          <Button variant="outline" size="sm">
                            <Settings2 className="w-4 h-4 mr-2" />
                            Manage
                          </Button>
                        </Link>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
