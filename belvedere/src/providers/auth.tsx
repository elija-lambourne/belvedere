import * as React from "react"
import { useQuery } from "@tanstack/react-query"
import { useLocation, useNavigate } from "react-router-dom"

import { fetchCurrentUser } from "@/features/auth"
import { isUnauthorizedError } from "@/lib/axios"
import { AuthContext } from "./auth-context"
import type { AuthContextValue } from "./auth-context"

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const navigate = useNavigate()
  const location = useLocation()
  const userQuery = useQuery({
    queryKey: ["auth", "current-user"],
    queryFn: fetchCurrentUser,
    retry: false,
  })

  React.useEffect(() => {
    if (!userQuery.error || !isUnauthorizedError(userQuery.error)) {
      return
    }

    if (location.pathname !== "/login") {
      navigate("/login", { replace: true, state: { from: location.pathname } })
    }
  }, [location.pathname, navigate, userQuery.error])

  const value = React.useMemo<AuthContextValue>(
    () => ({
      user: userQuery.data ?? null,
      isLoading: userQuery.isLoading,
      isAuthenticated: Boolean(userQuery.data),
      refetchUser: async () => userQuery.refetch(),
    }),
    [userQuery]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}



