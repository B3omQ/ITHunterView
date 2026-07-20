"use client"

import * as React from "react"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import { notificationService, type NotificationDto } from "@/services/notification.service"
import { useSignalR } from "@/hooks/useSignalR"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog"
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetDescription } from "@/components/ui/sheet"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Button } from "@/components/ui/button"
import { CheckCircle2, Circle, Bell } from "lucide-react"

interface NotificationDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function NotificationDialog({ open, onOpenChange }: NotificationDialogProps) {
  const queryClient = useQueryClient()
  const connection = useSignalR('/hubs/notification')

  React.useEffect(() => {
    if (connection) {
      connection.on("ReceiveNotification", () => {
        // Refetch notifications when a real-time event is received
        queryClient.invalidateQueries({ queryKey: ['notifications'] })
      })
    }
  }, [connection, queryClient])
  
  const getTimeAgo = (dateStr: string) => {
    const date = new Date(dateStr)
    const now = new Date()
    const diffMs = now.getTime() - date.getTime()
    const diffMins = Math.floor(diffMs / 60000)
    const diffHours = Math.floor(diffMins / 60)
    const diffDays = Math.floor(diffHours / 24)
    if (diffMins < 1) return "Just now"
    if (diffMins < 60) return `${diffMins}m ago`
    if (diffHours < 24) return `${diffHours}h ago`
    return `${diffDays}d ago`
  }

  const { data, isLoading } = useQuery({
    queryKey: ['notifications'],
    queryFn: () => notificationService.getUserNotifications(1, 50),
    enabled: open
  })

  const markAsReadMutation = useMutation({
    mutationFn: (id: string) => notificationService.markAsRead(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] })
    }
  })

  const notifications = data?.data || []

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full sm:max-w-md p-0 flex flex-col gap-0 border-l border-border/50">
        <SheetHeader className="px-6 pt-6 pb-4 border-b border-border/50 shrink-0 text-left">
          <SheetTitle className="flex items-center gap-2">
            <Bell size={20} className="text-primary" />
            Your Notifications
          </SheetTitle>
          <SheetDescription>
            Stay updated with the latest alerts and activities.
          </SheetDescription>
        </SheetHeader>

        <ScrollArea className="flex-1 w-full h-full">
          {isLoading ? (
            <div className="flex items-center justify-center h-full text-muted-foreground">
              Loading...
            </div>
          ) : notifications.length === 0 ? (
            <div className="flex flex-col items-center justify-center h-full text-center text-muted-foreground p-6">
              <Bell size={40} className="mb-4 opacity-20" />
              <p>You don't have any notifications right now.</p>
            </div>
          ) : (
            <div className="flex flex-col divide-y">
              {notifications.map((notification) => (
                <div 
                  key={notification.id} 
                  className={`p-4 flex gap-4 transition-colors hover:bg-muted/50 ${
                    notification.isRead ? 'opacity-70' : 'bg-primary/5'
                  }`}
                >
                  <div className="pt-1 flex-shrink-0">
                    <Button
                      variant="ghost"
                      size="icon-sm"
                      className="rounded-full"
                      onClick={() => {
                        if (!notification.isRead) {
                          markAsReadMutation.mutate(notification.id)
                        }
                      }}
                      disabled={notification.isRead || markAsReadMutation.isPending}
                      title={notification.isRead ? "Read" : "Mark as read"}
                    >
                      {notification.isRead ? (
                        <CheckCircle2 size={18} className="text-muted-foreground" />
                      ) : (
                        <Circle size={18} className="text-primary fill-primary/20" />
                      )}
                    </Button>
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex justify-between items-start gap-2 mb-1">
                      <p className={`text-sm font-medium ${!notification.isRead ? 'text-foreground' : 'text-muted-foreground'}`}>
                        {notification.title}
                      </p>
                      <span className="text-[10px] text-muted-foreground whitespace-nowrap">
                        {getTimeAgo(notification.createdAt)}
                      </span>
                    </div>
                    <p className="text-sm text-muted-foreground line-clamp-2">
                      {notification.message}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </ScrollArea>
      </SheetContent>
    </Sheet>
  )
}
