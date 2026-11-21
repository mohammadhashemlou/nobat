<script setup lang="ts">
const props = withDefaults(
  defineProps<{
    modelValue?: string | null
    /** ISO dates (YYYY-MM-DD) that cannot be selected (holidays, closed days). */
    disabledDates?: string[]
    /** Minimum selectable ISO date. Defaults to today. */
    minDate?: string
    /** Iranian weekday indexes that are selectable (0=Sat..6=Fri). Empty = all. */
    availableWeekdays?: number[]
  }>(),
  { disabledDates: () => [], availableWeekdays: () => [] }
)

const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const {
  IRANIAN_WEEKDAYS_SHORT,
  JALALI_MONTHS,
  persianDigits,
  todayApiDate,
  parts,
  daysInJalaliMonth,
  firstDowOfJalaliMonth,
  jalaliToGregorian,
  toApiDate,
  toIranianDow
} = useJalali()

const minIso = computed(() => props.minDate || todayApiDate())

const initial = computed(() => parts(props.modelValue || new Date()))
const viewYear = ref(initial.value.year)
const viewMonth = ref(initial.value.month)

watch(
  () => props.modelValue,
  (v) => {
    if (!v) return
    const p = parts(v)
    viewYear.value = p.year
    viewMonth.value = p.month
  }
)

const daysInMonth = computed(() => daysInJalaliMonth(viewYear.value, viewMonth.value))
const leadingBlanks = computed(() => firstDowOfJalaliMonth(viewYear.value, viewMonth.value))

interface Cell {
  day: number
  iso: string
  disabled: boolean
  isToday: boolean
  selected: boolean
}

const cells = computed<Cell[]>(() => {
  const list: Cell[] = []
  const today = todayApiDate()
  const disabledSet = new Set(props.disabledDates)
  const weekdays = props.availableWeekdays

  for (let day = 1; day <= daysInMonth.value; day++) {
    const g = jalaliToGregorian(viewYear.value, viewMonth.value, day)
    const iso = toApiDate(g)
    const dow = toIranianDow(new Date(`${iso}T00:00:00+03:30`).getDay())

    let disabled = iso < minIso.value || disabledSet.has(iso)
    if (weekdays.length && !weekdays.includes(dow)) disabled = true

    list.push({
      day,
      iso,
      disabled,
      isToday: iso === today,
      selected: iso === props.modelValue
    })
  }
  return list
})

function prevMonth() {
  if (viewMonth.value === 1) {
    viewMonth.value = 12
    viewYear.value--
  } else {
    viewMonth.value--
  }
}

function nextMonth() {
  if (viewMonth.value === 12) {
    viewMonth.value = 1
    viewYear.value++
  } else {
    viewMonth.value++
  }
}

const canGoPrev = computed(() => {
  const p = parts(minIso.value)
  return viewYear.value > p.year || (viewYear.value === p.year && viewMonth.value > p.month)
})

function select(cell: Cell) {
  if (cell.disabled) return
  emit('update:modelValue', cell.iso)
}
</script>

<template>
  <div class="panel p-4">
    <div class="mb-3 flex items-center justify-between">
      <UButton
        variant="ghost"
        color="neutral"
        size="sm"
        icon="i-lucide-chevron-right"
        :disabled="!canGoPrev"
        aria-label="ماه قبل"
        @click="prevMonth"
      />
      <p class="text-sm font-bold">
        {{ JALALI_MONTHS[viewMonth - 1] }} {{ persianDigits(viewYear) }}
      </p>
      <UButton
        variant="ghost"
        color="neutral"
        size="sm"
        icon="i-lucide-chevron-left"
        aria-label="ماه بعد"
        @click="nextMonth"
      />
    </div>

    <div class="mb-1 grid grid-cols-7 gap-1">
      <span
        v-for="(w, i) in IRANIAN_WEEKDAYS_SHORT"
        :key="i"
        class="grid h-8 place-items-center text-xs font-medium text-muted"
      >
        {{ w }}
      </span>
    </div>

    <div class="grid grid-cols-7 gap-1">
      <span v-for="n in leadingBlanks" :key="`b-${n}`" />

      <button
        v-for="cell in cells"
        :key="cell.iso"
        type="button"
        :disabled="cell.disabled"
        class="grid h-10 place-items-center rounded-lg text-sm font-medium tabular-nums transition"
        :class="[
          cell.selected
            ? 'bg-brand-600 text-white shadow-sm dark:bg-brand-500'
            : cell.disabled
              ? 'cursor-not-allowed text-gray-300 dark:text-gray-600'
              : 'text-gray-700 hover:bg-brand-50 hover:text-brand-700 dark:text-gray-200 dark:hover:bg-brand-500/10 dark:hover:text-brand-300',
          cell.isToday && !cell.selected ? 'ring-1 ring-brand-400 ring-inset' : ''
        ]"
        @click="select(cell)"
      >
        {{ persianDigits(cell.day) }}
      </button>
    </div>
  </div>
</template>
