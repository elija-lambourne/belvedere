import z from "zod";

export const photoSignedUrlResponse = z.object({
  temporaryUrl: z.string(),
});

export type PhotoSignedUrlResponse = z.infer<typeof photoSignedUrlResponse>;

export const photoSchema = z.object({
  id: z.string(),
  title: z.string().optional(),
  fileName: z.string(),
  description: z.string().optional,
  mimeType: z.string(),
  width: z.number(),
  height: z.number(),
  fileSize: z.number(),
  createdAt: z.iso.datetime(),
});

export type Photo = z.infer<typeof photoSchema>;

export const photoBlurSchema = photoSchema.extend({
  blurHash: z.string(),
});

export type PhotoBlur = z.infer<typeof photoBlurSchema>;

export const photoThumbnailSchema = photoSchema.extend({
  fileSize: z.number().positive(),
  make: z.string().optional(),
  model: z.string().optional(),
  exposureTime: z.number().positive().optional(),
  fNumber: z.number().positive().optional(),
  iso: z.number().positive().optional,
  city: z.string().optional(),
  isLivePhoto: z.boolean(),
  thumbnailUrl: z.string()
});

export type PhotoThumbnail = z.infer<typeof photoThumbnailSchema>;

export const photoMetadataSchema = photoSchema.extend({
  latitude: z.number().optional(),
  longitude: z.number().optional(),
  countryCode: z.string().optional(),
  capturedAt: z.iso.datetime(),
  fileSize: z.number().positive(),
  make: z.string().optional(),
  model: z.string().optional(),
  exposureTime: z.number().positive().optional(),
  fNumber: z.number().positive().optional(),
  iso: z.number().positive().optional,
  city: z.string().optional(),
  isLivePhoto: z.boolean(),
  reactions: z.record(z.string(), z.number())

});

export type PhotoMetadata = z.infer<typeof photoMetadataSchema>;

export const createPhotoRequestSchema = z.object({
  file: z.instanceof(File),
  title: z.string().optional(),
  description: z.string().optional(),
});

export type CreatePhotoRequest = z.infer<typeof createPhotoRequestSchema>;

export const createPhotoResponse = z.object({
  photo: photoSchema,
  thumbnail: photoThumbnailSchema,
  metadata: photoMetadataSchema,
  blur: photoBlurSchema,
  temporaryUrl: z.string(),
});

export type CreatePhotoResponse = z.infer<typeof createPhotoResponse>;