import z from "zod"

export const userProfileSchema = z.object({
  id: z.string(),
  name: z.string(),
  email: z.string().nullable(),
  roles: z.array(z.string()).nullable(),
})

export type UserProfile = z.infer<typeof userProfileSchema>;
