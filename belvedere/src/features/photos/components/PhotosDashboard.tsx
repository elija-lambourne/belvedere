import { Button } from "@/components/ui/button"

import { PhotoViewer } from "./PhotoViewer"

export function PhotosDashboard() {
  return (
    <main className="space-y-6 p-6">
      <section className="space-y-2">
        <p className="text-sm text-muted-foreground">Photos</p>
        <h1 className="text-3xl font-semibold tracking-tight">Encrypted media vault</h1>
        <p className="max-w-2xl text-sm text-muted-foreground">
          Media should flow through the BFF, use signed URLs, and never rely on direct public paths.
        </p>
      </section>

      <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_320px]">
        <PhotoViewer photoId="placeholder-photo" alt="Sample vault media" />
        <aside className="space-y-3 rounded-lg border bg-card p-4 text-card-foreground shadow-sm">
          <h2 className="font-medium">Actions</h2>
          <p className="text-sm text-muted-foreground">Wire your upload and delete flows to authenticated API endpoints.</p>
          <Button variant="outline" className="w-full">
            Refresh vault
          </Button>
        </aside>
      </div>
    </main>
  )
}

