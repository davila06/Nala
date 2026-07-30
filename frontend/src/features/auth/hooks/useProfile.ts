import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { authApi } from '../api/authApi'
import { useAuthStore } from '../store/authStore'

export function useMyProfile() {
  return useQuery({
    queryKey: ['auth', 'me'],
    queryFn: authApi.getMyProfile,
    staleTime: 5 * 60 * 1000,
  })
}

export function useUpdateProfile() {
  const queryClient = useQueryClient()
  const setAuth = useAuthStore((s) => s.setAuth)
  const user = useAuthStore((s) => s.user)
  const accessToken = useAuthStore((s) => s.accessToken)

  return useMutation({
    mutationFn: (data: { name: string }) => authApi.updateProfile(data),
    onSuccess: (_data, variables) => {
      if (user && accessToken) {
        setAuth({ ...user, name: variables.name }, accessToken)
      }
      void queryClient.invalidateQueries({ queryKey: ['auth', 'me'] })
    },
  })
}

export function useChangePassword() {
  return useMutation({
    mutationFn: (data: { currentPassword: string; newPassword: string }) =>
      authApi.changePassword(data),
  })
}

export function useDeleteAccount() {
  const clearAuth = useAuthStore((s) => s.clearAuth)

  return useMutation({
    mutationFn: (data: { confirmPassword: string }) => authApi.deleteAccount(data),
    onSuccess: () => {
      clearAuth()
    },
  })
}
