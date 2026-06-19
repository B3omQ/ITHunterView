import { create } from 'zustand';

interface UiState {
  sidebarOpen: boolean;
  globalNotifCount: number;
  setSidebarOpen: (open: boolean) => void;
  toggleSidebar: () => void;
  setGlobalNotifCount: (count: number) => void;
}

export const useUiStore = create<UiState>((set) => ({
  sidebarOpen: true,
  globalNotifCount: 0,
  setSidebarOpen: (open) => set({ sidebarOpen: open }),
  toggleSidebar: () => set((state) => ({ sidebarOpen: !state.sidebarOpen })),
  setGlobalNotifCount: (count) => set({ globalNotifCount: count }),
}));
