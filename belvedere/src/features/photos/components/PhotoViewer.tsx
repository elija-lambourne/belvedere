type PhotoViewerProps = {
  photoId: string
  alt: string
}

export function PhotoViewer({ photoId, alt }: PhotoViewerProps) {

  return (
    <figure className="space-y-2 rounded-lg border bg-card p-4 text-card-foreground shadow-sm">
      <div className="flex aspect-square items-center justify-center rounded-md border border-dashed bg-muted/40 text-center text-sm text-muted-foreground">
        <div>
          <p className="font-medium text-foreground">WebGL renderer placeholder</p>
          <p className="max-w-sm">
            Replace this region with the future <code>webgl-image</code> pipeline.
          </p>
        </div>
      </div>
      <figcaption className="space-y-1 text-xs text-muted-foreground">
        <p>{alt}</p>
        <p>Signed URL must come from the API {photoId}</p>
      </figcaption>
    </figure>
  )
}

