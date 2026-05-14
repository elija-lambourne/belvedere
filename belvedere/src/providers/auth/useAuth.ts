import * as React from "react"

import { AuthContext } from "./auth-context"

export function useAuth() {
  const context = React.useContext(AuthContext)
  console.log(context);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider")
  }

  return context;
}

