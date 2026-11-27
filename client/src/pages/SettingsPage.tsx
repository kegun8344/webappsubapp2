import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import ErrorAlert from '../components/ErrorAlert'
import SuccessToast from '../components/SuccessToast'
import { useAuth } from '../hooks/useAuth'
import { useSettings } from '../hooks/useSettings'
import { ApiError } from '../services/apiClient'
import * as settingsService from '../services/settingsService'
import type { ChangePasswordRequest, ColorVisionMode, Settings } from '../types/settings'
import { hasErrors, validateNewPassword, type FormErrors } from '../utils/validation'

/* Same choices as Subapp 1. The API checks the value again. */
const colorVisionOptions: { value: ColorVisionMode; text: string }[] = [
    { value: 'None', text: 'Standard colours' },
    { value: 'RedGreen', text: 'Red-green colour blindness' },
    { value: 'BlueYellow', text: 'Blue-yellow colour blindness' },
]

/* Settings page, same layout as Settings/Index in Subapp 1 */
export default function SettingsPage() {
    const { settings } = useSettings()
    const { hasRole } = useAuth()
    const [message, setMessage] = useState<string | null>(null)

    /* Administrator accounts are protected: no password change and no delete (the API refuses both too) */
    const isAdmin = hasRole('Admin')

    return (
        <div className="mx-auto py-4" style={{ maxWidth: 700 }}>
            <SuccessToast message={message} onClose={() => setMessage(null)} />

            {/* Same heading style as the other pages (Settings/Index in Subapp 1) */}
            <h1 className="landing-title mb-2">Settings</h1>
            <p className="lead text-secondary mb-4">{isAdmin ? 'Your colours and motion.' : 'Your password, colours and motion.'}</p>

            {/* key: the form starts again with the saved values once they have loaded */}
            <AppearanceCard key={JSON.stringify(settings)} initial={settings} onSaved={setMessage} />
            {isAdmin ? (
                <section className="card border-0 shadow-sm" aria-labelledby="protected-heading">
                    <div className="card-body p-4 d-flex gap-3">
                        <i className="bi bi-shield-lock fs-4 text-primary" aria-hidden="true"></i>
                        <div>
                            <h2 id="protected-heading" className="h5 fw-bold mb-1">Protected account</h2>
                            <p className="text-secondary mb-0">
                                Administrator accounts are protected. The password cannot be changed and the account cannot be deleted here.
                            </p>
                        </div>
                    </div>
                </section>
            ) : (
                <>
                    <PasswordCard onSaved={setMessage} />
                    <DeleteAccountCard />
                </>
            )}
        </div>
    )
}

/* Delete account. Only a link here: the delete page explains what happens and asks for the password. */
function DeleteAccountCard() {
    return (
        <section className="card border-danger-subtle shadow-sm" aria-labelledby="delete-heading">
            <div className="card-body p-4">
                <h2 id="delete-heading" className="h5 fw-bold text-danger mb-2">
                    <i className="bi bi-exclamation-triangle me-2" aria-hidden="true"></i>Delete account
                </h2>
                <p className="text-secondary mb-3">
                    Your private quizzes, attempts and points are deleted. Public quizzes you made stay available for others.
                    This cannot be undone.
                </p>
                <Link to="/settings/delete" className="btn btn-outline-danger">Delete my account</Link>
            </div>
        </section>
    )
}

/* Dark mode, reduce motion and colour vision. Saved on their own, separate from the password form. */
function AppearanceCard({ initial, onSaved }: { initial: Settings; onSaved: (message: string) => void }) {
    const { saveSettings } = useSettings()
    const [form, setForm] = useState<Settings>(initial)
    const [error, setError] = useState<string | null>(null)
    const [saving, setSaving] = useState(false)

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()
        setError(null)
        setSaving(true)
        try {
            await saveSettings(form)
            onSaved('Settings saved.')
        } catch (err) {
            setError(err instanceof ApiError ? err.message : 'Your settings could not be saved. Please try again.')
        } finally {
            setSaving(false)
        }
    }

    return (
        <section className="card border-0 shadow-sm mb-4" aria-labelledby="appearance-heading">
            <div className="card-body p-4">
                <h2 id="appearance-heading" className="h5 fw-bold mb-3">
                    <i className="bi bi-palette me-2" aria-hidden="true"></i>Appearance and accessibility
                </h2>

                <form onSubmit={handleSubmit} noValidate>
                    <ErrorAlert message={error} onClose={() => setError(null)} />

                    <div className="form-check form-switch mb-3">
                        <input
                            id="dark-mode"
                            type="checkbox"
                            role="switch"
                            className="form-check-input"
                            checked={form.darkModeEnabled}
                            onChange={(e) => setForm({ ...form, darkModeEnabled: e.target.checked })}
                        />
                        <label htmlFor="dark-mode" className="form-check-label">Dark mode</label>
                    </div>

                    <div className="form-check form-switch mb-1">
                        <input
                            id="reduce-motion"
                            type="checkbox"
                            role="switch"
                            className="form-check-input"
                            aria-describedby="reduce-motion-help"
                            checked={form.reduceMotion}
                            onChange={(e) => setForm({ ...form, reduceMotion: e.target.checked })}
                        />
                        <label htmlFor="reduce-motion" className="form-check-label">Reduce motion</label>
                    </div>
                    <p id="reduce-motion-help" className="text-secondary small mb-3">
                        Turns off animations, for example the score ring and page transitions.
                    </p>

                    <fieldset className="mb-4">
                        <legend className="form-label fs-6 mb-1">Colour vision</legend>

                        {colorVisionOptions.map((option) => (
                            <div className="form-check" key={option.value}>
                                <input
                                    id={`color-vision-${option.value}`}
                                    type="radio"
                                    name="colorVisionMode"
                                    className="form-check-input"
                                    value={option.value}
                                    checked={form.colorVisionMode === option.value}
                                    onChange={() => setForm({ ...form, colorVisionMode: option.value })}
                                />
                                <label htmlFor={`color-vision-${option.value}`} className="form-check-label">
                                    {option.text}
                                </label>
                            </div>
                        ))}
                    </fieldset>

                    <button type="submit" className="btn btn-primary" disabled={saving}>
                        {saving && <span className="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>}
                        {saving ? 'Saving…' : 'Save settings'}
                    </button>
                </form>
            </div>
        </section>
    )
}

