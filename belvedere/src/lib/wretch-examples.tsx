/**
 * Comprehensive Example: Component with Error Handling
 *
 * This file demonstrates how to build a component using wretch with proper error handling
 */

import React from "react"
import { useQuery } from "@tanstack/react-query"
import { parseApiError, getErrorMessage } from "@/lib/wretch-errors"
import { isUnauthorizedError, isGoneError, isNotFoundError } from "@/lib/wretch"
import { useNavigate } from "react-router-dom"

/**
 * Example 1: Simple Loading States
 */
export function AlbumLoadingExample({ albumId }: { albumId: string }) {
  const { data, isLoading, error } = useQuery({
    queryKey: ["albums", albumId],
    queryFn: async () => {
      // Simulate fetching album
      const response = await fetch(`/api/albums/${albumId}`)
      if (!response.ok) throw response
      return response.json()
    },
  })

  if (isLoading) return <div className="loading">Loading album...</div>
  if (error) return <div className="error">Error loading album</div>
  if (!data) return <div>No album found</div>

  return (
    <div>
      <h1>{data.title}</h1>
      <p>{data.description}</p>
    </div>
  )
}

/**
 * Example 2: Detailed Error Handling
 */
export function AlbumDetailExample({ albumId, shareKey }: { albumId: string; shareKey?: string }) {
  const navigate = useNavigate()
  const [errorMessage, setErrorMessage] = React.useState<string | null>(null)

  const { data, isLoading, error } = useQuery({
    queryKey: ["albums", albumId, "detail", shareKey],
    queryFn: async () => {
      try {
        if (isUnauthorizedError(error)) {
          navigate("/login")
          return null
        }

        if (isGoneError(error)) {
          setErrorMessage("Share link has expired. Contact the owner for a new link.")
          return null
        }

        if (isNotFoundError(error)) {
          setErrorMessage("Album not found or you don't have access")
          return null
        }

        // Simulate fetching with share key
        let url = `/api/albums/${albumId}/thumbnails`
        if (shareKey) {
          url += `?shareKey=${shareKey}`
        }

        const response = await fetch(url, { credentials: "include" })
        if (!response.ok) throw response
        return response.json()
      } catch (err) {
        const apiError = await parseApiError(err)
        setErrorMessage(getErrorMessage(apiError))
        throw err
      }
    },
    retry: false,
  })

  if (isLoading) {
    return <div className="loading">Loading album...</div>
  }

  if (errorMessage) {
    return (
      <div className="error">
        <p>{errorMessage}</p>
        <button onClick={() => navigate(-1)}>Go Back</button>
      </div>
    )
  }

  if (!data) {
    return <div className="not-found">Album not found</div>
  }

  return (
    <div className="album-detail">
      <h1>{data.title}</h1>
      {data.description && <p>{data.description}</p>}
      <div className="photos-grid">
        {data.photos.map((photo: any) => (
          <div key={photo.id} className="photo-card">
            <img src={photo.thumbnailUrl} alt={photo.fileName} />
            <h3>{photo.title || photo.fileName}</h3>
          </div>
        ))}
      </div>
    </div>
  )
}

/**
 * Example 3: Mutation with Error Handling
 */
export function CreateAlbumExample() {
  const queryClient = require("@tanstack/react-query").useQueryClient()
  const navigate = useNavigate()
  const [error, setError] = React.useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: async (formData: { title: string; isPublic: boolean }) => {
      const response = await fetch("/api/albums", {
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
          "X-XSRF-TOKEN": getCsrfToken(),
        },
        body: JSON.stringify(formData),
      })

      if (!response.ok) throw response
      return response.json()
    },
    onSuccess: (data) => {
      // Invalidate queries to trigger refetch
      queryClient.invalidateQueries({ queryKey: ["albums"] })
      navigate(`/albums/${data.id}`)
    },
    onError: async (err) => {
      const apiError = await parseApiError(err)

      if (apiError.type === "unauthorized") {
        navigate("/login")
      } else if (apiError.type === "validation") {
        setError(`Validation failed: ${Object.keys(apiError.errors).join(", ")}`)
      } else {
        setError(getErrorMessage(apiError))
      }
    },
  })

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const formData = new FormData(e.currentTarget)
    mutation.mutate({
      title: formData.get("title") as string,
      isPublic: formData.get("isPublic") === "on",
    })
  }

  return (
    <form onSubmit={handleSubmit}>
      {error && <div className="error">{error}</div>}

      <label>
        Title:
        <input type="text" name="title" required />
      </label>

      <label>
        Public:
        <input type="checkbox" name="isPublic" />
      </label>

      <button type="submit" disabled={mutation.isPending}>
        {mutation.isPending ? "Creating..." : "Create Album"}
      </button>
    </form>
  )
}

/**
 * Helper: Get CSRF token
 */
function getCsrfToken(): string | undefined {
  const cookie = document.cookie
    .split(";")
    .map((c) => c.trim())
    .find((c) => c.startsWith("XSRF-TOKEN="))

  return cookie?.slice("XSRF-TOKEN=".length)
}

// Import useMutation from react-query
import { useMutation } from "@tanstack/react-query"

