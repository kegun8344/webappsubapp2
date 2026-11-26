import { useState, type FormEvent } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import { ApiError } from '../services/apiClient'
import type { LoginRequest } from '../types/auth'
import { hasErrors, validateEmail, type FormErrors } from '../utils/validation'

/* Checks the form before it is sent. The API checks it again. */
function validate(form: LoginRequest): FormErrors<LoginRequest> {
    return {
        email: validateEmail(form.email) ?? undefined,
        password: form.password ? undefined : 'Please enter your password.',
    }
}

/* Log in page, same layout and texts as Account/Login in Subapp 1 */
export default function LoginPage() {
    const { user, isLoading, login } = useAuth()
    const navigate = useNavigate()
    const location = useLocation()

    const [form, setForm] = useState<LoginRequest>({ email: '', password: '' })
    const [errors, setErrors] = useState<FormErrors<LoginRequest>>({})
    const [serverError, setServerError] = useState<string | null>(null)
    const [submitting, setSubmitting] = useState(false)

    /* The page the user tried to open before being sent here, otherwise Home */
    const returnTo = (location.state as { from?: string } | null)?.from ?? '/'

    /* Already signed in: nothing to do here */
    if (!isLoading && user) {
        return <Navigate to={returnTo} replace />
    }

    function update(field: keyof LoginRequest, value: string) {
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
            await login({ email: form.email.trim(), password: form.password })
            navigate(returnTo, { replace: true })
        } catch (err) {
            /* E.g. "Invalid email or password." or "Account temporarily locked." from the API */
            setServerError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.')
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

                <h1 className="h3 mb-3">Log in</h1>

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

                    <div className="mb-3">
                        <label htmlFor="password" className="form-label">Password</label>
                        <input
                            id="password"
                            type="password"
                            className={`form-control${errors.password ? ' is-invalid' : ''}`}
                            value={form.password}
                            onChange={(e) => update('password', e.target.value)}
                            autoComplete="current-password"
                            aria-invalid={!!errors.password}
                            aria-describedby={errors.password ? 'password-error' : undefined}
                        />
                        {errors.password && <div id="password-error" className="invalid-feedback">{errors.password}</div>}
                    </div>

                    <button type="submit" className="btn btn-primary" disabled={submitting}>
                        {submitting && <span className="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>}
                        {submitting ? 'Logging in…' : 'Log in'}
                    </button>

                    <p className="text-secondary mt-3 mb-0">
                        Don't have an account?{' '}
                        <Link to="/register" className="fw-semibold link-offset-2">Sign up</Link>
                    </p>
                </form>
            </div>
        </div>
    )
}