const emptyPassword: ChangePasswordRequest = { currentPassword: '', newPassword: '', confirmPassword: '' }

/* Checked here first for quick feedback, and again by ChangePasswordDto in the API */
function validatePassword(form: ChangePasswordRequest): FormErrors<ChangePasswordRequest> {
    return {
        currentPassword: form.currentPassword ? undefined : 'Please enter your current password.',
        newPassword: validateNewPassword(form.newPassword) ?? undefined,
        confirmPassword: !form.confirmPassword
            ? 'Please confirm your new password.'
            : form.confirmPassword !== form.newPassword
                ? 'Passwords do not match.'
                : undefined,
    }
}

/* Change password, same fields and messages as Subapp 1 */
function PasswordCard({ onSaved }: { onSaved: (message: string) => void }) {
    const [form, setForm] = useState<ChangePasswordRequest>(emptyPassword)
    const [errors, setErrors] = useState<FormErrors<ChangePasswordRequest>>({})
    const [serverError, setServerError] = useState<string | null>(null)
    const [saving, setSaving] = useState(false)

    function update(field: keyof ChangePasswordRequest, value: string) {
        setForm((current) => ({ ...current, [field]: value }))
        /* The error goes away as soon as the user starts fixing the field */
        setErrors((current) => ({ ...current, [field]: undefined }))
    }

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()
        setServerError(null)

        const found = validatePassword(form)
        setErrors(found)
        if (hasErrors(found)) return

        setSaving(true)
        try {
            await settingsService.changePassword(form)
            setForm(emptyPassword)
            onSaved('Your password has been changed.')
        } catch (err) {
            if (err instanceof ApiError) {
                /* Errors for one field go under that field, Identity's errors (e.g. "Incorrect password.") at the top */
                const fields = err.fieldErrors
                setErrors({
                    currentPassword: fields.currentPassword?.[0],
                    newPassword: fields.newPassword?.[0],
                    confirmPassword: fields.confirmPassword?.[0],
                })
                setServerError(fields['']?.join(' ') ?? (Object.keys(fields).length ? null : err.message))
            } else {
                setServerError('Something went wrong. Please try again.')
            }
        } finally {
            setSaving(false)
        }
    }

    /* One password field, same markup as RegisterPage */
    function passwordField(name: keyof ChangePasswordRequest, label: string, autoComplete: string, help?: string) {
        const error = errors[name]

        return (
            <div className="mb-3">
                <label htmlFor={name} className="form-label">{label}</label>
                <input
                    id={name}
                    type="password"
                    autoComplete={autoComplete}
                    className={`form-control${error ? ' is-invalid' : ''}`}
                    value={form[name]}
                    onChange={(e) => update(name, e.target.value)}
                    aria-invalid={!!error}
                    aria-describedby={error ? `${name}-error` : undefined}
                />
                {help && <div className="form-text">{help}</div>}
                {error && <div id={`${name}-error`} className="invalid-feedback d-block">{error}</div>}
            </div>
        )
    }

    return (
        <section className="card border-0 shadow-sm mb-4" aria-labelledby="password-heading">
            <div className="card-body p-4">
                <h2 id="password-heading" className="h5 fw-bold mb-3">
                    <i className="bi bi-key me-2" aria-hidden="true"></i>Change password
                </h2>

                <form onSubmit={handleSubmit} noValidate>
                    <ErrorAlert message={serverError} onClose={() => setServerError(null)} />

                    {passwordField('currentPassword', 'Current password', 'current-password')}
                    {passwordField('newPassword', 'New password', 'new-password',
                        'At least 8 characters, with an uppercase letter, a lowercase letter, a number and a symbol.')}
                    {passwordField('confirmPassword', 'Confirm new password', 'new-password')}

                    <button type="submit" className="btn btn-primary" disabled={saving}>
                        {saving && <span className="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>}
                        {saving ? 'Changing…' : 'Change password'}
                    </button>
                </form>
            </div>
        </section>
    )
}