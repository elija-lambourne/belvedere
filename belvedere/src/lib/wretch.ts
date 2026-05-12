import wretch from "wretch";
import FormDataAddon from "wretch/addons/formData";
import QueryAddon from "wretch/addons/query";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "/api";
const CSRF_COOKIE_NAMES = ["XSRF-TOKEN", "CSRF-TOKEN"];
const CSRF_HEADER_NAME = "X-XSRF-TOKEN";
const SAFE_METHODS = new Set(["get", "head", "options", "trace"]);

/**
 * Read a cookie value from document.cookie
 * Used to retrieve the CSRF token from the XSRF-TOKEN cookie
 */
function readCookie(name: string): string | undefined {
  if (typeof document === "undefined") {
    return undefined;
  }

  const cookie = document.cookie
    .split(";")
    .map((entry) => entry.trim())
    .find((entry) => entry.startsWith(`${name}=`));

  return cookie ? decodeURIComponent(cookie.slice(name.length + 1)) : undefined;
}

/**
 * Retrieve CSRF token from cookies
 * Tries multiple cookie names for compatibility
 */
function getCsrfToken(): string | undefined {
  for (const cookieName of CSRF_COOKIE_NAMES) {
    const value = readCookie(cookieName);
    if (value) {
      return value;
    }
  }
  return undefined;
}

/**
 * Create a wretch instance configured for the Belvedere BFF
 */
export const api = wretch(API_BASE_URL)
  .addon(FormDataAddon)
  .addon(QueryAddon)
  .options({
    // Include cookies with every request (for session authentication)
    credentials: "include",
    headers: {
      "X-Requested-With": "XMLHttpRequest",
    },
  });

/**
 * Add request interceptor to include CSRF token for non-safe methods
 * This wraps the api instance with middleware that adds the X-XSRF-TOKEN header
 */
export function createApiWithCsrf() {
  return api.middlewares([
    (next) => (url, opts) => {
      const method = (opts?.method || "GET").toLowerCase();

      // Include CSRF token only for state-changing requests
      if (!SAFE_METHODS.has(method)) {
        const csrfToken = getCsrfToken();
        if (csrfToken) {
          opts = opts || {};
          opts.headers = opts.headers || {};
          opts.headers[CSRF_HEADER_NAME] = csrfToken;
        }
      }

      return next(url, opts);
    },
  ]);
}

/**
 * Create final API instance with CSRF middleware
 * Use this as your main API client throughout the app
 */
export const apiWithCsrf = createApiWithCsrf();

/**
 * Check if error is an unauthorized (401) response
 * Useful for detecting when user session has expired
 */
export function isUnauthorizedError(error: unknown): boolean {
  if (error instanceof Response) {
    return error.status === 401;
  }
  if (error instanceof Error) {
    const err = error as any;
    return err?.status === 401 || err?.response?.status === 401;
  }
  return false;
}

/**
 * Check if error is a "Gone" (410) response
 * Used for detecting expired share keys
 */
export function isGoneError(error: unknown): boolean {
  if (error instanceof Response) {
    return error.status === 410;
  }
  if (error instanceof Error) {
    const err = error as any;
    return err?.status === 410 || err?.response?.status === 410;
  }
  return false;
}

/**
 * Check if error is "Not Found" (404) response
 * Used for detecting missing resources or unauthorized access
 */
export function isNotFoundError(error: unknown): boolean {
  if (error instanceof Response) {
    return error.status === 404;
  }
  if (error instanceof Error) {
    const err = error as any;
    return err?.status === 404 || err?.response?.status === 404;
  }
  return false;
}

/**
 * Check if error is "Forbidden" (403) response
 * Used for detecting permission denied errors
 */
export function isForbiddenError(error: unknown): boolean {
  if (error instanceof Response) {
    return error.status === 403;
  }
  if (error instanceof Error) {
    const err = error as any;
    return err?.status === 403 || err?.response?.status === 403;
  }
  return false;
}

/**
 * Check if error is "Bad Request" (400) response
 * Used for detecting validation errors
 */
export function isBadRequestError(error: unknown): boolean {
  if (error instanceof Response) {
    return error.status === 400;
  }
  if (error instanceof Error) {
    const err = error as any;
    return err?.status === 400 || err?.response?.status === 400;
  }
  return false;
}

