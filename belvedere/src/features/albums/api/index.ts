/**
 * Albums API Client (Wretch Implementation)
 *
 * Provides methods for album-related API calls
 */

import { apiWithCsrf } from "@/lib/wretch"

export interface Album {
  id: string
  title: string
  description?: string
  coverPhotoId?: string
  isPublic: boolean
  createdAt: string
  photoCount: number
}

export interface PhotoBlur {
  id: string
  title?: string
  description?: string
  fileName: string
  blurHash: string
  width: number
  height: number
  mimeType: string
  createdAt: string
}

export interface PhotoThumbnail extends PhotoBlur {
  fileSize: number
  make?: string
  model?: string
  exposureTime?: number
  fNumber?: number
  iso?: number
  city?: string
  isLivePhoto: boolean
  thumbnailUrl: string
}

export interface AlbumExtended extends Album {
  photos: PhotoBlur[]
}

export interface AlbumWithThumbnails extends Album {
  photos: PhotoThumbnail[]
}

export interface CreateAlbumInput {
  title: string
  description?: string
  isPublic?: boolean
}

/**
 * Fetch all albums for the authenticated user
 */
export async function listAlbums(): Promise<Album[]> {
  return apiWithCsrf
    .url("/albums")
    .get()
    .json<Album[]>()
}

/**
 * Get album with all photo details including blur hashes
 * Supports both authenticated and share-key access
 */
export async function getAlbumPreload(
  albumId: string,
  shareKey?: string,
  sharePassword?: string
): Promise<AlbumExtended> {
  let req = apiWithCsrf.url(`/albums/${albumId}/preload`)

  if (shareKey) {
    req = req.query({ shareKey, sharePassword })
  }

  return req.get().json<AlbumExtended>()
}

/**
 * Get album with thumbnail URLs for all photos
 * Supports both authenticated and share-key access
 */
export async function getAlbumThumbnails(
  albumId: string,
  shareKey?: string,
  sharePassword?: string
): Promise<AlbumWithThumbnails> {
  let req = apiWithCsrf.url(`/albums/${albumId}/thumbnails`)

  if (shareKey) {
    req = req.query({ shareKey, sharePassword })
  }

  return req.get().json<AlbumWithThumbnails>()
}

/**
 * Create a new album
 */
export async function createAlbum(input: CreateAlbumInput): Promise<Album> {
  return apiWithCsrf
    .url("/albums")
    .post(input)
    .json<Album>()
}

/**
 * Delete an album (owner only)
 */
export async function deleteAlbum(albumId: string): Promise<void> {
  await apiWithCsrf.url(`/albums/${albumId}`).delete().res()
}

/**
 * Add a photo to an album (owner only)
 */
export async function addPhotoToAlbum(
  albumId: string,
  photoId: string
): Promise<void> {
  await apiWithCsrf
    .url(`/albums/${albumId}/photos/${photoId}`)
    .post({})
    .res()
}

/**
 * Remove a photo from an album (owner only)
 */
export async function removePhotoFromAlbum(
  albumId: string,
  photoId: string
): Promise<void> {
  await apiWithCsrf
    .url(`/albums/${albumId}/photos/${photoId}`)
    .delete()
    .res()
}
