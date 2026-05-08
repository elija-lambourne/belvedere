import * as React from "react"

export function useDisclosure(initialState = false) {
  const [isOpen, setIsOpen] = React.useState(initialState)

  const onOpen = React.useCallback(() => setIsOpen(true), [])
  const onClose = React.useCallback(() => setIsOpen(false), [])
  const onToggle = React.useCallback(() => setIsOpen((current) => !current), [])

  return { isOpen, onOpen, onClose, onToggle, setIsOpen }
}

