import { api } from "@/lib/axios"
import type { UserProfile } from "../types"

export async function fetchCurrentUser() {
  const { data } = await api.get<UserProfile>("/auth/me")
  return data
}

