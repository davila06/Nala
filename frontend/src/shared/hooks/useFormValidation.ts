import { useState, useCallback } from 'react'

type FieldRule<T> = {
  required?: boolean | string
  minLength?: { value: number; message: string }
  maxLength?: { value: number; message: string }
  pattern?:  { value: RegExp; message: string }
  validate?: (val: string, values: T) => string | undefined
}

type FieldRules<T> = { [K in keyof T]?: FieldRule<T> }
type FieldErrors<T> = { [K in keyof T]?: string }

/**
 * useFormValidation — lightweight inline validation hook.
 *
 * Usage:
 *   const { errors, validate, validateField, isValid } = useFormValidation(values, rules)
 *   onChange handler: validateField('email', value)
 *   onSubmit: if (!validate()) return
 */
export function useFormValidation<T extends Record<string, string>>(
  values: T,
  rules: FieldRules<T>,
) {
  const [errors, setErrors] = useState<FieldErrors<T>>({})
  const [touched, setTouched] = useState<Partial<Record<keyof T, boolean>>>({})

  const validateOne = useCallback((field: keyof T, val: string): string | undefined => {
    const rule = rules[field]
    if (!rule) return undefined

    if (rule.required && !val.trim()) {
      return typeof rule.required === 'string' ? rule.required : 'Este campo es obligatorio.'
    }
    if (rule.minLength && val.length < rule.minLength.value) return rule.minLength.message
    if (rule.maxLength && val.length > rule.maxLength.value) return rule.maxLength.message
    if (rule.pattern && !rule.pattern.value.test(val)) return rule.pattern.message
    if (rule.validate) return rule.validate(val, values)
    return undefined
  }, [rules, values])

  const validateField = useCallback((field: keyof T, val: string) => {
    const err = validateOne(field, val)
    setTouched((t) => ({ ...t, [field]: true }))
    setErrors((e) => ({ ...e, [field]: err }))
    return !err
  }, [validateOne])

  const validate = useCallback((): boolean => {
    const newErrors: FieldErrors<T> = {}
    const newTouched: Partial<Record<keyof T, boolean>> = {}
    let valid = true

    for (const field of Object.keys(rules) as Array<keyof T>) {
      const err = validateOne(field, values[field] ?? '')
      newErrors[field] = err
      newTouched[field] = true
      if (err) valid = false
    }

    setErrors(newErrors)
    setTouched(newTouched)
    return valid
  }, [rules, values, validateOne])

  const clearField = useCallback((field: keyof T) => {
    setErrors((e) => ({ ...e, [field]: undefined }))
  }, [])

  const isValid = Object.values(errors).every((e) => !e)
  const isTouched = (field: keyof T) => !!touched[field]
  const showError = (field: keyof T) => touched[field] ? errors[field] : undefined

  return { errors, touched, validate, validateField, clearField, isValid, isTouched, showError }
}
