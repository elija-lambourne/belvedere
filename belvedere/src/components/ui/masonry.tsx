import { BoxiconsShutter } from "@/components/ui/icons/Shutter.tsx"
import { RiCameraLensAiFill } from "@/components/ui/icons/Aperature.tsx"
import { MaterialSymbolsShutterSpeedRounded } from "@/components/ui/icons/ShutterSpeed.tsx"
import { SolarCameraLinear } from "@/components/ui/icons/Camera.tsx"
import { CarbonIsoFilled } from "@/components/ui/icons/CarbonIsoFilled.tsx"
import { BoxiconsCalendarEvent } from "@/components/ui/icons/Calendar.tsx"
import { BoxiconsLocationPin } from "@/components/ui/icons/Pin.tsx"
import type { PhotoThumbnail } from "@/types"

export interface MasonryParams {
  photos: PhotoThumbnail[];
}

export function Masonry(params: MasonryParams){
  return (
    <div className="relative">
      <div className="-mx-6 mt-6 overflow-hidden">
        <div className="columns-1 gap-4 px-6 py-8 sm:columns-2 lg:columns-3 xl:columns-4">
          {params.photos.map((p, idx) => (
            <figure
              key={p.id}
              className="group relative mb-4 inline-block w-full break-inside-avoid overflow-hidden rounded-sm"
            >
              <img
                src={p.thumbnailUrl}
                alt={p.title ?? p.fileName}
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
                    <span>{p.createdAt}</span>
                    <span>·</span>
                    <BoxiconsLocationPin className="h-3.5 w-3.5" />
                    <span>{p.city}</span>
                  </div>
                </div>

                <div className="flex gap-2 text-xs text-white/90">
                  <div className="flex items-center gap-1 text-muted-foreground/90">
                    <BoxiconsShutter className="h-3 w-3" />
                    <span className="font-medium">{p.focalLength}</span> mm
                  </div>
                  <div className="flex items-center gap-1 text-muted-foreground/90">
                    <RiCameraLensAiFill className="h-3 w-3" />
                    <span className="font-medium">{p.fNumber}</span>
                  </div>
                  <div className="flex items-center gap-1 text-muted-foreground/90">
                    <MaterialSymbolsShutterSpeedRounded className="h-3 w-3" />
                    <span className="font-medium">{p.exposureTime}</span>
                  </div>
                  <div className="flex items-center gap-1 text-muted-foreground/90">
                    <CarbonIsoFilled className="h-3 w-3" />
                    <span className="font-medium">{p.iso}</span>
                  </div>
                </div>

                <div className="mt-3 inline-flex items-center gap-1 text-xs text-muted-foreground/90">
                  <SolarCameraLinear className="h-3.5 w-3.5" />
                  <span>
                    {p.make} {p.model}
                  </span>
                </div>
              </figcaption>
            </figure>
          ))}
        </div>
      </div>
    </div>
  )
}