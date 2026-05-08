import { Button } from "@/components/ui/button"
import { useMemo } from "react"
import { CarbonIsoFilled } from "@/components/ui/icons/CarbonIsoFilled.tsx"
import { BoxiconsShutter } from "@/components/ui/icons/Shutter.tsx"
import { MaterialSymbolsShutterSpeedRounded } from "@/components/ui/icons/ShutterSpeed.tsx"
import { RiCameraLensAiFill } from "@/components/ui/icons/Aperature.tsx"
import { SolarCameraLinear } from "@/components/ui/icons/Camera.tsx"
import { BoxiconsCalendarEvent } from "@/components/ui/icons/Calendar.tsx"
import { BoxiconsLocationPin } from "@/components/ui/icons/Pin.tsx"

type Photo = {
  id: string
  src: string
  title: string
  date: string
  city?: string
  mm?: number
  aperture?: string
  shutterSpeed?: string
  iso?: string
  device?: string
}

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
  const album = useMemo(
    () => ({
      title: "2024 Brussels",
      subtitle: "Why is this here?",
      date: "Jan 2024",
      createdAt: "2024-01-16",
    }),
    []
  )

  const photos: Photo[] = [
    {
      id: "1",
      src: "https://images.unsplash.com/photo-1519681393784-d120267933ba?w=1600&q=80&auto=format&fit=crop",
      title: "IMG 1079",
      date: "2024-01-16",
      city: "Strasbourg",
      mm: 70,
      aperture: "f/1.8",
      shutterSpeed: "1/20",
      iso: "1000",
      device: "Apple iPhone 14 Pro Max",
    },
    {
      id: "2",
      src: "https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?w=1600&q=80&auto=format&fit=crop",
      title: "IMG 1080",
      date: "2024-01-16",
      city: "Shenzhen",
      mm: 35,
      aperture: "f/1.8",
      shutterSpeed: "1/160",
      iso: "200",
      device: "Apple iPhone 14 Pro Max",
    },
    {
      id: "3",
      src: "https://images.unsplash.com/photo-1500534314209-a25ddb2bd429?w=1600&q=80&auto=format&fit=crop",
      title: "IMG 1081",
      date: "2024-01-16",
      city: "Linz",
      mm: 50,
      aperture: "f/2.2",
      shutterSpeed: "1/125",
      iso: "320",
      device: "Apple iPhone 14 Pro Max",
    },
    {
      id: "4",
      src: "https://images.unsplash.com/photo-1470770903676-69b98201ea1c?w=1600&q=80&auto=format&fit=crop",
      title: "IMG 1082",
      date: "2024-01-16",
      city: "Vienna",
      mm: 24,
      aperture: "f/4",
      shutterSpeed: "1/200",
      iso: "100",
      device: "Apple iPhone 14 Pro Max",
    },
    {
      id: "5",
      src: "https://images.unsplash.com/photo-1482192596544-9eb780fc7f66?w=1600&q=80&auto=format&fit=crop",
      title: "IMG 1083",
      date: "2024-01-16",
      city: "St. Pölten",
      mm: 85,
      aperture: "f/1.4",
      shutterSpeed: "1/60",
      iso: "400",
      device: "Apple iPhone 14 Pro Max",
    },
    {
      id: "5",
      src: "https://images.unsplash.com/photo-1482192596544-9eb780fc7f66?w=1600&q=80&auto=format&fit=crop",
      title: "IMG 1083",
      date: "2024-01-16",
      city: "St. Pölten",
      mm: 85,
      aperture: "f/1.4",
      shutterSpeed: "1/60",
      iso: "400",
      device: "Apple iPhone 14 Pro Max",
    },
    {
      id: "5",
      src: "https://images.unsplash.com/photo-1482192596544-9eb780fc7f66?w=1600&q=80&auto=format&fit=crop",
      title: "IMG 1083",
      date: "2024-01-16",
      city: "St. Pölten",
      mm: 85,
      aperture: "f/1.4",
      shutterSpeed: "1/60",
      iso: "400",
      device: "Apple iPhone 14 Pro Max",
    },
    {
      id: "5",
      src: "https://images.unsplash.com/photo-1482192596544-9eb780fc7f66?w=1600&q=80&auto=format&fit=crop",
      title: "IMG 1083",
      date: "2024-01-16",
      city: "St. Pölten",
      mm: 85,
      aperture: "f/1.4",
      shutterSpeed: "1/60",
      iso: "400",
      device: "Apple iPhone 14 Pro Max",
    },
    {
      id: "5",
      src: "https://images.unsplash.com/photo-1482192596544-9eb780fc7f66?w=1600&q=80&auto=format&fit=crop",
      title: "IMG 1083",
      date: "2024-01-16",
      city: "St. Pölten",
      mm: 85,
      aperture: "f/1.4",
      shutterSpeed: "1/60",
      iso: "400",
      device: "Apple iPhone 14 Pro Max",
    },
  ]

  return (
    <main className="space-y-6 p-6">
      <section className="space-y-2">
        <p className="text-sm text-muted-foreground">Albums</p>
        <h1 className="text-4xl font-semibold tracking-tight">{album.title}</h1>
        <p className="max-w-2xl text-sm text-muted-foreground">
          {album.subtitle}
        </p>

        <div className="mt-4 flex items-center gap-4 text-sm text-muted-foreground">
          <span className="inline-flex items-center gap-2">
            📷 {photos.length} Photos
          </span>
          <span className="inline-flex items-center gap-2">
            📅 {album.date}
          </span>
          <span className="inline-flex items-center gap-2">
            ⏱ {monthsAgo(album.createdAt)}
          </span>
        </div>
      </section>

      <div className="relative">
        <div className="-mx-6 mt-6 overflow-hidden">
          <div className="columns-1 gap-4 px-6 py-8 sm:columns-2 lg:columns-3 xl:columns-4">
            {photos.map((p, idx) => (
              <figure
                key={p.id}
                className="group relative mb-4 inline-block w-full overflow-hidden rounded-sm break-inside-avoid"
              >
                <img
                  src={p.src}
                  alt={p.title}
                  className={`block w-full object-cover transition-transform duration-500 group-hover:scale-105 ${
                    idx % 5 === 0
                      ? "aspect-4/5"
                      : idx % 5 === 1
                        ? "aspect-3/4"
                        : idx % 5 === 2
                          ? "aspect-square"
                          : idx % 5 === 3
                            ? "aspect-5/4"
                            : "aspect-4/3"
                  }`}
                />

                <figcaption className="absolute inset-x-0 bottom-0 z-10 w-full bg-linear-to-t from-black/70 to-transparent px-4 py-3 text-white opacity-0 transition-opacity duration-200 group-hover:opacity-100">
                  <div className="mb-1">
                    <div className="text-sm font-semibold">{p.title}</div>
                    <div className="flex items-center gap-1 text-xs text-muted-foreground/80">
                      <BoxiconsCalendarEvent className="h-3.5 w-3.5" />
                      <span>{p.date}</span>
                      <span>·</span>
                      <BoxiconsLocationPin className="h-3.5 w-3.5" />
                      <span>{p.city}</span>
                    </div>
                  </div>

                  <div className="flex  gap-2 text-xs text-white/90">
                    <div className="flex items-center gap-1 text-muted-foreground/90">
                      <BoxiconsShutter className="h-3 w-3" />
                      <span className="font-medium">{p.mm}</span> mm
                    </div>
                    <div className="flex items-center gap-1 text-muted-foreground/90">
                      <RiCameraLensAiFill className="h-3 w-3" />
                      <span className="font-medium">{p.aperture}</span>
                    </div>
                    <div className="flex items-center gap-1 text-muted-foreground/90">
                      <MaterialSymbolsShutterSpeedRounded className="h-3 w-3" />
                      <span className="font-medium">{p.shutterSpeed}</span>
                    </div>
                    <div className="flex items-center gap-1 text-muted-foreground/90">
                      <CarbonIsoFilled className="h-3 w-3" />
                      <span className="font-medium">{p.iso}</span>
                    </div>
                  </div>

                  <div className="mt-3 inline-flex items-center gap-1 text-xs text-muted-foreground/90">
                    <SolarCameraLinear className="h-3.5 w-3.5" />
                    <span>{p.device}</span>
                  </div>
                </figcaption>
              </figure>
            ))}
          </div>
        </div>
      </div>

      <div className="mt-4">
        <Button variant="outline">Sync albums</Button>
      </div>
    </main>
  )
}
