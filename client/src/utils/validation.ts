/*
 * The same rules as the DTOs in the API (RegisterDto, LoginDto) and as Subapp 1.
 * Checked here first for quick feedback, and always again on the server.
 */

/* Requires a dot in the domain, so "a@b" is not accepted (same regex as RegisterDto) */
const emailPattern = /^[^@\s]+@[^@\s]+\.[^@\s]+$/

/* At least 8 characters, with upper and lower case, a number and a symbol (same as Identity in Program.cs) */
const passwordPattern = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$/

export function validateEmail(email: string): string | null {
    if (!email.trim()) return 'Please enter your email.'
    if (!emailPattern.test(email.trim())) return 'Please enter a valid email address.'
    return null
}

export function validateNewPassword(password: string): string | null {
    if (!password) return 'Please choose a password.'
    if (password.length < 8) return 'The password must be at least 8 characters.'
    if (!passwordPattern.test(password)) return 'The password needs an uppercase letter, a lowercase letter, a number and a symbol.'
    return null
}

/* An object with only the fields that have an error. Empty means the form is valid. */
export type FormErrors<T> = Partial<Record<keyof T, string>>

export function hasErrors<T>(errors: FormErrors<T>): boolean {
    return Object.values(errors).some(Boolean)
}