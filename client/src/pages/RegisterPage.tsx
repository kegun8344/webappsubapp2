import { useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import { ApiError } from '../services/apiClient'
import type { RegisterRequest } from '../types/auth'
import { hasErrors, validateEmail, validateNewPassword, type FormErrors } from '../utils/validation'

/* Checks the form before it is sent. The API checks it again. */
function validate(form: RegisterRequest): FormErrors<RegisterRequest> {
    return {
        email: validateEmail(form.email) ?? undefined,
        password: validateNewPassword(form.password) ?? undefined,
        confirmPassword: !form.confirmPassword
            ? 'Please confirm your password.'
            : form.confirmPassword !== form.password
                ? 'Passwords do not match.'
                : undefined,
    }
}

/* Sign up page, same layout and texts as Account/Register in Subapp 1 */
export default function RegisterPage() {
    const { user, isLoading, register } = useAuth()
    const navigate = useNavigate()

    const [form, setForm] = useState<RegisterRequest>({ email: '', password: '', confirmPassword: '' })
    const [errors, setErrors] = useState<FormErrors<RegisterRequest>>({})
    const [serverError, setServerError] = useState<string | null>(null)
    const [submitting, setSubmitting] = useState(false)

    /* Already signed in: nothing to do here */
    if (!isLoading && user) {
        return <Navigate to="/" replace />
    }

    function update(field: keyof RegisterRequest, value: string) {
        setForm((current) => ({ ...current, [field]: value }))
        /* The error goes away as soon as the user starts fixing the field */
        setErrors((current) => ({ ...current, [field]: undefined }))
    }

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()
        setServerError(null)

        const found = validate(form)
        setErrors(found)
        if (hasErrors(found)) return

        setSubmitting(true)
        try {
            await register({ ...form, email: form.email.trim() })
            /* Signed in straight away, and everyone lands on Home (same as Subapp 1) */
            navigate('/', { replace: true })
        } catch (err) {
            if (err instanceof ApiError) {
                /* Errors for one field go under that field, the rest (e.g. "Email is already taken") at the top */
                const fields = err.fieldErrors
                setErrors({
                    email: fields.email?.[0],
                    password: fields.password?.[0],
                    confirmPassword: fields.confirmPassword?.[0],
                })
                setServerError(fields['']?.join(' ') ?? (Object.keys(fields).length ? null : err.message))
            } else {
                setServerError('Something went wrong. Please try again.')
            }
        } finally {
            setSubmitting(false)
        }
    }

    return (
        <div className="row justify-content-center">
            <div className="col-md-5">
                <Link to="/" className="btn btn-outline-secondary d-inline-flex align-items-center gap-2 mb-3">
                    <i className="bi bi-arrow-left" aria-hidden="true"></i>
                    Back
                </Link>

                <h1 className="h3 mb-3">Sign up</h1>

                {/* noValidate: our own messages are shown instead of the browser's */}
                <form onSubmit={handleSubmit} noValidate>
                    {serverError && (
                        <div className="alert alert-danger" role="alert">
                            {serverError}
                        </div>
                    )}

                    <div className="mb-3">
                        <label htmlFor="email" className="form-label">Email</label>
                        <input
                            id="email"
                            type="email"
                            className={`form-control${errors.email ? ' is-invalid' : ''}`}
                            value={form.email}
                            onChange={(e) => update('email', e.target.value)}
                            autoComplete="email"
                            aria-invalid={!!errors.email}
                            aria-describedby={errors.email ? 'email-error' : undefined}
                        />
                        {errors.email && <div id="email-error" className="invalid-feedback">{errors.email}</div>}
                    </div>

                    {/* All new accounts are students. Only an administrator can make someone a teacher. */}
                    <p className="text-secondary small mb-3">
                        New accounts are created as students. If you are a teacher, please contact your administrator or IT-department.
                    </p>

                    <div className="mb-3">
                        <label htmlFor="password" className="form-label">Password</label>
                        <input
                            id="password"
                            type="password"
                            className={`form-control${errors.password ? ' is-invalid' : ''}`}
                            value={form.password}
                            onChange={(e) => update('password', e.target.value)}
                            autoComplete="new-password"
                            aria-invalid={!!errors.password}
                            aria-describedby={errors.password ? 'password-help password-error' : 'password-help'}
                        />
                        <div id="password-help" className="form-text">
                            At least 8 characters, with an uppercase letter, a lowercase letter, a number and a symbol.
                        </div>
                        {errors.password && <div id="password-error" className="invalid-feedback d-block">{errors.password}</div>}
                    </div>

                    <div className="mb-3">
                        <label htmlFor="confirmPassword" className="form-label">Confirm password</label>
                        <input
                            id="confirmPassword"
                            type="password"
                            className={`form-control${errors.confirmPassword ? ' is-invalid' : ''}`}
                            value={form.confirmPassword}
                            onChange={(e) => update('confirmPassword', e.target.value)}
                            autoComplete="new-password"
                            aria-invalid={!!errors.confirmPassword}
                            aria-describedby={errors.confirmPassword ? 'confirm-error' : undefined}
                        />
                        {errors.confirmPassword && <div id="confirm-error" className="invalid-feedback">{errors.confirmPassword}</div>}
                    </div>

                    <button type="submit" className="btn btn-primary" disabled={submitting}>
                        {submitting && <span className="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>}
                        {submitting ? 'Signing up…' : 'Sign up'}
                    </button>

                    <p className="text-secondary mt-3 mb-0">
                        Already have an account?{' '}
                        <Link to="/login" className="fw-semibold link-offset-2">Log in</Link>
                    </p>
                </form>
            </div>
        </div>
    )
}