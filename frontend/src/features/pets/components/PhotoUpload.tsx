import { useCallback, useRef, useState } from 'react'

interface PhotoUploadProps {
  value?: File | null
  previewUrl?: string | null
  onChange: (file: File | null) => void
  disabled?: boolean
}

const ACCEPTED = ['image/jpeg', 'image/png', 'image/webp']
const MAX_MB = 5

export const PhotoUpload = ({ value, previewUrl, onChange, disabled }: PhotoUploadProps) => {
  const inputRef = useRef<HTMLInputElement>(null)
  const [dragging, setDragging] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const preview = value
    ? URL.createObjectURL(value)
    : (previewUrl ?? null)

  const validate = (file: File): string | null => {
    if (!ACCEPTED.includes(file.type)) return 'Solo se aceptan archivos JPEG, PNG o WebP.'
    if (file.size > MAX_MB * 1024 * 1024) return `El archivo debe pesar menos de ${MAX_MB} MB.`
    return null
  }

  const handleFile = useCallback(
    (file: File | null) => {
      if (!file) {
        setError(null)
        onChange(null)
        return
      }
      const err = validate(file)
      if (err) {
        setError(err)
        return
      }
      setError(null)
      onChange(file)
    },
    [onChange],
  )

  const onInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    handleFile(e.target.files?.[0] ?? null)
  }

  const onDrop = (e: React.DragEvent) => {
    e.preventDefault()
    setDragging(false)
    handleFile(e.dataTransfer.files?.[0] ?? null)
  }

  return (
    <div className="space-y-2">
      <div
        role="button"
        tabIndex={disabled ? -1 : 0}
        aria-label="Subir foto de la mascota"
        onClick={() => !disabled && inputRef.current?.click()}
        onKeyDown={(e) => (e.key === 'Enter' || e.key === ' ') && !disabled && inputRef.current?.click()}
        onDragOver={(e) => {
          e.preventDefault()
          setDragging(true)
        }}
        onDragLeave={() => setDragging(false)}
        onDrop={onDrop}
        className={[
          'relative flex min-h-40 cursor-pointer flex-col items-center justify-center gap-3 rounded-2xl border-2 border-dashed transition-colors',
          dragging
            ? 'border-brand-400 bg-brand-50'
            : 'border-sand-300 bg-sand-50 hover:border-brand-400 hover:bg-brand-50/40',
          disabled ? 'pointer-events-none opacity-50' : '',
        ].join(' ')}
      >
        {preview ? (
          <img
            src={preview}
            alt="Vista previa de la foto"
            className="h-36 max-w-full rounded-xl object-cover"
          />
        ) : (
          <>
            <span className="text-3xl" aria-hidden="true">
              📷
            </span>
            <p className="text-sm font-medium text-sand-600">
              Arrastra o{' '}
              <span className="text-brand-600 underline underline-offset-2">selecciona un archivo</span>
            </p>
            <p className="text-xs text-sand-400">JPEG, PNG o WebP · máx {MAX_MB} MB</p>
          </>
        )}
      </div>

      {preview && !disabled && (
        <button
          type="button"
          onClick={() => {
            handleFile(null)
            if (inputRef.current) inputRef.current.value = ''
          }}
          className="text-xs text-danger-500 hover:underline"
        >
          Eliminar foto
        </button>
      )}

      {error && (
        <p role="alert" className="text-xs font-medium text-danger-600">
          {error}
        </p>
      )}

      <input
        ref={inputRef}
        type="file"
        accept={ACCEPTED.join(',')}
        className="sr-only"
        onChange={onInputChange}
        disabled={disabled}
        aria-hidden="true"
      />
    </div>
  )
}

