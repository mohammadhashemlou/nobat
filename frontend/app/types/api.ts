export type UserRole = 'Admin' | 'Barber' | 'Customer'

export type AppointmentStatus =
  | 'Pending'
  | 'Confirmed'
  | 'Completed'
  | 'Cancelled'
  | 'NoShow'

export interface UserInfo {
  id: string
  email: string
  fullName: string
  role: UserRole
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAt: string
  refreshTokenExpiresAt: string
  user: UserInfo
}

export interface RegisterResponse {
  userId: string
  email: string
  fullName: string
  role: UserRole
}

export interface PublicService {
  id: string
  name: string
  description?: string | null
  durationMinutes: number
  price: number
}

export interface PublicBarber {
  id: string
  fullName: string
  bio?: string | null
  avatarUrl?: string | null
  serviceIds: string[]
}

export interface PublicSalon {
  id: string
  name: string
  slug: string
  address?: string | null
  phone?: string | null
  description?: string | null
  services: PublicService[]
  barbers: PublicBarber[]
}

export interface TimeSlot {
  startUtc: string
  endUtc: string
  localStart: string
  localEnd: string
}

export interface Availability {
  barberId: string
  serviceId: string
  date: string
  isHoliday: boolean
  holidayDescription?: string | null
  isWorkingDay: boolean
  slots: TimeSlot[]
}

export interface Appointment {
  id: string
  salonId: string
  barberId: string
  barberName: string
  serviceId: string
  serviceName: string
  customerId: string
  customerName: string
  customerPhone?: string | null
  startAt: string
  endAt: string
  status: AppointmentStatus
  notes?: string | null
  price: number
  durationMinutes: number
}

export interface ServiceAdmin {
  id: string
  salonId: string
  name: string
  description?: string | null
  durationMinutes: number
  price: number
  isActive: boolean
  barberIds: string[]
}

export interface BarberAdmin {
  id: string
  userId: string
  salonId: string
  fullName: string
  email?: string | null
  phoneNumber?: string | null
  bio?: string | null
  avatarUrl?: string | null
  isActive: boolean
  serviceIds: string[]
}

export interface WorkingHours {
  id: string
  barberId: string
  dayOfWeek: string
  dayOfWeekValue: number
  startTime: string
  endTime: string
  breakStart?: string | null
  breakEnd?: string | null
}

export interface Holiday {
  id: string
  date: string
  description?: string | null
}

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  code?: string
  errors?: Record<string, string[]>
}
