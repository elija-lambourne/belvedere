/**
 * Albums API Client (Axios + Zod)
 *
 * Provides methods for album-related API calls.
 */

import { apiWithCsrf } from "@/lib/axios"
import {
  albumExtendedSchema,
  albumListSchema,
  albumSchema,
  albumWithThumbnailsSchema,
  type Album,
  type AlbumExtended,
  type AlbumWithThumbnails,
} from "@/types"


export interface CreateAlbumInput {
  title: string
  description?: string
  isPublic?: boolean
}

/**
 * Fetch all albums for the authenticated user
 */
export async function listAlbums(): Promise<Album[]> {
  const data = await apiWithCsrf.url("/albums").get().json<unknown>()
  return albumListSchema.parse(data)
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

  const data = await req.get().json<unknown>()
  return albumExtendedSchema.parse(data)
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

  const data = await req.get().json<AlbumWithThumbnails>()
  return albumWithThumbnailsSchema.parse(data)
}

/**
 * Create a new album
 */
export async function createAlbum(input: CreateAlbumInput): Promise<Album> {
  const data = await apiWithCsrf.url("/albums").post(input).json<unknown>()
  return albumSchema.parse(data)
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
