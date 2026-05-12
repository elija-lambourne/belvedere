/**
 * Shares API Client (Wretch Implementation)
 *
 * Provides methods for share link creation and resolution
 */

import { apiWithCsrf } from "@/lib/wretch"
import wretch from "wretch"

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
  let req = wretch("/api").url(`/shares/${shareKey}`)

  if (password) {
    req = req.query({ password })
  }

  return req.get().json<ShareResolution>()
}

