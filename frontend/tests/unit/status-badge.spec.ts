import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import StatusBadge from '~/components/StatusBadge.vue'

const UBadgeStub = {
  props: ['color', 'variant', 'size'],
  template: '<span :data-color="color"><slot /></span>'
}

function render(status: string) {
  return mount(StatusBadge, {
    props: { status },
    global: { stubs: { UBadge: UBadgeStub } }
  })
}

describe('StatusBadge', () => {
  it.each([
    ['Pending', 'در انتظار', 'warning'],
    ['Confirmed', 'تأیید شده', 'success'],
    ['Completed', 'انجام شده', 'info'],
    ['Cancelled', 'لغو شده', 'error'],
    ['NoShow', 'عدم حضور', 'neutral']
  ])('renders %s as "%s" with the %s colour', (status, label, color) => {
    const wrapper = render(status)

    expect(wrapper.text()).toBe(label)
    expect(wrapper.find('span').attributes('data-color')).toBe(color)
  })

  it('falls back to the raw value for an unknown status', () => {
    const wrapper = render('SomethingNew')

    expect(wrapper.text()).toBe('SomethingNew')
    expect(wrapper.find('span').attributes('data-color')).toBe('neutral')
  })
})
