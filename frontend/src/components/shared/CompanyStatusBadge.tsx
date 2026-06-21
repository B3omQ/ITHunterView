import React from 'react';
import { CompanyStatus } from '@/types/company.types';

import { cn } from '@/lib/utils';

interface CompanyStatusBadgeProps {
  status: CompanyStatus;
  className?: string;
  rejectReason?: string;
}

export const CompanyStatusBadge: React.FC<CompanyStatusBadgeProps> = ({ status, className, rejectReason }) => {
  let label = '';
  let colorClass = '';

  switch (status) {
    case 'DRAFT':
      label = 'Draft Profile';
      colorClass = 'bg-gray-500 text-white';
      break;
    case 'PENDING':
      label = 'Pending verification';
      colorClass = 'bg-yellow-500 text-white';
      break;
    case 'VERIFIED':
      label = 'Verified';
      colorClass = 'bg-green-500 text-white';
      break;
    case 'REJECTED':
      label = 'Rejected';
      colorClass = 'bg-red-500 text-white';
      break;
    default:
      label = 'Unknown';
      colorClass = 'bg-gray-500 text-white';
  }

  // We are creating a simple custom badge if shadcn badge isn't available
  return (
    <div className={cn('flex flex-col items-start gap-1', className)}>
      <div className={cn('px-2.5 py-0.5 rounded-full text-xs font-semibold', colorClass)}>
        {label}
      </div>
      {status === 'REJECTED' && rejectReason && (
        <p className="text-xs text-red-500 mt-1 max-w-sm">Reason: {rejectReason}</p>
      )}
    </div>
  );
};
