import { useState, useEffect } from 'react'

/**
 * useProgressiveImage — loads a high-res image while showing a blur placeholder.
 *
 * Returns `{ src, isLoaded }` where `src` transitions from `placeholder` to `highRes`
 * once the high-res image finishes loading.
 *
 * Usage:
 *   const { src, isLoaded } = useProgressiveImage(pet.photoUrl, '/placeholder.jpg')
 *   <img src={src} className={isLoaded ? 'blur-0' : 'blur-md'} style={{ transition: 'filter 0.4s' }} />
 */
export function useProgressiveImage(highRes: string | null | undefined, placeholder = '') {
  const [src, setSrc] = useState(placeholder || highRes || '')
  const [isLoaded, setIsLoaded] = useState(!highRes)

  useEffect(() => {
    if (!highRes) {
      setSrc(placeholder)
      setIsLoaded(true)
      return
    }

    setSrc(placeholder || highRes)
    setIsLoaded(false)

    const img = new Image()
    img.src = highRes
    img.onload = () => {
      setSrc(highRes)
      setIsLoaded(true)
    }
    img.onerror = () => {
      setSrc(placeholder)
      setIsLoaded(true)
    }

    return () => {
      img.onload = null
      img.onerror = null
    }
  }, [highRes, placeholder])

  return { src, isLoaded }
}

/**
 * ProgressiveImg — drop-in <img> replacement with blur-up effect.
 */
interface ProgressiveImgProps extends Omit<React.ImgHTMLAttributes<HTMLImageElement>, 'src'> {
  src: string | null | undefined
  placeholder?: string
  alt: string
}

export function ProgressiveImg({ src, placeholder, alt, className = '', style, ...rest }: ProgressiveImgProps) {
  const { src: currentSrc, isLoaded } = useProgressiveImage(src, placeholder)

  return (
    <img
      src={currentSrc}
      alt={alt}
      className={className}
      style={{
        filter: isLoaded ? 'blur(0)' : 'blur(8px)',
        transition: 'filter 0.45s ease',
        ...style,
      }}
      {...rest}
    />
  )
}
