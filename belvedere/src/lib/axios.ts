import axios from "axios"
import type { AxiosInstance } from "axios"

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "/api"
const CSRF_COOKIE_NAMES = ["XSRF-TOKEN", "CSRF-TOKEN"]
const CSRF_HEADER_NAME = "X-XSRF-TOKEN"
const SAFE_METHODS = new Set(["get", "head", "options", "trace"])

type QueryParams = Record<string, unknown>

type StatusLikeError = {
  status?: unknown
  response?: { status?: unknown }
}

function readCookie(name: string): string | undefined {
  if (typeof document === "undefined") {
    return undefined
  }

  const cookie = document.cookie
    .split(";")
    .map((entry) => entry.trim())
    .find((entry) => entry.startsWith(`${name}=`))

  return cookie ? decodeURIComponent(cookie.slice(name.length + 1)) : undefined
}

function getCsrfToken(): string | undefined {
  for (const name of CSRF_COOKIE_NAMES) {
    const value = readCookie(name)
    if (value) {
      return value
    }
  }

  return undefined
}

function createAxiosInstance(withCsrf = false): AxiosInstance {
  const instance = axios.create({
    baseURL: API_BASE_URL,
    withCredentials: true,
    headers: {
      "X-Requested-With": "XMLHttpRequest",
    },
  })

  if (withCsrf) {
    instance.interceptors.request.use((config) => {
      const method = (config.method || "get").toLowerCase()

      if (!SAFE_METHODS.has(method)) {
        const token = getCsrfToken()
        if (token) {
          const headers = axios.AxiosHeaders.from(config.headers ?? {})
          headers.set(CSRF_HEADER_NAME, token)
          config.headers = headers
        }
      }

      return config
    })
  }

  return instance;
}

export const api = createAxiosInstance(false)
const apiWithCsrfClient = createAxiosInstance(true)

function isStatusLikeError(error: unknown): error is StatusLikeError {
  return typeof error === "object" && error !== null
}

function getStatusFromError(error: unknown): number | undefined {
  if (axios.isAxiosError(error)) {
    return error.response?.status
  }

  if (!isStatusLikeError(error)) {
    return undefined
  }

  if (typeof error.status === "number") {
    return error.status
  }
  if (error.response && typeof error.response.status === "number") {
    return error.response.status
  }

  return undefined
}

function makeChain(path: string) {
  let query: QueryParams | undefined

  return {
    query(q: QueryParams | undefined) {
      query = q
      return this
    },
    get() {
      const promise = apiWithCsrfClient.get(path, { params: query })
      return {
        json: async <T>() => (await promise).data as T,
        res: async () => await promise,
      }
    },
    post(body?: unknown) {
      const promise = apiWithCsrfClient.post(path, body, {
        params: query,
      })
      return {
        json: async <T>() => (await promise).data as T,
        res: async () => await promise,
      }
    },
    delete() {
      const promise = apiWithCsrfClient.delete(path, { params: query })
      return {
        res: async () => await promise,
      }
    },
  }
}

export const apiWithCsrf = {
  url(path: string) {
    return makeChain(path)
  },
}

export function isUnauthorizedError(error: unknown): boolean {
  return getStatusFromError(error) === 401
}

export function isGoneError(error: unknown): boolean {
  return getStatusFromError(error) === 410
}

export function isNotFoundError(error: unknown): boolean {
  return getStatusFromError(error) === 404
}
