import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ApiError } from '../services/apiClient'
import { difficultyLevels, type ModuleOption } from '../types/quiz'
import { validateQuiz, type QuizFormErrors, type QuizFormValues } from '../utils/quizValidation'

interface QuizDetailsFormProps {
    initial: QuizFormValues
    modules: ModuleOption[]
    /* "create": Create and Cancel buttons. "edit": Save and Discard appear only when something has changed. */
    mode: 'create' | 'edit'
    /* Saves the values. Throws an ApiError if the server says no. */
    onSubmit: (values: QuizFormValues) => Promise<void>
}

/* The quiz form, used by both Create quiz and Edit quiz (same fields and texts as Subapp 1) */
export default function QuizDetailsForm({ initial, modules, mode, onSubmit }: QuizDetailsFormProps) {
    const [values, setValues] = useState<QuizFormValues>(initial)
    const [errors, setErrors] = useState<QuizFormErrors>({})
    const [serverError, setServerError] = useState<string | null>(null)
    const [submitting, setSubmitting] = useState(false)

    /* On the edit page the buttons only appear after a change, like in Subapp 1 */
    const isDirty = (Object.keys(initial) as (keyof QuizFormValues)[]).some((field) => values[field] !== initial[field])

    function update(field: keyof QuizFormValues, value: string) {
        setValues((current) => ({ ...current, [field]: value }))
        setErrors((current) => ({ ...current, [field]: undefined }))
    }

    function discard() {
        setValues(initial)
        setErrors({})
        setServerError(null)
    }

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()
        setServerError(null)

        const found = validateQuiz(values)
        setErrors(found)
        if (Object.values(found).some(Boolean)) return

        setSubmitting(true)
        try {
            await onSubmit(values)
        } catch (err) {
            if (err instanceof ApiError) {
                /* Errors per field from the server, e.g. "Choose a valid module." */
                setErrors({
                    title: err.fieldErrors.title?.[0],
                    description: err.fieldErrors.description?.[0],
                    moduleId: err.fieldErrors.moduleId?.[0],
                    difficulty: err.fieldErrors.difficulty?.[0],
                })
                setServerError(err.message)
            } else {
                setServerError('Something went wrong. Please try again.')
            }
        } finally {
            setSubmitting(false)
        }
    }

    /* Props shared by the inputs, so the red border and the error text stay linked */
    function fieldProps(field: keyof QuizFormValues) {
        return {
            id: `quiz-${field}`,
            'aria-invalid': Boolean(errors[field]),
            'aria-describedby': errors[field] ? `quiz-${field}-error` : undefined,
        }
    }

    function errorText(field: keyof QuizFormValues) {
        return errors[field] && <div id={`quiz-${field}-error`} className="invalid-feedback">{errors[field]}</div>
    }

    const showActions = mode === 'create' || isDirty

    return (
        <form onSubmit={handleSubmit} noValidate>
            {serverError && (
                <div className="alert alert-danger" role="alert">{serverError}</div>
            )}

            <div className="mb-3">
                <label htmlFor="quiz-title" className="form-label">Title</label>
                <input
                    {...fieldProps('title')}
                    className={`form-control${errors.title ? ' is-invalid' : ''}`}
                    maxLength={100}
                    value={values.title}
                    onChange={(event) => update('title', event.target.value)}
                />
                {errorText('title')}
            </div>

            <div className="mb-3">
                <label htmlFor="quiz-description" className="form-label">Description</label>
                <textarea
                    {...fieldProps('description')}
                    className={`form-control${errors.description ? ' is-invalid' : ''}`}
                    rows={4}
                    maxLength={500}
                    value={values.description}
                    onChange={(event) => update('description', event.target.value)}
                />
                {errorText('description')}
            </div>

            <div className="mb-3">
                <label htmlFor="quiz-moduleId" className="form-label">Module</label>
                <select
                    {...fieldProps('moduleId')}
                    className={`form-select${errors.moduleId ? ' is-invalid' : ''}`}
                    value={values.moduleId}
                    onChange={(event) => update('moduleId', event.target.value)}
                >
                    <option value="">Choose a module</option>
                    {modules.map((module) => (
                        <option key={module.id} value={module.id}>{module.name}</option>
                    ))}
                </select>
                {errorText('moduleId')}
            </div>

            <div className="mb-3">
                <label htmlFor="quiz-difficulty" className="form-label">Difficulty</label>
                <select
                    {...fieldProps('difficulty')}
                    className={`form-select${errors.difficulty ? ' is-invalid' : ''}`}
                    value={values.difficulty}
                    onChange={(event) => update('difficulty', event.target.value)}
                >
                    {difficultyLevels.map((level) => (
                        <option key={level} value={level}>{level}</option>
                    ))}
                </select>
                {errorText('difficulty')}
            </div>

            {/* The same sliding bar as Subapp 1 (details-actions in site.css) */}
            <div className={`details-actions${showActions ? ' is-open' : ''}`}>
                <div className="details-actions-inner">
                    <div className="details-actions-content d-flex flex-wrap align-items-center gap-2">
                        <button type="submit" className="btn btn-primary" disabled={submitting}>
                            {submitting && <span className="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>}
                            {mode === 'create' ? 'Create quiz' : 'Save changes'}
                        </button>

                        {mode === 'create' ? (
                            <Link to="/quizzes" className="btn btn-outline-secondary">Cancel</Link>
                        ) : (
                            <>
                                <button type="button" className="btn btn-outline-secondary" onClick={discard} disabled={submitting}>
                                    Discard changes
                                </button>
                                <span className="text-secondary small">You have unsaved changes.</span>
                            </>
                        )}
                    </div>
                </div>
            </div>

            {/* Tells screen reader users when the buttons appear */}
            <p className="visually-hidden" aria-live="polite">
                {mode === 'edit' && isDirty ? 'You have unsaved changes.' : ''}
            </p>
        </form>
    )
}