import { useMutation } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { authApi, decodeRoleFromJwt } from '../api/authApi'
import { useAuthStore } from '../store/authStore'

export function useLogin() {
  const setAuth = useAuthStore((s) => s.setAuth)
  const navigate = useNavigate()

  return useMutation({
    mutationFn: authApi.login,
    onSuccess: ({ data }) => {
      const role = decodeRoleFromJwt(data.accessToken)
      setAuth(
        {
          id: data.user.id,
          name: data.user.name,
          email: data.user.email,
          role,
          isAdmin: data.user.isAdmin,
        },
        data.accessToken,
      )
      navigate(role === 'Ally' ? '/allies/panel' : '/dashboard')
    },
  })
}

export function useRegister() {
  const navigate = useNavigate()

  return useMutation({
    mutationFn: authApi.register,
    onSuccess: () => {
      navigate('/login?registered=true')
    },
  })
}

export function useForgotPassword() {
  return useMutation({
    mutationFn: authApi.forgotPassword,
  })
}

export function useResetPassword() {
  return useMutation({
    mutationFn: authApi.resetPassword,
  })
}

export function useLogout() {
  const clearAuth = useAuthStore((s) => s.clearAuth)
  const navigate = useNavigate()

  return useMutation({
    mutationFn: authApi.logout,
    onSettled: () => {
      clearAuth()
      navigate('/login')
    },
  })
}
