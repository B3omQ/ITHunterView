'use client';

import React, { useState } from 'react';
import { Building2, Briefcase } from 'lucide-react';

interface CompanyLogoProps {
  src?: string | null;
  alt?: string;
  fallbackType?: 'building' | 'briefcase';
  fallbackIconClassName?: string;
  imageClassName?: string;
}

export function CompanyLogo({ 
  src, 
  alt, 
  fallbackType = 'briefcase', 
  fallbackIconClassName = 'text-slate-400 w-4 h-4',
  imageClassName = 'object-contain w-full h-full'
}: CompanyLogoProps) {
  const [imgError, setImgError] = useState(false);

  const Icon = fallbackType === 'building' ? Building2 : Briefcase;

  if (src && !imgError) {
    return (
      <img 
        src={src} 
        alt={alt || 'Company Logo'} 
        className={imageClassName} 
        onError={() => setImgError(true)}
      />
    );
  }

  return <Icon className={fallbackIconClassName} />;
}
