/**
 * Photos API Client (Wretch Implementation)
 *
 * Provides methods for photo-related API calls
 */

import { apiWithCsrf } from "@/lib/axios.ts"
import {
  type CreatePhotoRequest,
  createPhotoResponse,
  type CreatePhotoResponse,
  photoSignedUrlResponse,
  type PhotoSignedUrlResponse,
} from "@/features/photos"
import { type PhotoMetadata, photoMetadataSchema } from "@/types"

/**
 * Get comprehensive metadata for a photo including EXIF data
 * Supports both authenticated and share-key access
 */
export async function getPhotoMetadata(
  photoId: string,
  shareKey?: string,
  sharePassword?: string
): Promise<PhotoMetadata> {
  let req = apiWithCsrf.url(`/photos/${photoId}`)

  if (shareKey) {
    req = req.query({ shareKey, sharePassword })
  }

  const res = req.get().json<PhotoMetadata>();
  return photoMetadataSchema.parse(res);
}

/**
 * Get presigned URL for accessing full-resolution photo
 * Returns a temporary 5-minute S3 URL
 * Supports both authenticated and share-key access
 */
export async function getPhotoSignedUrl(
  photoId: string,
  shareKey?: string,
  sharePassword?: string
): Promise<string> {
  let req = apiWithCsrf.url(`/photos/${photoId}/signed-url`)

  if (shareKey) {
    req = req.query({ shareKey, sharePassword })
  }

  const res = await req.get().json<PhotoSignedUrlResponse>();
  return photoSignedUrlResponse.parse(res).temporaryUrl;
}

/**
 * Upload a new photo
 * Currently returns 501 Not Implemented - pending full implementation
 */
export async function uploadPhoto(input: CreatePhotoRequest): Promise<CreatePhotoResponse> {
  const formData = new FormData()
  formData.append("file", input.file)
  if (input.title) {
    formData.append("title", input.title)
  }
  if (input.description) {
    formData.append("description", input.description)
  }

  const res = await apiWithCsrf
    .url("/photos")
    .post(formData)
    .json<CreatePhotoResponse>();
  return createPhotoResponse.parse(res);
}
