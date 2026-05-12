/**
 * Photos API Client (Wretch Implementation)
 *
 * Provides methods for photo-related API calls
 */

import { apiWithCsrf } from "@/lib/wretch"

export interface PhotoMetadata {
  id: string
  title?: string
  description?: string
  fileName: string
  mimeType: string
  width: number
  height: number
  fileSize: number
  createdAt: string
  capturedAt: string
  make?: string
  model?: string
  exposureTime?: number
  fNumber?: number
  iso?: number
  latitude?: number
  longitude?: number
  countryCode?: string
  city?: string
  isLivePhoto: boolean
  reactions: Record<string, number>
}

export interface PhotoSignedUrl {
  temporaryUrl: string
}

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

  return req.get().json<PhotoMetadata>()
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

  const { temporaryUrl } = await req.get().json<PhotoSignedUrl>()
  return temporaryUrl
}

export interface CreatePhotoInput {
  file: File
  title?: string
  description?: string
}

export interface CreatePhotoResponse {
  id: string
  title?: string
  fileName: string
  mimeType: string
  width: number
  height: number
  fileSize: number
  createdAt: string
  temporaryUrl: string
}

/**
 * Upload a new photo
 * Currently returns 501 Not Implemented - pending full implementation
 */
export async function uploadPhoto(input: CreatePhotoInput): Promise<CreatePhotoResponse> {
  const formData = new FormData()
  formData.append("file", input.file)
  if (input.title) {
    formData.append("title", input.title)
  }
  if (input.description) {
    formData.append("description", input.description)
  }

  return apiWithCsrf
    .url("/photos")
    .post(formData)
    .json<CreatePhotoResponse>()
}
