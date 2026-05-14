import z from "zod"

export const photoSchema = z.object({
  id: z.string(),
  title: z.string().nullable(),
  fileName: z.string(),
  description: z.string().nullable(),
  mimeType: z.string(),
  width: z.number(),
  height: z.number(),
  fileSize: z.number(),
  createdAt: z.iso.datetime(),
})

export type Photo = z.infer<typeof photoSchema>

export const photoThumbnailSchema = photoSchema.extend({
  fileSize: z.number().positive(),
  make: z.string().nullable(),
  model: z.string().nullable(),
  exposureTime: z.number().positive().nullable(),
  fNumber: z.number().positive().nullable(),
  focalLength: z.number().positive().nullable(),
  iso: z.number().positive().nullable(),
  city: z.string().nullable(),
  isLivePhoto: z.boolean(),
  thumbnailUrl: z.string(),
})

export type PhotoThumbnail = z.infer<typeof photoThumbnailSchema>

export const photoMetadataSchema = photoSchema.extend({
  latitude: z.number().nullable(),
  longitude: z.number().nullable(),
  countryCode: z.string().nullable(),
  capturedAt: z.iso.datetime(),
  fileSize: z.number().positive(),
  make: z.string().nullable(),
  model: z.string().nullable(),
  exposureTime: z.number().positive().nullable(),
  fNumber: z.number().positive().nullable(),
  focalLength: z.number().positive().nullable(),
  iso: z.number().positive().nullable(),
  city: z.string().nullable(),
  isLivePhoto: z.boolean(),
  reactions: z.record(z.string(), z.number()),
})

export type PhotoMetadata = z.infer<typeof photoMetadataSchema>
