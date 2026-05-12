/**
 * Authentication API Client (Wretch Implementation)
 *
 * Provides methods for authentication-related API calls using wretch
 */

import { apiWithCsrf } from "../../../lib/axios"
import type { UserProfile } from "../types"

/**
 * Fetch CSRF token from backend
 * Should be called during app initialization before making any state-changing requests
 */
export async function fetchCsrfToken(): Promise<void> {
  try {
    await apiWithCsrf.url("/auth/csrf").get().res()
    // Token is now in XSRF-TOKEN cookie and will be automatically
    // included in state-changing requests via the middleware
  } catch (error) {
    console.error("Failed to fetch CSRF token:", error)
    // Not critical - will be fetched on first mutation if needed
  }
}

/**
 * Fetch information about the currently authenticated user
 * Returns 401 if user is not authenticated
 */
export async function fetchCurrentUser(): Promise<UserProfile> {
  return apiWithCsrf
    .url("/auth/me")
    .get()
    .json<UserProfile>()
}

/**
 * Initiate OIDC login flow via Keycloak
 * Redirects browser to Keycloak login page
 *
 * @param returnUrl - URL to redirect to after successful login (default: "/")
 */
export function login(returnUrl: string = "/"): void {
  const encodedUrl = encodeURIComponent(returnUrl)
  window.location.href = `/api/auth/login?returnUrl=${encodedUrl}`
}

/**
 * Initiate OIDC logout flow via Keycloak
 * Clears session and redirects browser through logout flow
 *
 * @param returnUrl - URL to redirect to after logout (default: "/")
 */
export function logout(returnUrl: string = "/"): void {
  const encodedUrl = encodeURIComponent(returnUrl)
  window.location.href = `/api/auth/logout?returnUrl=${encodedUrl}`
}
