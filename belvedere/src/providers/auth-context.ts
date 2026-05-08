import * as React from "react"

import type { UserProfile } from "@/features/auth/types"

type AuthContextValue = {
  user: UserProfile | null
  isLoading: boolean
  isAuthenticated: boolean
  refetchUser: () => Promise<unknown>
}

export const AuthContext = React.createContext<AuthContextValue | undefined>(undefined)

export type { AuthContextValue }


