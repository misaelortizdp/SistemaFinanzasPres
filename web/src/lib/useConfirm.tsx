import { useCallback, useState } from "react"
import { ConfirmDialog } from "@/components/ui/dialog"

interface UseConfirmOptions {
  title: string
  description: string
  confirmText?: string
  cancelText?: string
  variant?: "default" | "destructive"
}

export function useConfirm() {
  const [isOpen, setIsOpen] = useState(false)
  const [options, setOptions] = useState<UseConfirmOptions>({
    title: "",
    description: ""
  })
  const [resolver, setResolver] = useState<((value: boolean) => void) | null>(null)

  function confirm(opts: UseConfirmOptions): Promise<boolean> {
    return new Promise((resolve) => {
      setOptions(opts)
      setIsOpen(true)
      setResolver(() => resolve)
    })
  }

  function handleConfirm() {
    if (resolver) {
      resolver(true)
      setResolver(null)
    }
    setIsOpen(false)
  }

  function handleCancel() {
    if (resolver) {
      resolver(false)
      setResolver(null)
    }
    setIsOpen(false)
  }

  const ConfirmDialogComponent = useCallback(() => (
    <ConfirmDialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) handleCancel()
      }}
      title={options.title}
      description={options.description}
      confirmText={options.confirmText}
      cancelText={options.cancelText}
      variant={options.variant}
      onConfirm={handleConfirm}
    />
  ), [isOpen, options, resolver])

  return { confirm, ConfirmDialog: ConfirmDialogComponent }
}
