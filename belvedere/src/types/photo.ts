export interface PhotoBlur {
  id: string
  title?: string
  description?: string
  fileName: string
  blurHash: string
  width: number
  height: number
  mimeType: string
  createdAt: string
}

export interface PhotoThumbnail extends PhotoBlur {
  fileSize: number
  make?: string
  model?: string
  exposureTime?: number
  fNumber?: number
  iso?: number
  city?: string
  isLivePhoto: boolean
  thumbnailUrl: string
}
