import { z } from "zod"

export const photoBlurSchema = z.object({
  id: z.string(),
  title: z.string().nullable(),
  description: z.string().nullable(),
  fileName: z.string(),
  blurHash: z.string(),
  width: z.number(),
  height: z.number(),
  mimeType: z.string(),
  createdAt: z.string(),
})

export const photoThumbnailSchema = photoBlurSchema.extend({
  fileSize: z.number(),
  make: z.string().nullable(),
  model: z.string().nullable(),
  exposureTime: z.number().nullable(),
  fNumber: z.number().nullable(),
  iso: z.number().nullable(),
  city: z.string().nullable(),
  isLivePhoto: z.boolean(),
  thumbnailUrl: z.string(),
})

export type PhotoBlur = z.infer<typeof photoBlurSchema>
export type PhotoThumbnail = z.infer<typeof photoThumbnailSchema>
