import type { FetchOptions } from 'ofetch'
import type { ProblemDetails } from '~/types/api'

export interface ApiOptions extends FetchOptions {
  /** Skip attaching the Authorization header (login/register/public endpoints). */
  skipAuth?: boolean
  /** Do not attempt a silent token refresh on 401. */
  skipRefresh?: boolean
}

// ofetch wrapper: prefixes the API base, attaches the access token, retries once
// after a silent refresh, and turns ProblemDetails into a typed Error.
export function useApi() {
  const config = useRuntimeConfig()
  const auth = useAuthStore()

  return async function api<T>(path: string, options: ApiOptions = {}): Promise<T> {
    const { skipAuth, skipRefresh, ...fetchOptions } = options

    const headers = new Headers(fetchOptions.headers as HeadersInit | undefined)
    if (!headers.has('Content-Type') && fetchOptions.body) {
      headers.set('Content-Type', 'application/json')
    }
    if (!skipAuth && auth.state.accessToken) {
      headers.set('Authorization', `Bearer ${auth.state.accessToken}`)
    }

    const request = () =>
      $fetch<T>(path, {
        baseURL: config.public.apiBase,
        // Appointments change from several places at once (panel, customer, booking),
        // so a cached GET is always the wrong answer here.
        cache: 'no-store',
        ...(fetchOptions as Record<string, unknown>),
        headers
      } as any) as Promise<T>

    return trackRequest(async () => {
      try {
        return await request()
      } catch (error: any) {
        const status = error?.response?.status ?? error?.status

        if (status === 401 && !skipAuth && !skipRefresh && auth.state.refreshToken) {
          const refreshed = await auth.refresh()
          if (refreshed) {
            headers.set('Authorization', `Bearer ${auth.state.accessToken}`)
            return await request()
          }
        }

        throw normalizeError(error)
      }
    })
  }
}

export function normalizeError(error: any): Error & { status?: number; details?: ProblemDetails } {
  const status: number | undefined = error?.response?.status ?? error?.status
  const data = (error?.response?._data ?? error?.data ?? {}) as ProblemDetails

  const message =
    data?.errors && Object.keys(data.errors).length
      ? Object.values(data.errors).flat().join('، ')
      : data?.title || data?.detail || 'خطایی رخ داد. لطفاً دوباره تلاش کنید.'

  const normalized = new Error(message) as Error & {
    status?: number
    details?: ProblemDetails
  }
  normalized.status = status
  normalized.details = data
  return normalized
}
