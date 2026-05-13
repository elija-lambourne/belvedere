import * as React from "react"

export type Notification = {
  id: string
  title: string
  description?: string
  type?: "info" | "success" | "warning" | "error"
}

// The notification passed to the `notify` function won't have an ID yet.
type NotificationInput = Omit<Notification, "id">

export function useNotification() {
  const [notifications, setNotifications] = React.useState<Notification[]>([])

  const notify = React.useCallback((notification: NotificationInput) => {
    const newNotification = { ...notification, id: new Date().getTime().toString() }
    setNotifications((current) => [...current, newNotification])
    return newNotification.id
  }, [])

  const dismiss = React.useCallback((id: string) => {
    setNotifications((current) => current.filter((item) => item.id !== id))
  }, [])

  const clear = React.useCallback(() => {
    setNotifications([])
  }, [])

  return { notifications, notify, dismiss, clear }
}
