import { useSyncExternalStore } from "react"

type SessionState = {
  isSidebarOpen: boolean
}

let state: SessionState = {
  isSidebarOpen: false,
}

const listeners = new Set<() => void>()

function setState(nextState: Partial<SessionState>) {
  state = { ...state, ...nextState }
  listeners.forEach((listener) => listener())
}

export function useSessionStore() {
  return useSyncExternalStore(
    (listener) => {
      listeners.add(listener)
      return () => listeners.delete(listener)
    },
    () => state,
    () => state
  )
}

export function toggleSidebar() {
  setState({ isSidebarOpen: !state.isSidebarOpen })
}

