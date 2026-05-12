import type { PhotoBlur, PhotoThumbnail } from "@/features/albums/api"

export interface Album {
  id: string
  title: string
  description?: string
  coverPhotoId?: string
  isPublic: boolean
  createdAt: string
  photoCount: number
}
export interface AlbumExtended extends Album {
  photos: PhotoBlur[]
}

export interface AlbumWithThumbnails extends Album {
  photos: PhotoThumbnail[]
}
