import { api } from "@/lib/axios"

export async function getSettings() {
  const { data } = await api.get<unknown>("/settings")
  return data
}

