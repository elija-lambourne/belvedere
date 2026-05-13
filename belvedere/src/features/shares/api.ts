/**
 * Shares API Client
 *
 * Provides methods for share link creation and resolution
 */

import { apiWithCsrf, api } from "@/lib/axios.ts"
import { z } from "zod"

// By defining the enum with Zod first, we get both the runtime validation
// and the TypeScript type for free, ensuring they are always in sync.
const ResourceTypeEnum = z.enum(['ALBUM', 'PHOTO']);
export type ResourceType = z.infer<typeof ResourceTypeEnum>;


export interface CreateShareInput {
  targetType: ResourceType
  targetId: string
  password?: string
  expiresAt?: string // ISO 8601
}

const shareResponseSchema = z.object({
  shareKey: z.string(),
  shareUrl: z.string(),
  expiresAt: z.iso.datetime().nullable(),
});

export type ShareResponse = z.infer<typeof shareResponseSchema>;

const shareResolutionSchema = z.object({
  targetType: ResourceTypeEnum,
  targetId: z.uuid(),
})
export type ShareResolution = z.infer<typeof shareResolutionSchema>;

/**
 * Create a new share link for a photo or album
 * Requires user authentication
 *
 * Can throw an AxiosError. You can check the status code on the error
 * response, e.g. `error.response?.status === 401`.
 */
export async function createShare(
  input: CreateShareInput
): Promise<ShareResponse> {
  const response = await apiWithCsrf.url("/shares").post(input).json<ShareResponse>()

  return shareResponseSchema.parse(response)
}

/**
 * Resolve a share key to determine what resource it points to
 * Public endpoint - no authentication required
 * This function will throw an AxiosError on failure (e.g., 404, 401),
 * which should be handled by the calling code (e.g., in a React Query hook).
 */
export async function resolveShare(
  shareKey: string,
  password?: string
): Promise<ShareResolution> {
  const response = await api.get(`/shares/${shareKey}`, {
      params: password ? { password } : undefined,
    });

  return shareResolutionSchema.parse(response.data);
}
