import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios"

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "/api"
const CSRF_COOKIE_NAMES = ["XSRF-TOKEN", "CSRF-TOKEN", "__RequestVerificationToken"]
const CSRF_HEADER_NAME = "RequestVerificationToken"
const SAFE_METHODS = new Set(["get", "head", "options", "trace"])

function readCookie(name: string) {
  if (typeof document === "undefined") {
    return undefined
  }

  const cookie = document.cookie
    .split(";")
    .map((entry) => entry.trim())
    .find((entry) => entry.startsWith(`${name}=`))

  return cookie ? decodeURIComponent(cookie.slice(name.length + 1)) : undefined
}

function getCsrfToken() {
  for (const cookieName of CSRF_COOKIE_NAMES) {
    const value = readCookie(cookieName)
    if (value) {
      return value
    }
  }

  const metaToken = document
    .querySelector<HTMLMetaElement>('meta[name="csrf-token"]')
    ?.getAttribute("content")

  return metaToken ?? undefined
}

export const api = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true,
  headers: {
    "X-Requested-With": "XMLHttpRequest",
  },
})

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const method = (config.method ?? "get").toLowerCase()

  if (!SAFE_METHODS.has(method)) {
    const csrfToken = getCsrfToken()
    if (csrfToken) {
      config.headers.set(CSRF_HEADER_NAME, csrfToken)
    }
  }

  return config
})

export function isUnauthorizedError(error: unknown) {
  return axios.isAxiosError(error) && error.response?.status === 401
}

export type { AxiosError }

