/**
 * Requires the Admin or Barber role. Customers are redirected to their own area.
 */
export default defineNuxtRouteMiddleware((to) => {
  const auth = useAuthStore()

  if (!auth.isAuthenticated) {
    return navigateTo({ path: '/login', query: { redirect: to.fullPath } })
  }

  if (!auth.isAdmin && !auth.isBarber) {
    return navigateTo('/my')
  }
})
