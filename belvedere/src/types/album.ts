import { z } from "zod"

import type { PhotoBlur, PhotoThumbnail } from "./photo"
import { photoBlurSchema, photoThumbnailSchema } from "./photo"

export const albumSchema = z.object({
  id: z.string(),
  title: z.string(),
  description: z.string().optional(),
  coverPhotoId: z.string().optional(),
  isPublic: z.boolean(),
  createdAt: z.string(),
  photoCount: z.number(),
})

export const albumExtendedSchema = albumSchema.extend({
  photos: z.array(photoBlurSchema),
})

export const albumWithThumbnailsSchema = albumSchema.extend({
  photos: z.array(photoThumbnailSchema),
})

export const albumListSchema = z.array(albumSchema)

export type Album = z.infer<typeof albumSchema>
export type AlbumExtended = Album & { photos: PhotoBlur[] }
export type AlbumWithThumbnails = Album & { photos: PhotoThumbnail[] }
