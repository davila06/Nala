/**
 * Centralised toast utility — thin wrapper around sonner so consumers
 * never import from "sonner" directly. This makes it trivial to swap
 * the underlying library in the future.
 */
import { toast as sonnerToast } from 'sonner'

export const toast = {
  success: (message: string, description?: string) =>
    sonnerToast.success(message, { description }),

  error: (message: string, description?: string) =>
    sonnerToast.error(message, { description }),

  info: (message: string, description?: string) =>
    sonnerToast.info(message, { description }),

  warning: (message: string, description?: string) =>
    sonnerToast.warning(message, { description }),

  loading: (message: string) => sonnerToast.loading(message),

  dismiss: (id?: string | number) => sonnerToast.dismiss(id),

  promise: sonnerToast.promise,
} as const
