export function generateSignedUrl(photoId: string) {
  return `/api/photos/${encodeURIComponent(photoId)}/signed-url`
}

