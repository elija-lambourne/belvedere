import z from "zod";

export const photoSignedUrlResponse = z.object({
  temporaryUrl: z.string(),
});

export type PhotoSignedUrlResponse = z.infer<typeof photoSignedUrlResponse>;

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

export type Photo = z.infer<typeof photoSchema>;

export const photoBlurSchema = photoSchema.extend({
  blurHash: z.string(),
});

export type PhotoBlur = z.infer<typeof photoBlurSchema>;

export const photoThumbnailSchema = photoSchema.extend({
  fileSize: z.number().positive(),
  make: z.string().nullable(),
  model: z.string().nullable(),
  exposureTime: z.number().positive().nullable(),
  fNumber: z.number().positive().nullable(),
  iso: z.number().positive().nullable,
  city: z.string().nullable(),
  isLivePhoto: z.boolean(),
  thumbnailUrl: z.string(),
})

export type PhotoThumbnail = z.infer<typeof photoThumbnailSchema>;

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
  iso: z.number().positive().nullable,
  city: z.string().nullable(),
  isLivePhoto: z.boolean(),
  reactions: z.record(z.string(), z.number()),
})

export type PhotoMetadata = z.infer<typeof photoMetadataSchema>;

export const createPhotoRequestSchema = z.object({
  file: z.instanceof(File),
  title: z.string().nullable(),
  description: z.string().nullable(),
})

export type CreatePhotoRequest = z.infer<typeof createPhotoRequestSchema>;

export const createPhotoResponse = z.object({
  photo: photoSchema,
  thumbnail: photoThumbnailSchema,
  metadata: photoMetadataSchema,
  blur: photoBlurSchema,
  temporaryUrl: z.string(),
});

export type CreatePhotoResponse = z.infer<typeof createPhotoResponse>;