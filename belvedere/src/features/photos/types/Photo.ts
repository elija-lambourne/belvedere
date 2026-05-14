import z from "zod";
import { photoMetadataSchema, photoSchema, photoThumbnailSchema } from "@/types"

export const photoSignedUrlResponse = z.object({
  temporaryUrl: z.string(),
});

export type PhotoSignedUrlResponse = z.infer<typeof photoSignedUrlResponse>;

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
  temporaryUrl: z.string(),
});

export type CreatePhotoResponse = z.infer<typeof createPhotoResponse>;