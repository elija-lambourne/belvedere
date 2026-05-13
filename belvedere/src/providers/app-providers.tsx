import * as React from "react"

import { AuthProvider } from "./auth/auth"
import { QueryClientProvider } from "./query-client-provider"
import { ThemeProvider } from "./theme"
import { Toaster } from "@/components/ui/sonner.tsx"

export function AppProviders({ children }: { children: React.ReactNode }) {
  return (
    <QueryClientProvider>
      <ThemeProvider>
        <AuthProvider>
          {children}
        <Toaster />
        </AuthProvider>
      </ThemeProvider>
    </QueryClientProvider>
  )
}

