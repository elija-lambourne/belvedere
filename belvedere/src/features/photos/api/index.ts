import { api } from "@/lib/axios"

export async function listPhotos() {
  const { data } = await api.get<unknown[]>("/photos")
  return data
}

