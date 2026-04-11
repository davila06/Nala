/**
 * Shared render wrapper: provides QueryClient + MemoryRouter
 * so components that use React Query / react-router-dom can mount without errors.
 */
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, type RenderOptions } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import type { ReactElement } from 'react'

function makeQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
}

export function renderWithProviders(
  ui: ReactElement,
  {
    initialEntries = ['/'],
    routePath,
    ...options
  }: RenderOptions & { initialEntries?: string[]; routePath?: string } = {},
) {
  const queryClient = makeQueryClient()

  // If a routePath is provided (e.g. '/p/:id'), wrap in Routes so useParams works.
  const content = routePath ? (
    <Routes>
      <Route path={routePath} element={ui} />
    </Routes>
  ) : ui

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={initialEntries}>{content}</MemoryRouter>
    </QueryClientProvider>,
    options,
  )
}

