import * as React from "react"

import { AuthProvider } from "./auth/auth"
import { QueryClientProvider } from "./query-client-provider"
import { ThemeProvider } from "./theme"

export function AppProviders({ children }: { children: React.ReactNode }) {
  return (
    <QueryClientProvider>
      <ThemeProvider>
        <AuthProvider>{children}</AuthProvider>
      </ThemeProvider>
    </QueryClientProvider>
  )
}

