import { http, HttpResponse } from 'msw'

const API = 'http://localhost:5000/api'

export const handlers = [
  // Absorb CORS preflight OPTIONS for all API routes (axios withCredentials triggers this in jsdom)
  http.options(`${API}/*`, () => new HttpResponse(null, { status: 204 })),

  // Auth: register
  http.post(`${API}/auth/register`, () =>
    HttpResponse.json({ userId: 'user-test-id' }, { status: 201 }),
  ),

  // Auth: login
  http.post(`${API}/auth/login`, () =>
    HttpResponse.json({
      user: { id: 'user-id', name: 'Denis', email: 'denis@test.cr', isAdmin: false },
      accessToken: 'mock-jwt-token',
      expiresIn: 900,
    }),
  ),

  // Auth: refresh (returns 401 so the interceptor clears auth cleanly in tests)
  http.post(`${API}/auth/refresh`, () =>
    HttpResponse.json({ detail: 'No refresh token' }, { status: 401 }),
  ),

  // Auth: logout
  http.post(`${API}/auth/logout`, () => new HttpResponse(null, { status: 204 })),

  // Public pet profile
  http.get(`${API}/public/pets/:id`, ({ params }) =>
    HttpResponse.json({
      id: params.id,
      name: 'Luna',
      species: 'Dog',
      breed: 'Labrador',
      photoUrl: null,
      status: 'Lost',
    }),
  ),
]

