import { describe, expect, it } from 'vitest'
import { normalizeError } from '~/composables/useApi'

describe('normalizeError', () => {
  it('joins field errors into a single Persian message', () => {
    const error = {
      response: {
        status: 400,
        _data: {
          title: 'خطای اعتبارسنجی',
          errors: {
            Email: ['فرمت ایمیل نامعتبر است'],
            Password: ['رمز عبور الزامی است', 'رمز عبور باید حداقل ۸ کاراکتر باشد']
          }
        }
      }
    }

    const result = normalizeError(error)

    expect(result.status).toBe(400)
    expect(result.message).toContain('فرمت ایمیل نامعتبر است')
    expect(result.message).toContain('رمز عبور الزامی است')
    // Multiple messages for one field are joined with the Persian comma.
    expect(result.message).toContain('،')
  })

  it('falls back to the problem title when there are no field errors', () => {
    const error = {
      response: {
        status: 409,
        _data: { title: 'این بازه زمانی قبلاً رزرو شده است', code: 'SLOT_ALREADY_BOOKED' }
      }
    }

    const result = normalizeError(error)

    expect(result.status).toBe(409)
    expect(result.message).toBe('این بازه زمانی قبلاً رزرو شده است')
    expect(result.details?.code).toBe('SLOT_ALREADY_BOOKED')
  })

  it('uses the detail field when no title is present', () => {
    const result = normalizeError({ response: { status: 500, _data: { detail: 'جزئیات خطا' } } })

    expect(result.message).toBe('جزئیات خطا')
  })

  it('returns a generic Persian message for unknown failures', () => {
    const result = normalizeError(new Error('boom'))

    expect(result.message).toBe('خطایی رخ داد. لطفاً دوباره تلاش کنید.')
    expect(result.status).toBeUndefined()
  })

  it('reads the status from a plain status property too', () => {
    const result = normalizeError({ status: 401, data: { title: 'دسترسی غیرمجاز' } })

    expect(result.status).toBe(401)
    expect(result.message).toBe('دسترسی غیرمجاز')
  })

  it('ignores an empty errors object', () => {
    const result = normalizeError({ response: { status: 400, _data: { title: 'نامعتبر', errors: {} } } })

    expect(result.message).toBe('نامعتبر')
  })

  it('always returns an Error instance', () => {
    expect(normalizeError(undefined)).toBeInstanceOf(Error)
    expect(normalizeError(null)).toBeInstanceOf(Error)
  })
})
