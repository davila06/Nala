import { useRef, useState, useCallback, useEffect } from 'react'

interface UsePullToRefreshOptions {
  onRefresh: () => Promise<void> | void
  /** Minimum pull distance in px to trigger refresh (default 80) */
  threshold?: number
  /** Whether pull-to-refresh is active (disable on desktop or when already loading) */
  enabled?: boolean
}

/**
 * usePullToRefresh — native-feeling pull-down gesture for PWA refresh.
 *
 * Returns:
 *  - `containerRef` — attach to the scrollable container
 *  - `pullProgress` — 0..1, used to show a visual indicator
 *  - `isRefreshing` — true while onRefresh is running
 *
 * Usage:
 *   const { containerRef, pullProgress, isRefreshing } = usePullToRefresh({ onRefresh: refetch })
 *   <div ref={containerRef} className="overflow-y-auto">
 *     {isRefreshing && <RefreshIndicator progress={pullProgress} />}
 *     {children}
 *   </div>
 */
export function usePullToRefresh({
  onRefresh,
  threshold = 80,
  enabled = true,
}: UsePullToRefreshOptions) {
  const containerRef = useRef<HTMLDivElement>(null)
  const startY = useRef<number | null>(null)
  const [pullDistance, setPullDistance] = useState(0)
  const [isRefreshing, setIsRefreshing] = useState(false)

  const pullProgress = Math.min(pullDistance / threshold, 1)

  const handleTouchStart = useCallback((e: TouchEvent) => {
    const el = containerRef.current
    if (!el || el.scrollTop > 0) return   // only trigger at the very top
    startY.current = e.touches[0].clientY
  }, [])

  const handleTouchMove = useCallback((e: TouchEvent) => {
    if (startY.current === null || isRefreshing) return
    const el = containerRef.current
    if (!el || el.scrollTop > 0) { startY.current = null; return }

    const dy = e.touches[0].clientY - startY.current
    if (dy <= 0) { setPullDistance(0); return }

    // Dampen the pull with a rubber-band feel
    const damped = Math.pow(dy, 0.72) * 2.5
    setPullDistance(Math.min(damped, threshold * 1.5))
    if (dy > 10) e.preventDefault()
  }, [isRefreshing, threshold])

  const handleTouchEnd = useCallback(async () => {
    if (startY.current === null) return
    startY.current = null

    if (pullDistance >= threshold && !isRefreshing) {
      setIsRefreshing(true)
      setPullDistance(0)
      try {
        await onRefresh()
      } finally {
        setIsRefreshing(false)
      }
    } else {
      setPullDistance(0)
    }
  }, [pullDistance, threshold, isRefreshing, onRefresh])

  useEffect(() => {
    const el = containerRef.current
    if (!el || !enabled) return

    el.addEventListener('touchstart', handleTouchStart, { passive: true })
    el.addEventListener('touchmove', handleTouchMove, { passive: false })
    el.addEventListener('touchend', handleTouchEnd)

    return () => {
      el.removeEventListener('touchstart', handleTouchStart)
      el.removeEventListener('touchmove', handleTouchMove)
      el.removeEventListener('touchend', handleTouchEnd)
    }
  }, [enabled, handleTouchStart, handleTouchMove, handleTouchEnd])

  return { containerRef, pullProgress, isRefreshing, pullDistance }
}
