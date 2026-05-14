import { Button } from "@/components/ui/button"
import type { AlbumWithThumbnails } from "@/types"
import { Masonry } from "@/components/ui/masonry.tsx"

function monthsAgo(dateStr: string) {
  try {
    const then = new Date(dateStr)
    const now = new Date()
    const months =
      (now.getFullYear() - then.getFullYear()) * 12 +
      (now.getMonth() - then.getMonth())
    if (months <= 0) return "Created this month"
    if (months === 1) return "Created 1 month ago"
    return `Created ${months} months ago`
  } catch {
    return "Created some time ago"
  }
}

export function AlbumsPage() {
  const album: AlbumWithThumbnails = {
    id: "",
    title: "Vienna trip",
    description: "Vienna is a lovely city in Austria",
    coverPhotoId: "",
    isPublic: true,
    createdAt: "01-01-2026",
    photoCount: 0,
    photos: []
  };


  return (
    <main className="space-y-6 p-6">
      <section className="space-y-2">
        <p className="text-sm text-muted-foreground">Albums</p>
        <h1 className="text-4xl font-semibold tracking-tight">{album.title}</h1>
        <p className="max-w-2xl text-sm text-muted-foreground">
          {album.description}
        </p>

        <div className="mt-4 flex items-center gap-4 text-sm text-muted-foreground">
          <span className="inline-flex items-center gap-2">
            📷 {album.photoCount} Photos
          </span>
          <span className="inline-flex items-center gap-2">
            📅 {album.createdAt} ({monthsAgo(album.createdAt)})
          </span>
        </div>
      </section>
      <Masonry photos={album.photos} />
      <div className="mt-4">
        <Button variant="outline">Sync albums</Button>
      </div>
    </main>
  )
}
