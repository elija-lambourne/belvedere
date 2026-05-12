/**
 * Settings API Client (Wretch Implementation)
 */

import { apiWithCsrf } from "@/lib/wretch"

export async function getSettings() {
  return apiWithCsrf
    .url("/settings")
    .get()
    .json<unknown>()
}

