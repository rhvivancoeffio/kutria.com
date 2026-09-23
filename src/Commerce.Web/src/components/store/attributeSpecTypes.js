/** Gravity EntityAttributes SpecificationType (swagger enum 1–8). */
export const SPECIFICATION_TYPE_OPTIONS = [
  { value: 1, label: 'Texto' },
  { value: 2, label: 'Número' },
  { value: 3, label: 'Booleano' },
  { value: 4, label: 'Fecha' },
  { value: 5, label: 'Lista' },
  { value: 6, label: 'Multiselect' },
  { value: 7, label: 'Color' },
  { value: 8, label: 'Archivo' }
]

/** Lista (5) and Multiselect (6) accept option values. */
export const SPECIFICATION_TYPES_WITH_VALUES = new Set([5, 6])

export function specificationTypeLabel(type) {
  const n = Number(type)
  return SPECIFICATION_TYPE_OPTIONS.find((o) => o.value === n)?.label ?? null
}

export function specificationTypeSupportsValues(type) {
  return SPECIFICATION_TYPES_WITH_VALUES.has(Number(type))
}
