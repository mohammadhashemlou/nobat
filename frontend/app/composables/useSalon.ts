/**
 * MVP runs a single salon. This resolves the salon slug to an id once and
 * caches it for the session so panel pages can use it.
 */
export function useSalon() {
  const config = useRuntimeConfig()
  const api = useApi()

  const salonId = useState<string | null>('salon_id', () => null)
  const salon = useState<PublicSalon | null>('salon_data', () => null)

  async function ensure() {
    if (salonId.value && salon.value) return salon.value
    const data = await api<PublicSalon>(`/api/v1/salons/${config.public.salonSlug}`, {
      skipAuth: true
    })
    salon.value = data
    salonId.value = data.id
    return data
  }

  return { salonId, salon, ensure }
}
