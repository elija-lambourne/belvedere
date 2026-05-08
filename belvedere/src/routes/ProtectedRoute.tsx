import { Navigate, useLocation } from "react-router-dom"

import { useAuth } from "@/providers/useAuth"
import * as React from "react"

export function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const location = useLocation()
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return (
      <div className="flex min-h-svh items-center justify-center p-6 text-sm text-muted-foreground">
        Loading secure session...
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate replace to="/login" state={{ from: location.pathname }} />
  }

  return children
}


