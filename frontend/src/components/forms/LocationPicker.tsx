'use client';

import React, { useState, useEffect } from 'react';
import dynamic from 'next/dynamic';
import { Input } from '@/components/ui/input';
import { MapPin } from 'lucide-react';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';

// Dynamically import MapChild with ssr: false to prevent window is not defined
const MapChild = dynamic(() => import('./MapChild'), { ssr: false });

export interface LocationData {
  provinceCode: string;
  detailedLocation: string;
  latitude: number;
  longitude: number;
}

interface LocationPickerProps {
  value: LocationData;
  onChange: (value: LocationData) => void;
  disabled?: boolean;
}

const DEFAULT_LAT = 21.028511; // Hanoi
const DEFAULT_LNG = 105.804817;

const PROVINCE_MAP: Record<string, string> = {
  'hà nội': 'HN',
  'hồ chí minh': 'SG',
  'đà nẵng': 'DN',
  'hải phòng': 'HP',
  'cần thơ': 'CT',
};

const getProvinceCode = (address: any): string => {
  if (!address) return '';
  const provinceName = address.state || address.city || address.province || '';
  const normalized = provinceName.toLowerCase()
    .replace('thành phố ', '')
    .replace('tỉnh ', '')
    .trim();
  
  return PROVINCE_MAP[normalized] || provinceName;
};

export function LocationPicker({ value, onChange, disabled }: LocationPickerProps) {
  const [searchTerm, setSearchTerm] = useState(value.detailedLocation || '');
  const [suggestions, setSuggestions] = useState<any[]>([]);
  const [isOpen, setIsOpen] = useState(false);

  // Debounce search term
  const [debouncedSearch, setDebouncedSearch] = useState(searchTerm);

  useEffect(() => {
    // Sync external value to local state if changed from outside
    if (value.detailedLocation && value.detailedLocation !== searchTerm && !isOpen) {
      setSearchTerm(value.detailedLocation);
    }
  }, [value.detailedLocation]);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(searchTerm);
    }, 400);
    return () => clearTimeout(handler);
  }, [searchTerm]);

  useEffect(() => {
    if (debouncedSearch && debouncedSearch !== value.detailedLocation) {
      fetchSuggestions(debouncedSearch);
    } else {
      setSuggestions([]);
      setIsOpen(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch]);

  const fetchSuggestions = async (query: string) => {
    try {
      const res = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&countrycodes=vn&addressdetails=1`);
      const data = await res.json();
      setSuggestions(data || []);
      setIsOpen((data || []).length > 0);
    } catch (e) {
      console.error('Error fetching suggestions:', e);
    }
  };

  const handleSelectSuggestion = (item: any) => {
    const lat = parseFloat(item.lat);
    const lon = parseFloat(item.lon);
    const detailed = item.display_name;
    const pCode = getProvinceCode(item.address);

    setSearchTerm(detailed);
    setIsOpen(false);
    
    onChange({
      provinceCode: pCode,
      detailedLocation: detailed,
      latitude: lat,
      longitude: lon
    });
  };

  const handleMapChange = async (lat: number, lng: number) => {
    try {
      const res = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&addressdetails=1`);
      const data = await res.json();
      if (data && data.display_name) {
        const pCode = getProvinceCode(data.address);
        setSearchTerm(data.display_name);
        onChange({
          provinceCode: pCode,
          detailedLocation: data.display_name,
          latitude: lat,
          longitude: lng
        });
      }
    } catch (e) {
      console.error('Error reverse geocoding:', e);
      // Even if reverse geocoding fails, update lat/lng
      onChange({
        ...value,
        latitude: lat,
        longitude: lng
      });
    }
  };

  const lat = value.latitude || DEFAULT_LAT;
  const lng = value.longitude || DEFAULT_LNG;

  return (
    <div className="space-y-4">
      <Popover open={isOpen} onOpenChange={setIsOpen}>
        <PopoverTrigger asChild>
          <div className="relative">
            <Input
              placeholder="Search address..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              disabled={disabled}
              className="pr-10"
              autoComplete="off"
            />
            <MapPin className="absolute right-3 top-2.5 h-5 w-5 text-muted-foreground" />
          </div>
        </PopoverTrigger>
        <PopoverContent 
          className="w-[var(--radix-popover-trigger-width)] p-0 bg-popover text-popover-foreground border-input rounded-md shadow-md" 
          align="start"
          onOpenAutoFocus={(e) => e.preventDefault()}
        >
          <div className="max-h-60 overflow-y-auto">
            {suggestions.map((item, index) => (
              <div
                key={index}
                className="px-4 py-3 hover:bg-muted cursor-pointer text-sm border-b last:border-b-0 transition-colors"
                onClick={() => handleSelectSuggestion(item)}
              >
                {item.display_name}
              </div>
            ))}
          </div>
        </PopoverContent>
      </Popover>

      <div className="h-[300px] rounded-md overflow-hidden border">
        <MapChild 
          latitude={lat} 
          longitude={lng} 
          onChange={handleMapChange} 
        />
      </div>
    </div>
  );
}
