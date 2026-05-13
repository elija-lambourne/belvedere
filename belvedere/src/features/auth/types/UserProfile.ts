import z from "zod"

export const userProfileSchema = z.object({
  id: z.string(),
  displayName: z.string(),
  email: z.string().optional(),
  roles: z.array(z.string()).optional(),
});

export type UserProfile = z.infer<typeof userProfileSchema>;


