import { defineStore } from 'pinia'
import type { LoginResponse, RegisterResponse, UserInfo, UserRole } from '~/types/api'

interface AuthState {
  user: UserInfo | null
  accessToken: string | null
  refreshToken: string | null
  accessTokenExpiresAt: string | null
}

export const useAuthStore = defineStore('auth', () => {
  // Cookies make the session SSR-friendly and survive reloads
  const userCookie = useCookie<UserInfo | null>('nobat_user', {
    sameSite: 'lax',
    maxAge: 60 * 60 * 24 * 7
  })
  const accessCookie = useCookie<string | null>('nobat_at', {
    sameSite: 'lax',
    maxAge: 60 * 60 * 24 * 7
  })
  const refreshCookie = useCookie<string | null>('nobat_rt', {
    sameSite: 'lax',
    maxAge: 60 * 60 * 24 * 7
  })
  const accessExpCookie = useCookie<string | null>('nobat_atexp', {
    sameSite: 'lax',
    maxAge: 60 * 60 * 24 * 7
  })

  const state = useState<AuthState>('auth', () => ({
    user: userCookie.value ?? null,
    accessToken: accessCookie.value ?? null,
    refreshToken: refreshCookie.value ?? null,
    accessTokenExpiresAt: accessExpCookie.value ?? null
  }))

  const isAuthenticated = computed(() => !!state.value.accessToken && !!state.value.user)
  const user = computed(() => state.value.user)
  const role = computed<UserRole | null>(() => state.value.user?.role ?? null)
  const isAdmin = computed(() => role.value === 'Admin')
  const isBarber = computed(() => role.value === 'Barber')
  const isCustomer = computed(() => role.value === 'Customer')

  function persist() {
    userCookie.value = state.value.user
    accessCookie.value = state.value.accessToken
    refreshCookie.value = state.value.refreshToken
    accessExpCookie.value = state.value.accessTokenExpiresAt
  }

  function setSession(payload: LoginResponse) {
    state.value.user = payload.user
    state.value.accessToken = payload.accessToken
    state.value.refreshToken = payload.refreshToken
    state.value.accessTokenExpiresAt = payload.accessTokenExpiresAt
    persist()
  }

  function clear() {
    state.value.user = null
    state.value.accessToken = null
    state.value.refreshToken = null
    state.value.accessTokenExpiresAt = null
    persist()
  }

  async function login(phoneNumber: string, password: string) {
    const api = useApi()
    const res = await api<LoginResponse>('/api/v1/auth/login', {
      method: 'POST',
      body: { phoneNumber, password },
      skipAuth: true
    })
    setSession(res)
    return res
  }

  async function register(payload: {
    fullName: string
    phoneNumber: string
    password: string
    confirmPassword: string
    role: UserRole
  }) {
    const api = useApi()
    return api<RegisterResponse>('/api/v1/auth/register', {
      method: 'POST',
      body: payload,
      skipAuth: true
    })
  }

  async function refresh() {
    if (!state.value.refreshToken) return false
    try {
      const api = useApi()
      const res = await api<{
        accessToken: string
        refreshToken: string
        accessTokenExpiresAt: string
        refreshTokenExpiresAt: string
      }>('/api/v1/auth/refresh', {
        method: 'POST',
        body: { refreshToken: state.value.refreshToken },
        skipAuth: true
      })
      state.value.accessToken = res.accessToken
      state.value.refreshToken = res.refreshToken
      state.value.accessTokenExpiresAt = res.accessTokenExpiresAt
      persist()
      return true
    } catch {
      clear()
      return false
    }
  }

  function logout() {
    clear()
    navigateTo('/')
  }

  return {
    state,
    user,
    role,
    isAuthenticated,
    isAdmin,
    isBarber,
    isCustomer,
    login,
    register,
    refresh,
    logout,
    setSession,
    clear
  }
})
