import * as React from "react"

export type Notification = {
  title: string
  description?: string
  type?: "info" | "success" | "warning" | "error"
}

export function useNotification() {
  const [notifications, setNotifications] = React.useState<Notification[]>([])

  const notify = React.useCallback((notification: Notification) => {
    setNotifications((current) => [...current, notification])
  }, [])

  const dismiss = React.useCallback((title: string) => {
    setNotifications((current) => current.filter((item) => item.title !== title))
  }, [])

  const clear = React.useCallback(() => {
    setNotifications([])
  }, [])

  return { notifications, notify, dismiss, clear }
}

