'use client';

import React, { useEffect, useRef } from 'react';
import { MapContainer, TileLayer, Marker, useMap } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png';
import markerIcon from 'leaflet/dist/images/marker-icon.png';
import markerShadow from 'leaflet/dist/images/marker-shadow.png';

// Fix Leaflet's default icon path issues in Next.js by importing local assets
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: typeof markerIcon2x === 'string' ? markerIcon2x : markerIcon2x.src,
  iconUrl: typeof markerIcon === 'string' ? markerIcon : markerIcon.src,
  shadowUrl: typeof markerShadow === 'string' ? markerShadow : markerShadow.src,
});

interface MapChildProps {
  latitude: number;
  longitude: number;
  onChange: (lat: number, lng: number) => void;
}

// Helper component to recenter map when lat/lng props change
function MapRecenter({ lat, lng }: { lat: number; lng: number }) {
  const map = useMap();
  useEffect(() => {
    map.flyTo([lat, lng], map.getZoom());
  }, [lat, lng, map]);
  return null;
}

export default function MapChild({ latitude, longitude, onChange }: MapChildProps) {
  const markerRef = useRef<L.Marker>(null);

  const eventHandlers = React.useMemo(
    () => ({
      dragend() {
        const marker = markerRef.current;
        if (marker != null) {
          const { lat, lng } = marker.getLatLng();
          onChange(lat, lng);
        }
      },
    }),
    [onChange]
  );

  return (
    <MapContainer 
      center={[latitude, longitude]} 
      zoom={15} 
      style={{ height: '100%', width: '100%', zIndex: 0 }}
    >
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      <Marker
        draggable={true}
        eventHandlers={eventHandlers}
        position={[latitude, longitude]}
        ref={markerRef}
      />
      <MapRecenter lat={latitude} lng={longitude} />
    </MapContainer>
  );
}
