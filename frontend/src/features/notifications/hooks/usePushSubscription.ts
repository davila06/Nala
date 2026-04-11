import { useState } from 'react'
import {
  subscribeToPushNotifications,
  unsubscribeFromPushNotifications,
} from '../services/pushSubscription'

export function usePushSubscription() {
  const [status, setStatus] = useState<'idle' | 'loading' | 'subscribed' | 'denied' | 'unsupported'>(() => {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) return 'unsupported'
    return 'idle'
  })

  const subscribe = async () => {
    const permission = await Notification.requestPermission()
    if (permission === 'denied') {
      setStatus('denied')
      return
    }
    setStatus('loading')
    const ok = await subscribeToPushNotifications()
    setStatus(ok ? 'subscribed' : 'idle')
  }

  const unsubscribe = async () => {
    await unsubscribeFromPushNotifications()
    setStatus('idle')
  }

  return { status, subscribe, unsubscribe }
}
