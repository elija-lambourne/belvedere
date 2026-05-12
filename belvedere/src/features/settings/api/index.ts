/**
 * Settings API Client (Wretch Implementation)
 */

import { apiWithCsrf } from "../../../lib/axios"

export async function getSettings() {
  return apiWithCsrf
    .url("/settings")
    .get()
    .json<unknown>()
}

