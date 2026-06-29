"use client"

import React, { useState, useEffect, useRef } from 'react';
import dynamic from 'next/dynamic';
import { Input } from "@/components/ui/input"
import { MapPin, Search, Loader2 } from "lucide-react"
import { VIETNAM_PROVINCES } from "@/lib/job-constants"

// Dynamically import MapChild with ssr disabled to fix Leaflet window is not defined error
const MapChild = dynamic(() => import('./MapChild'), {
  ssr: false,
  loading: () => <div className="h-[300px] w-full border border-zinc-200 dark:border-zinc-800 rounded-md bg-zinc-50 dark:bg-zinc-900 animate-pulse flex items-center justify-center text-sm text-zinc-500">Loading Map...</div>
});

export interface LocationData {
  provinceCode: string;
  detailedLocation: string;
  latitude: number | null;
  longitude: number | null;
}

interface LocationPickerProps {
  value: LocationData;
  onChange: (value: LocationData) => void;
}

// Default to Ho Chi Minh City if no location
const defaultCenter = {
  lat: 10.762622,
  lng: 106.660172
};

export function LocationPicker({ value, onChange }: LocationPickerProps) {
  const [inputValue, setInputValue] = useState(value.detailedLocation || "");
  const [suggestions, setSuggestions] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const [showDropdown, setShowDropdown] = useState(false);
  
  const [mapCenter, setMapCenter] = useState(
    value.latitude && value.longitude 
      ? { lat: value.latitude, lng: value.longitude } 
      : defaultCenter
  );

  const debounceTimerRef = useRef<NodeJS.Timeout | null>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setShowDropdown(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const searchNominatim = async (query: string) => {
    if (!query.trim()) {
      setSuggestions([]);
      return;
    }
    
    setLoading(true);
    try {
      const res = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&countrycodes=vn&addressdetails=1`);
      const data = await res.json();
      setSuggestions(data);
      setShowDropdown(true);
    } catch (error) {
      console.error("Error fetching location data: ", error);
    } finally {
      setLoading(false);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    setInputValue(val);
    onChange({ ...value, detailedLocation: val });
    
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
    }
    
    debounceTimerRef.current = setTimeout(() => {
      searchNominatim(val);
    }, 400);
  };

  const extractProvinceCode = (address: any) => {
    if (!address) return "Other";
    
    // Nominatim returns state or city
    const provinceName = address.state || address.city || address.province || "";
    if (!provinceName) return "Other";

    const normalized = provinceName.toLowerCase().replace("tỉnh ", "").replace("thành phố ", "");
    const match = VIETNAM_PROVINCES.find(p => p.toLowerCase().includes(normalized) || normalized.includes(p.toLowerCase()));
    
    return match || "Other";
  };

  const handleSelectSuggestion = (suggestion: any) => {
    const lat = parseFloat(suggestion.lat);
    const lon = parseFloat(suggestion.lon);
    const addressName = suggestion.display_name;
    const provinceCode = extractProvinceCode(suggestion.address);
    
    setInputValue(addressName);
    setShowDropdown(false);
    setMapCenter({ lat, lng: lon });
    
    onChange({
      provinceCode,
      detailedLocation: addressName,
      latitude: lat,
      longitude: lon
    });
  };

  const handleMarkerDragEnd = async (lat: number, lng: number) => {
    setMapCenter({ lat, lng });
    
    try {
      const res = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&addressdetails=1`);
      const data = await res.json();
      
      if (data && data.display_name) {
        const addressName = data.display_name;
        const provinceCode = extractProvinceCode(data.address);
        
        setInputValue(addressName);
        
        onChange({
          provinceCode,
          detailedLocation: addressName,
          latitude: lat,
          longitude: lng
        });
      }
    } catch (error) {
      console.error("Error reverse geocoding: ", error);
      // Fallback update lat/lng even if reverse geocode fails
      onChange({ ...value, latitude: lat, longitude: lng });
    }
  };

  return (
    <div className="space-y-3 w-full">
      <div className="relative" ref={dropdownRef}>
        <div className="relative flex items-center">
          <Search className="absolute left-3 h-4 w-4 text-zinc-400" />
          <Input
            value={inputValue}
            onChange={handleInputChange}
            placeholder="Search for an address in Vietnam..."
            className="pl-9 pr-9 w-full focus-visible:ring-blue-500"
            onFocus={() => {
              if (suggestions.length > 0) setShowDropdown(true);
            }}
          />
          {loading && (
            <Loader2 className="absolute right-3 h-4 w-4 text-zinc-400 animate-spin" />
          )}
        </div>
        
        {/* Dropdown Suggestions */}
        {showDropdown && suggestions.length > 0 && (
          <ul className="absolute z-[1000] w-full mt-1 bg-popover text-popover-foreground border border-input rounded-md shadow-md max-h-60 overflow-auto">
            {suggestions.map((item) => (
              <li
                key={item.place_id}
                onClick={() => handleSelectSuggestion(item)}
                className="px-4 py-2 hover:bg-accent hover:text-accent-foreground cursor-pointer flex items-center gap-2 text-sm transition-colors"
              >
                <MapPin className="h-4 w-4 text-zinc-400 shrink-0" />
                <span className="truncate">{item.display_name}</span>
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="relative">
        <MapChild position={mapCenter} onPositionChange={handleMarkerDragEnd} />
        <div className="absolute bottom-3 left-3 right-3 bg-background/90 backdrop-blur-sm p-2 rounded-md shadow-sm border border-input text-xs text-muted-foreground flex items-center gap-2 z-[400] pointer-events-none">
          <MapPin className="h-4 w-4 text-blue-500 shrink-0" />
          <span className="truncate">
            {value.detailedLocation || "Drag the marker or search to set a location"}
          </span>
        </div>
      </div>
    </div>
  );
}
