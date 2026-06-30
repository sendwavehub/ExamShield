import { describe, it, expect } from 'vitest'
import { cn, getInitials } from '../lib/utils'

describe('cn', () => {
  it('merges class names', () => {
    expect(cn('foo', 'bar')).toBe('foo bar')
  })

  it('deduplicates conflicting tailwind classes (last wins)', () => {
    const result = cn('p-2', 'p-4')
    expect(result).toBe('p-4')
  })

  it('handles conditional falsy values', () => {
    expect(cn('foo', false && 'bar', undefined, null, 'baz')).toBe('foo baz')
  })

  it('handles object syntax', () => {
    expect(cn({ 'text-red-500': true, 'text-blue-500': false })).toBe('text-red-500')
  })

  it('returns empty string for no inputs', () => {
    expect(cn()).toBe('')
  })
})

describe('getInitials', () => {
  it('returns first letters of two words uppercased', () => {
    expect(getInitials('John Doe')).toBe('JD')
  })

  it('returns first two chars uppercased for a single word', () => {
    expect(getInitials('Admin')).toBe('AD')
  })

  it('returns empty string for empty input', () => {
    expect(getInitials('')).toBe('')
  })

  it('handles multiple spaces between words', () => {
    expect(getInitials('Alice   Bob')).toBe('AB')
  })

  it('handles leading/trailing spaces', () => {
    expect(getInitials('  Jane  Smith  ')).toBe('JS')
  })

  it('only uses the first two words even when more are present', () => {
    expect(getInitials('Mary Jane Watson')).toBe('MJ')
  })

  it('uppercases lowercase initials', () => {
    expect(getInitials('alice bob')).toBe('AB')
  })
})
