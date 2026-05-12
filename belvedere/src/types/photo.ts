import { z } from "zod"

export const photoBlurSchema = z.object({
  id: z.string(),
  title: z.string().optional(),
  description: z.string().optional(),
  fileName: z.string(),
  blurHash: z.string(),
  width: z.number(),
  height: z.number(),
  mimeType: z.string(),
  createdAt: z.string(),
})

export const photoThumbnailSchema = photoBlurSchema.extend({
  fileSize: z.number(),
  make: z.string().optional(),
  model: z.string().optional(),
  exposureTime: z.number().optional(),
  fNumber: z.number().optional(),
  iso: z.number().optional(),
  city: z.string().optional(),
  isLivePhoto: z.boolean(),
  thumbnailUrl: z.string(),
})

export type PhotoBlur = z.infer<typeof photoBlurSchema>
export type PhotoThumbnail = z.infer<typeof photoThumbnailSchema>
