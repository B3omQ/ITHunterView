'use client';

import React, { useState, useEffect } from 'react';
import Image from 'next/image';
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
  const [isLoaded, setIsLoaded] = useState(false);

  useEffect(() => {
    setImgError(false);
    setIsLoaded(false);
  }, [src]);

  const Icon = fallbackType === 'building' ? Building2 : Briefcase;

  return (
    <div className="relative w-full h-full flex items-center justify-center">
      {/* Fallback Icon - Always rendered beneath */}
      <Icon className={fallbackIconClassName} />

      {/* Real Logo - Fades in on load */}
      {src && !imgError && (
        <Image 
          src={src} 
          alt={alt || 'Company Logo'} 
          className={`${imageClassName} absolute inset-0 transition-opacity duration-300 ${isLoaded ? 'opacity-100' : 'opacity-0'}`} 
          fill
          sizes="(max-width: 768px) 100vw, 100px"
          onLoad={() => setIsLoaded(true)}
          onError={() => setImgError(true)}
        />
      )}
    </div>
  );
}

