import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import ErrorAlert from '../components/ErrorAlert'
import LoadingSpinner from '../components/LoadingSpinner'
import { useAuth } from '../hooks/useAuth'
import { ApiError } from '../services/apiClient'
import * as settingsService from '../services/settingsService'
import type { DeleteAccountInfo } from '../types/settings'

/* "1 quiz is" / "2 quizzes are" */
function plural(count: number, one: string, many: string): string {
    return count === 1 ? one : many
}

/* Shows what happens before the user deletes their own account, and asks for the password */
export default function DeleteAccountPage() {
    const { logout } = useAuth()
    const navigate = useNavigate()

    const [info, setInfo] = useState<DeleteAccountInfo | null>(null)
    const [loadError, setLoadError] = useState<string | null>(null)
    const [password, setPassword] = useState('')
    const [passwordError, setPasswordError] = useState<string | null>(null)
    const [serverError, setServerError] = useState<string | null>(null)
    const [deleting, setDeleting] = useState(false)

    useEffect(() => {
        let active = true

        settingsService
            .getDeleteInfo()
            .then((data) => {
                if (active) setInfo(data)
            })
            .catch(() => {
                if (active) setLoadError('Your account information could not be loaded. Please try again.')
            })

        return () => {
            active = false
        }
    }, [])

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()
        setServerError(null)

        if (!password) {
            setPasswordError('Please enter your password.')
            return
        }

        setDeleting(true)
        try {
            await settingsService.deleteAccount(password)
            /* The API has already deleted the cookie. logout() clears the user in the app. */
            await logout()
            navigate('/', { replace: true, state: { message: 'Your account has been deleted.' } })
        } catch (err) {
            if (err instanceof ApiError && err.fieldErrors.password) {
                setPasswordError(err.fieldErrors.password[0])
            } else {
                setServerError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.')
            }
            setDeleting(false)
        }
    }

    if (loadError) {
        return <ErrorAlert message={loadError} />
    }

    if (!info) {
        return <LoadingSpinner />
    }

    /* Administrators cannot delete themselves (same rule as Subapp 1) */
    if (info.blockReason) {
        return (
            <div className="mx-auto py-4" style={{ maxWidth: 600 }}>
                <ErrorAlert message={info.blockReason} />
                <Link to="/settings" className="btn btn-outline-secondary">Back to settings</Link>
            </div>
        )
    }

    return (
        <div className="mx-auto py-4" style={{ maxWidth: 600 }}>
            <h1 className="fw-bold mb-3">Delete your account?</h1>

            <div className="card border-0 shadow-sm mb-4">
                <div className="card-body">
                    <p className="mb-2">This is what happens when your account is deleted:</p>

                    <ul className="mb-0">
                        <li>
                            <strong>{info.privateQuizzes}</strong>{' '}
                            private {plural(info.privateQuizzes, 'quiz is', 'quizzes are')} deleted.
                        </li>
                        <li>
                            <strong>{info.publicQuizzes}</strong>{' '}
                            public {plural(info.publicQuizzes, 'quiz stays', 'quizzes stay')} available for others,
                            but without a creator.
                        </li>
                        <li>
                            <strong>{info.attempts}</strong>{' '}
                            quiz {plural(info.attempts, 'attempt and', 'attempts and')}{' '}
                            <strong>{info.points}</strong> points are removed, so you leave the leaderboard.
                        </li>
                        <li>Your personal tags and settings are deleted.</li>
                    </ul>
                </div>
            </div>

            <p className="text-danger fw-semibold">This cannot be undone.</p>

            <form onSubmit={handleSubmit} noValidate>
                <ErrorAlert message={serverError} />

                <div className="mb-3">
                    <label htmlFor="password" className="form-label">Enter your password to confirm</label>
                    <input
                        id="password"
                        type="password"
                        autoComplete="current-password"
                        className={`form-control${passwordError ? ' is-invalid' : ''}`}
                        value={password}
                        onChange={(e) => {
                            setPassword(e.target.value)
                            setPasswordError(null)
                        }}
                        aria-invalid={!!passwordError}
                        aria-describedby={passwordError ? 'password-error' : undefined}
                    />
                    {passwordError && <div id="password-error" className="invalid-feedback">{passwordError}</div>}
                </div>

                <div className="d-flex gap-2">
                    <button type="submit" className="btn btn-danger" disabled={deleting}>
                        {deleting ? 'Deleting…' : 'Delete my account'}
                    </button>
                    <Link to="/settings" className="btn btn-outline-secondary">Cancel</Link>
                </div>
            </form>
        </div>
    )
}