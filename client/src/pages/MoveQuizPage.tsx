import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import ErrorAlert from '../components/ErrorAlert'
import LoadingSpinner from '../components/LoadingSpinner'
import { ApiError } from '../services/apiClient'
import { getMoveInfo, moveQuiz } from '../services/adminService'
import type { MoveQuizInfo } from '../types/admin'

interface LoadedInfo {
    id: number | null
    info: MoveQuizInfo | null
    error: string | null
}

/* Change teacher: gives a quiz to another teacher, the same as QuizTeacher/Move in Subapp 1. Administrators only. */
export default function MoveQuizPage() {
    const quizId = Number(useParams().id)
    const navigate = useNavigate()

    const [loaded, setLoaded] = useState<LoadedInfo>({ id: null, info: null, error: null })
    const [teacherId, setTeacherId] = useState('')
    const [fieldError, setFieldError] = useState<string | null>(null)
    const [saveError, setSaveError] = useState<string | null>(null)
    const [saving, setSaving] = useState(false)

    const isLoading = loaded.id !== quizId

    useEffect(() => {
        let active = true

        getMoveInfo(quizId)
            .then((info) => {
                if (active) setLoaded({ id: quizId, info, error: null })
            })
            .catch((err: unknown) => {
                if (!active) return

                const error = err instanceof ApiError && err.status === 404
                    ? 'This quiz was not found.'
                    : 'The teachers could not be loaded. Please try again.'

                setLoaded({ id: quizId, info: null, error })
            })

        return () => {
            active = false
        }
    }, [quizId])

    async function onSubmit(event: FormEvent<HTMLFormElement>, info: MoveQuizInfo) {
        event.preventDefault()
        setSaveError(null)

        /* Checked here for a quick answer, and again on the server */
        const teacher = info.teachers.find((t) => t.id === teacherId)
        if (!teacher) {
            setFieldError('Choose a teacher.')
            return
        }

        setFieldError(null)
        setSaving(true)

        try {
            await moveQuiz(info.quizId, teacher.id)
            navigate(`/quizzes/${info.quizId}/edit`, { state: { message: `"${info.title}" now belongs to ${teacher.email}.` } })
        } catch (err) {
            if (err instanceof ApiError && err.fieldErrors.teacherId) {
                setFieldError(err.fieldErrors.teacherId[0])
            } else {
                setSaveError(err instanceof ApiError ? err.message : 'The quiz could not be moved. Please try again.')
            }
            setSaving(false)
        }
    }

    if (isLoading) {
        return <LoadingSpinner />
    }

    const info = loaded.info

    if (!info) {
        return (
            <div className="mx-auto text-center py-5" style={{ maxWidth: 520 }}>
                <ErrorAlert message={loaded.error} />
                <Link to="/" className="btn btn-outline-primary">Back to Home</Link>
            </div>
        )
    }

    return (
        <div className="mx-auto" style={{ maxWidth: 700 }}>
            <Link to={`/quizzes/${info.quizId}/edit`} className="btn btn-outline-secondary d-inline-flex align-items-center gap-2 mb-3">
                <i className="bi bi-arrow-left" aria-hidden="true"></i>
                Back to the quiz
            </Link>

            <h1 className="landing-title mb-2">Change teacher</h1>
            <p className="lead text-secondary mb-4">{info.title}</p>

            {saveError && <ErrorAlert message={saveError} onClose={() => setSaveError(null)} />}

            <div className="card border-0 shadow-sm">
                <div className="card-body p-4">
                    <p className="mb-4">
                        Current teacher:{' '}
                        {info.currentTeacherEmail ? (
                            <strong>{info.currentTeacherEmail}</strong>
                        ) : (
                            <span className="badge bg-warning-subtle text-warning-emphasis border border-warning-subtle">No teacher</span>
                        )}
                    </p>

                    {info.teachers.length === 0 ? (
                        <p className="text-secondary mb-0">There are no other teachers to move the quiz to.</p>
                    ) : (
                        <form onSubmit={(event) => void onSubmit(event, info)} noValidate>
                            <div className="mb-3">
                                <label htmlFor="teacher-id" className="form-label">New teacher</label>
                                <select
                                    id="teacher-id"
                                    className={`form-select ${fieldError ? 'is-invalid' : ''}`}
                                    value={teacherId}
                                    aria-describedby={fieldError ? 'teacher-id-error' : undefined}
                                    onChange={(event) => {
                                        setTeacherId(event.target.value)
                                        setFieldError(null)
                                    }}
                                >
                                        <option value="" disabled hidden>Choose a teacher</option>
                                    {info.teachers.map((teacher) => (
                                        <option key={teacher.id} value={teacher.id}>{teacher.email}</option>
                                    ))}
                                </select>
                                {fieldError && <div id="teacher-id-error" className="invalid-feedback">{fieldError}</div>}
                            </div>

                            <p className="small text-secondary">The new teacher can edit and delete the quiz. Questions, attempts and points stay with the quiz.</p>

                            <button type="submit" className="btn btn-primary" disabled={saving}>
                                {saving ? 'Moving...' : 'Move quiz'}
                            </button>
                        </form>
                    )}
                </div>
            </div>
        </div>
    )
}