import { Button } from "@/components/ui/button"

export function LoginPage() {
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
        <Button className="w-full" asChild>
          <a href="/api/auth/login">Continue to login</a>
        </Button>
      </section>
    </main>
  )
}

