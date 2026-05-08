import { api } from "@/lib/axios"

export async function listAlbums() {
  const { data } = await api.get<unknown[]>("/albums")
  return data
}

