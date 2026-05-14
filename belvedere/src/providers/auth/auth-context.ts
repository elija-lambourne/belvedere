import * as React from "react"
import type { UserProfile } from "@/providers/auth/types/UserProfile.ts"


type AuthContextValue = {
  user: UserProfile | null
  isLoading: boolean
  isAuthenticated: boolean
  refetchUser: () => Promise<unknown>
}

export const AuthContext = React.createContext<AuthContextValue | undefined>(undefined)

export type { AuthContextValue }


