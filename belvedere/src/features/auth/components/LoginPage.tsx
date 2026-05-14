import { useLocation } from "react-router-dom"

import { Button } from "@/components/ui/button"
import { login } from "@/providers/auth/authRequests.ts"

export function LoginPage() {
  const location = useLocation()
  const returnUrl = (location.state as { from?: string } | null | undefined)?.from ?? "/"

  return (
    <main className="flex min-h-svh items-center justify-center p-6">
      <section className="w-full max-w-md space-y-4 rounded-lg border bg-card p-6 text-card-foreground shadow-sm">
        <div className="space-y-2">
          <p className="text-sm text-muted-foreground">Private vault access</p>
          <h1 className="text-2xl font-semibold tracking-tight">Sign in through the BFF</h1>
          <p className="text-sm text-muted-foreground">
            Authentication should complete via HttpOnly cookies and never expose tokens to the browser.
          </p>
        </div>
        <Button className="w-full" onClick={() => login(returnUrl)}>
          Continue to login
        </Button>
      </section>
    </main>
  )
}

