import { useEffect, useState } from 'react';
import { HubConnection, HubConnectionBuilder, LogLevel, HttpTransportType } from '@microsoft/signalr';
import { authStore } from '@/store/auth.store';

export function useSignalR(hubUrl: string) {
  const [connection, setConnection] = useState<HubConnection | null>(null);

  useEffect(() => {
    const token = authStore.getState().accessToken || (typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null);
    
    if (!token) return;

    const baseUrl = (process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000').replace(/\/+$/, '');
    const safeHubUrl = hubUrl.startsWith('/') ? hubUrl : `/${hubUrl}`;
    const fullUrl = `${baseUrl}${safeHubUrl}`;
    
    console.log(`[SignalR] Attempting to connect to: ${fullUrl}`);

    const newConnection = new HubConnectionBuilder()
      .withUrl(fullUrl, {
        accessTokenFactory: () => token
      })
      .configureLogging(LogLevel.Information)
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, [hubUrl]);

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => console.log(`Connected to SignalR hub: ${hubUrl}`))
        .catch(e => console.log('Connection failed: ', e));
      
      return () => {
        connection.stop();
      };
    }
  }, [connection, hubUrl]);

  return connection;
}
