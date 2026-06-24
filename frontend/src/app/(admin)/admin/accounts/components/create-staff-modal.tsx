import React, { useState } from 'react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Loader2, XCircle } from 'lucide-react';
import { useCreateStaff } from '@/hooks/useUserGovernance';

interface CreateStaffModalProps {
  children: React.ReactElement;
  onSuccess?: (message: string) => void;
}

export function CreateStaffModal({ children, onSuccess }: CreateStaffModalProps) {
  const [open, setOpen] = useState(false);
  const [staffEmail, setStaffEmail] = useState('');
  const [staffPassword, setStaffPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');

  const createStaffMutation = useCreateStaff();

  const handleOpenChange = (isOpen: boolean) => {
    setOpen(isOpen);
    if (!isOpen) {
      // Reset form on close
      setStaffEmail('');
      setStaffPassword('');
      setShowPassword(false);
      setError('');
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!staffEmail.trim()) {
      setError('Please enter an email address.');
      return;
    }
    if (!staffPassword.trim()) {
      setError('Please enter an initial password.');
      return;
    }
    if (staffPassword.trim().length < 6) {
      setError('Password must be at least 6 characters.');
      return;
    }

    setError('');
    createStaffMutation.mutate(
      {
        email: staffEmail.trim(),
        password: staffPassword.trim(),
      },
      {
        onSuccess: (res) => {
          if (res.success) {
            onSuccess?.('Staff account created successfully!');
            handleOpenChange(false);
          } else {
            setError(res.message || 'Failed to create staff account.');
          }
        },
        onError: (err: any) => {
          setError(err.response?.data?.message || 'An error occurred while creating staff account.');
        },
      }
    );
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogTrigger render={children} />
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Create Staff Account</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 pt-2">
          <div className="space-y-1.5">
            <label className="text-xs font-bold text-muted-foreground uppercase tracking-wider">
              Email Address <span className="text-rose-500">*</span>
            </label>
            <input
              type="email"
              placeholder="Enter staff email address..."
              value={staffEmail}
              onChange={(e) => setStaffEmail(e.target.value)}
              required
              className="w-full py-2 px-3 border border-border rounded-xl bg-background text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 transition-all placeholder:text-muted-foreground"
            />
          </div>

          <div className="space-y-1.5 relative">
            <label className="text-xs font-bold text-muted-foreground uppercase tracking-wider">
              Initial Password <span className="text-rose-500">*</span>
            </label>
            <div className="relative">
              <input
                type={showPassword ? 'text' : 'password'}
                placeholder="Enter initial password (min 6 chars)..."
                value={staffPassword}
                onChange={(e) => setStaffPassword(e.target.value)}
                required
                className="w-full py-2 pl-3 pr-10 border border-border rounded-xl bg-background text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 transition-all placeholder:text-muted-foreground"
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-xs font-semibold text-primary hover:text-primary/80 transition-colors"
              >
                {showPassword ? 'Hide' : 'Show'}
              </button>
            </div>
          </div>

          {error && (
            <div className="p-3 bg-rose-500/10 border border-rose-500/25 rounded-xl text-rose-500 text-xs font-semibold flex items-center gap-1.5">
              <XCircle size={14} className="shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <div className="flex justify-end gap-3 pt-2">
            <button
              type="button"
              onClick={() => handleOpenChange(false)}
              className="px-4 py-2 border border-border hover:bg-muted text-foreground font-semibold text-sm rounded-xl transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={createStaffMutation.isPending}
              className="px-4 py-2 bg-primary hover:bg-primary/95 text-primary-foreground font-semibold text-sm rounded-xl shadow-xs transition-colors flex items-center gap-1.5 disabled:opacity-50"
            >
              {createStaffMutation.isPending && <Loader2 size={14} className="animate-spin" />}
              <span>Create</span>
            </button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
