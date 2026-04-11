import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { NotificationItemCard } from '@/features/notifications/components/NotificationItem'

const markReadMock = vi.fn()
const respondResolveCheckMock = vi.fn()

vi.mock('@/features/notifications/hooks/useNotifications', () => ({
  useMarkNotificationRead: () => ({ mutate: markReadMock }),
  useRespondResolveCheck: () => ({ mutate: respondResolveCheckMock, isPending: false }),
}))

describe('NotificationItemCard resolve-check actions', () => {
  it('renders resolve-check actions and sends affirmative response', () => {
    render(
      <NotificationItemCard
        notification={{
          id: 'n-1',
          type: 'ResolveCheck',
          title: '¿Encontraste a Firulais?',
          body: 'Confirma estado del reporte',
          isRead: false,
          relatedEntityId: 'lost-1',
          createdAt: new Date().toISOString(),
        }}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: /sí, ya está en casa/i }))

    expect(respondResolveCheckMock).toHaveBeenCalledWith({ id: 'n-1', foundAtHome: true })
    expect(markReadMock).not.toHaveBeenCalled()
  })
})
