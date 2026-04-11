import { RouterProvider } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { Toaster } from 'sonner'
import { router } from './app/routes'
import { queryClient } from './app/providers'
import { useTrackLocation } from '@/features/locations/hooks/useTrackLocation'
import { useAlertPreference } from '@/features/locations/hooks/useAlertPreference'

function LocationTracker() {
  const { receiveNearbyAlerts } = useAlertPreference()
  useTrackLocation({ receiveNearbyAlerts })
  return null
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <LocationTracker />
      <RouterProvider router={router} />
      <Toaster
        position="top-right"
        richColors
        closeButton
        toastOptions={{
          classNames: {
            toast:
              'font-body text-sm rounded-xl border shadow-md',
            title: 'font-semibold',
            description: 'text-xs opacity-80',
          },
        }}
      />
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  )
}
