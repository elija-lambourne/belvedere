/**
 * API Error Handling Utilities
 *
 * Provides type-safe error parsing and handling for HTTP responses
 */

export type ApiError =
  | { type: "unauthorized" }
  | { type: "forbidden"; message?: string }
  | { type: "notFound"; resourceId?: string }
  | { type: "gone"; message?: string }
  | { type: "validation"; errors: Record<string, string[]> }
  | { type: "payloadTooLarge"; message?: string }
  | { type: "unsupportedMediaType"; message?: string }
  | { type: "conflict"; message?: string }
  | { type: "unknown"; message: string; status?: number };

/**
 * Parse HTTP errors into typed ApiError objects
 * Attempts to extract error details from Response bodies when available
 */
export async function parseApiError(error: unknown): Promise<ApiError> {
  try {
    // Error is a Response object (from fetch/wretch)
    if (error instanceof Response) {
      const status = error.status;

      if (status === 401) {
        return { type: "unauthorized" };
      }

      if (status === 403) {
        return { type: "forbidden", message: "Access denied" };
      }

      if (status === 404) {
        return { type: "notFound" };
      }

      if (status === 410) {
        return { type: "gone", message: "Resource expired or no longer available" };
      }

      if (status === 413) {
        return { type: "payloadTooLarge", message: "File is too large" };
      }

      if (status === 415) {
        return {
          type: "unsupportedMediaType",
          message: "File type is not supported",
        };
      }

      if (status === 409) {
        return { type: "conflict", message: "Resource conflict" };
      }

      if (status === 400) {
        try {
          const data = await error.clone().json();
          if (data.errors && typeof data.errors === "object") {
            return { type: "validation", errors: data.errors };
          }
          return {
            type: "unknown",
            message: data.message || `HTTP ${status}`,
            status,
          };
        } catch {
          return { type: "unknown", message: `HTTP ${status}`, status };
        }
      }

      // Try to read error body
      try {
        const text = await error.clone().text();
        return { type: "unknown", message: text || `HTTP ${status}`, status };
      } catch {
        return { type: "unknown", message: `HTTP ${status}`, status };
      }
    }

    // Error is a plain Error or Error-like object
    if (error instanceof Error) {
      const err = error as any;

      // Check status properties
      const status = err?.status || err?.response?.status;

      if (status === 401) {
        return { type: "unauthorized" };
      }
      if (status === 403) {
        return { type: "forbidden", message: error.message };
      }
      if (status === 404) {
        return { type: "notFound" };
      }
      if (status === 410) {
        return { type: "gone", message: error.message };
      }
      if (status === 413) {
        return { type: "payloadTooLarge", message: error.message };
      }
      if (status === 415) {
        return { type: "unsupportedMediaType", message: error.message };
      }
      if (status === 409) {
        return { type: "conflict", message: error.message };
      }

      return {
        type: "unknown",
        message: error.message || "Unknown error",
        status,
      };
    }

    // Fallback for primitive error values
    return {
      type: "unknown",
      message: typeof error === "string" ? error : "Unknown error occurred",
    };
  } catch {
    return {
      type: "unknown",
      message: "Failed to parse error response",
    };
  }
}

/**
 * Convert ApiError to user-friendly message
 */
export function getErrorMessage(error: ApiError): string {
  switch (error.type) {
    case "unauthorized":
      return "You are not authenticated. Please log in.";

    case "forbidden":
      return error.message || "You don't have permission to perform this action";

    case "notFound":
      return "The resource you're looking for was not found";

    case "gone":
      return error.message || "This share link has expired";

    case "validation":
      // Combine all validation errors into a single string
      const errorMessages = Object.entries(error.errors)
        .flatMap(([field, messages]) =>
          messages.map((msg) => `${field}: ${msg}`)
        )
        .join(", ");
      return `Validation failed: ${errorMessages}`;

    case "payloadTooLarge":
      return error.message || "File is too large";

    case "unsupportedMediaType":
      return error.message || "File type is not supported";

    case "conflict":
      return error.message || "Resource conflict occurred";

    case "unknown":
    default:
      return error.message || "An unknown error occurred";
  }
}

/**
 * Wraps an async function with automatic error parsing and handling
 * Useful for component handlers that need consistent error management
 */
export async function withErrorHandling<T>(
  fn: () => Promise<T>,
  options?: {
    onError?: (error: ApiError) => void;
    onSuccess?: (result: T) => void;
    rethrow?: boolean;
  }
): Promise<T | null> {
  try {
    const result = await fn();
    options?.onSuccess?.(result);
    return result;
  } catch (error) {
    const apiError = await parseApiError(error);
    options?.onError?.(apiError);

    if (options?.rethrow) {
      throw apiError;
    }

    return null;
  }
}

/**
 * Get HTTP status code from error
 */
export function getErrorStatus(error: unknown): number | undefined {
  if (error instanceof Response) {
    return error.status;
  }

  if (error instanceof Error) {
    const err = error as any;
    return err?.status ?? err?.response?.status;
  }

  return undefined;
}

/**
 * Check if error has a specific type
 */
export function isApiErrorType(error: unknown, type: ApiError["type"]): boolean {
  return (error as any)?.type === type;
}

