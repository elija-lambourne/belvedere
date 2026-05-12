/**
 * Shares API Client (Wretch Implementation)
 *
 * Provides methods for share link creation and resolution
 */

import { apiWithCsrf, api } from "../../lib/axios"

export type ResourceType = "photo" | "album"

export interface CreateShareInput {
  targetType: ResourceType
  targetId: string
  password?: string
  expiresAt?: string // ISO 8601
}

export interface ShareResponse {
  shareKey: string
  shareUrl: string
  expiresAt?: string
}

export interface ShareResolution {
  targetType: ResourceType
  targetId: string
}

/**
 * Create a new share link for a photo or album
 * Requires user authentication
 */
export async function createShare(input: CreateShareInput): Promise<ShareResponse> {
  return apiWithCsrf
    .url("/shares")
    .post(input)
    .json<ShareResponse>()
}

/**
 * Resolve a share key to determine what resource it points to
 * Public endpoint - no authentication required
 *
 * Use this without apiWithCsrf for anonymous share access
 */
export async function resolveShare(
  shareKey: string,
  password?: string
): Promise<ShareResolution> {
  const resp = await api.get(`/shares/${shareKey}`, {
    params: password ? { password } : undefined,
  })

  return resp.data as ShareResolution
}

