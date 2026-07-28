"use client"

import React, { useState } from "react"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import Link from "next/link"
import { Plus, Trash2, Bell, Search, ChevronLeft, ChevronRight } from "lucide-react"
import { useDebounce } from "@/hooks/use-debounce"

import { notificationService, type SystemNotificationDto } from "@/services/notification.service"
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Badge } from "@/components/ui/badge"
import { toast } from "sonner"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter, DialogClose } from "@/components/ui/dialog"

export default function AdminNotificationsPage() {
  const queryClient = useQueryClient()
  const [pageIndex, setPageIndex] = useState(1)
  const pageSize = 10

  const [deleteDialog, setDeleteDialog] = useState<{ open: boolean, item: SystemNotificationDto | null }>({
    open: false,
    item: null
  })

  const [searchTerm, setSearchTerm] = useState("")
  const debouncedSearchTerm = useDebounce(searchTerm, 500)

  const { data, isLoading } = useQuery({
    queryKey: ['system-notifications', pageIndex, pageSize, debouncedSearchTerm],
    queryFn: () => notificationService.getSystemNotifications(pageIndex, pageSize, debouncedSearchTerm)
  })

  const deleteMutation = useMutation({
    mutationFn: (item: SystemNotificationDto) => notificationService.deleteSystemNotification(item.title, item.message),
    onSuccess: () => {
      toast.success("System notification deleted successfully")
      queryClient.invalidateQueries({ queryKey: ['system-notifications'] })
      setDeleteDialog({ open: false, item: null })
    },
    onError: () => {
      toast.error("Failed to delete notification")
    }
  })

  const notifications = data?.data || []
  const meta = data?.meta

  const formatDate = (dateStr: string) => {
    const d = new Date(dateStr)
    return d.toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric", hour: "2-digit", minute: "2-digit" })
  }

  return (
    <div className="w-full pb-8 space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">System Notifications (Admin)</h1>
          <p className="text-muted-foreground mt-1">
            Manage announcements and notifications sent to candidates and recruiters system-wide.
          </p>
        </div>
        <Link href="/admin/notifications/create">
          <Button className="gap-2">
            <Plus size={16} />
            Create Notification
          </Button>
        </Link>
      </div>

      <div className="flex items-center gap-2 max-w-sm">
        <div className="relative flex-1">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search by title..."
            className="pl-9"
            value={searchTerm}
            onChange={(e) => {
              setSearchTerm(e.target.value)
              setPageIndex(1) // reset to first page on search
            }}
          />
        </div>
      </div>

      <Card>
        <CardHeader className="pb-4">
          <CardTitle className="flex items-center gap-2">
            <Bell className="w-5 h-5" />
            Sent Notifications
          </CardTitle>
          <CardDescription>
            A history of system-wide notifications that have been broadcasted.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="rounded-md border overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow className="bg-muted/50">
                  <TableHead className="w-[200px]">Title</TableHead>
                  <TableHead>Message</TableHead>
                  <TableHead className="w-[120px]">Status</TableHead>
                  <TableHead className="w-[150px]">Sent Date</TableHead>
                  <TableHead className="w-[80px] text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {isLoading ? (
                  <TableRow>
                    <TableCell colSpan={5} className="text-center py-8 text-muted-foreground">
                      Loading notifications...
                    </TableCell>
                  </TableRow>
                ) : notifications.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} className="text-center py-12 text-muted-foreground">
                      No system notifications have been sent yet.
                    </TableCell>
                  </TableRow>
                ) : (
                  notifications.map((notification, idx) => (
                    <TableRow key={idx}>
                      <TableCell className="font-medium truncate max-w-[200px]" title={notification.title}>
                        {notification.title}
                      </TableCell>
                      <TableCell className="truncate max-w-[400px]" title={notification.message}>
                        {notification.message}
                      </TableCell>
                      <TableCell>
                        {notification.isHidden ? (
                          <Badge variant="secondary" className="text-muted-foreground">Hidden</Badge>
                        ) : (
                          <Badge variant="default" className="bg-green-500 hover:bg-green-600">Active</Badge>
                        )}
                      </TableCell>
                      <TableCell className="text-muted-foreground whitespace-nowrap">
                        {formatDate(notification.createdAt)}
                      </TableCell>
                      <TableCell className="text-right">
                        <Button
                          variant="ghost"
                          size="icon-sm"
                          className="text-destructive hover:text-destructive hover:bg-destructive/10"
                          onClick={() => setDeleteDialog({ open: true, item: notification })}
                          disabled={notification.isHidden}
                        >
                          <Trash2 size={16} />
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>

          {/* Pagination Controls */}
          {meta && meta.totalPages > 1 && (
            <div className="flex items-center justify-between mt-4">
              <div className="text-sm text-muted-foreground">
                Showing page {meta.currentPage} of {meta.totalPages} ({meta.totalItems} total)
              </div>
              <div className="flex items-center space-x-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPageIndex((p) => Math.max(1, p - 1))}
                  disabled={pageIndex === 1 || isLoading}
                >
                  <ChevronLeft className="h-4 w-4" />
                  Previous
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPageIndex((p) => Math.min(meta.totalPages, p + 1))}
                  disabled={pageIndex >= meta.totalPages || isLoading}
                >
                  Next
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog open={deleteDialog.open} onOpenChange={(open) => setDeleteDialog(prev => ({ ...prev, open }))}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete System Notification</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete this notification? It will be removed from all users' inboxes immediately. This action cannot be undone.
            </DialogDescription>
          </DialogHeader>

          <div className="bg-muted/50 p-4 rounded-md my-2 border border-border">
            <h4 className="font-medium text-sm mb-1">{deleteDialog.item?.title}</h4>
            <p className="text-sm text-muted-foreground line-clamp-3">{deleteDialog.item?.message}</p>
          </div>

          <DialogFooter className="mt-4">
            <DialogClose render={<Button variant="outline" />}>
              Cancel
            </DialogClose>
            <Button
              variant="destructive"
              onClick={() => deleteDialog.item && deleteMutation.mutate(deleteDialog.item)}
              disabled={deleteMutation.isPending}
            >
              {deleteMutation.isPending ? "Deleting..." : "Delete Notification"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
