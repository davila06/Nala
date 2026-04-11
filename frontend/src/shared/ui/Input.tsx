import {
  type InputHTMLAttributes,
  type TextareaHTMLAttributes,
  type ReactNode,
  forwardRef,
  useId,
} from 'react'

// ── FormField wrapper ────────────────────────────────────────────────────────

interface FormFieldProps {
  label: string
  htmlFor?: string
  error?: string
  hint?: string
  required?: boolean
  children: ReactNode
  className?: string
}

export function FormField({
  label,
  htmlFor,
  error,
  hint,
  required,
  children,
  className = '',
}: FormFieldProps) {
  return (
    <div className={`flex flex-col gap-1.5 ${className}`}>
      <label
        htmlFor={htmlFor}
        className="text-sm font-medium text-sand-800"
      >
        {label}
        {required && (
          <span className="ml-0.5 text-brand-500" aria-hidden="true">
            *
          </span>
        )}
      </label>
      {children}
      {error && (
        <p role="alert" className="flex items-center gap-1 text-xs text-danger-600">
          <svg viewBox="0 0 16 16" fill="currentColor" className="h-3.5 w-3.5 shrink-0" aria-hidden="true">
            <path d="M8 1a7 7 0 1 0 0 14A7 7 0 0 0 8 1zm0 3.5c.414 0 .75.336.75.75v3a.75.75 0 0 1-1.5 0v-3c0-.414.336-.75.75-.75zm0 6a.875.875 0 1 1 0-1.75A.875.875 0 0 1 8 10.5z" />
          </svg>
          {error}
        </p>
      )}
      {hint && !error && (
        <p className="text-xs text-sand-500">{hint}</p>
      )}
    </div>
  )
}

// ── Input ────────────────────────────────────────────────────────────────────

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
  hint?: string
}

const inputBase =
  'w-full rounded-xl border px-3.5 py-2.5 text-sm text-sand-900 placeholder:text-sand-400 ' +
  'bg-white transition-base outline-none ' +
  'focus:ring-2 focus:ring-brand-400 focus:border-brand-400 ' +
  'disabled:bg-sand-100 disabled:text-sand-400 disabled:cursor-not-allowed'

const inputNormal = 'border-sand-300 hover:border-sand-400'
const inputError  = 'border-danger-400 ring-2 ring-danger-200 focus:ring-danger-400 focus:border-danger-500'

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, hint, id, className = '', required, ...props }, ref) => {
    const uid = useId()
    const inputId = id ?? uid
    const errId   = `${inputId}-err`
    const hintId  = `${inputId}-hint`

    const input = (
      <input
        ref={ref}
        id={inputId}
        required={required}
        aria-required={required}
        aria-invalid={!!error}
        aria-describedby={
          [error ? errId : undefined, hint ? hintId : undefined]
            .filter(Boolean)
            .join(' ') || undefined
        }
        className={[inputBase, error ? inputError : inputNormal, className]
          .filter(Boolean)
          .join(' ')}
        {...props}
      />
    )

    if (!label) return input

    return (
      <FormField
        label={label}
        htmlFor={inputId}
        error={error}
        hint={hint}
        required={required}
      >
        {input}
      </FormField>
    )
  },
)
Input.displayName = 'Input'

// ── Textarea ─────────────────────────────────────────────────────────────────

interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string
  error?: string
  hint?: string
}

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ label, error, hint, id, className = '', required, ...props }, ref) => {
    const uid = useId()
    const textareaId = id ?? uid

    const textarea = (
      <textarea
        ref={ref}
        id={textareaId}
        required={required}
        aria-required={required}
        aria-invalid={!!error}
        rows={4}
        className={[
          inputBase,
          error ? inputError : inputNormal,
          'resize-y min-h-[100px]',
          className,
        ]
          .filter(Boolean)
          .join(' ')}
        {...props}
      />
    )

    if (!label) return textarea

    return (
      <FormField
        label={label}
        htmlFor={textareaId}
        error={error}
        hint={hint}
        required={required}
      >
        {textarea}
      </FormField>
    )
  },
)
Textarea.displayName = 'Textarea'
