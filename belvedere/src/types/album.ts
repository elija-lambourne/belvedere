import { z } from "zod"

import { photoThumbnailSchema } from "./photo"

export const albumSchema = z.object({
  id: z.string(),
  title: z.string(),
  description: z.string().nullable(),
  coverPhotoId: z.string().nullable(),
  isPublic: z.boolean(),
  createdAt: z.string(),
  photoCount: z.number(),
});

export const albumWithThumbnailsSchema = albumSchema.extend({
  photos: z.array(photoThumbnailSchema),
});

export const albumListSchema = z.array(albumSchema)

export type Album = z.infer<typeof albumSchema>
export type AlbumWithThumbnails = z.infer<typeof albumWithThumbnailsSchema>;
