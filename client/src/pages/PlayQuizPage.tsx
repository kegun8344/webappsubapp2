import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import ConfirmDialog from '../components/ConfirmDialog'
import ErrorAlert from '../components/ErrorAlert'
import LoadingSpinner from '../components/LoadingSpinner'
import QuizResultCard from '../components/QuizResultCard'
import { ApiError } from '../services/apiClient'
import { getQuizForPlay, submitAttempt } from '../services/playService'
import type { QuizPlay, QuizResult } from '../types/play'

/* The quiz loaded for one quiz ID, so an old answer is never shown for another quiz */
interface LoadedQuiz {
    id: number | null
    quiz: QuizPlay | null
    error: string | null
}

/* A friendly message for the errors the play endpoint can give */
function loadErrorMessage(err: unknown): string {
    if (err instanceof ApiError) {
        if (err.status === 404) return 'This quiz was not found, or you do not have access to it.'
        return err.message
    }
    return 'The quiz could not be loaded. Please try again.'
}

/*
 * Taking a quiz, the same as QuizPlayer/Take in Subapp 1:
 * one question per page with Back and Next, and an overview before submitting.
 * The result is shown on the same page afterwards.
 */
export default function PlayQuizPage() {
    const quizId = Number(useParams().id)

    const navigate = useNavigate()

    /* The "Leave this quiz?" popup, only shown when something has been answered */
    const [showLeave, setShowLeave] = useState(false)

    const [loaded, setLoaded] = useState<LoadedQuiz>({ id: null, quiz: null, error: null })

    /* The chosen option per question ID */
    const [answers, setAnswers] = useState<Record<number, number>>({})

    /* Which question is shown. The number of questions means the overview. */
    const [step, setStep] = useState(0)

    const [submitting, setSubmitting] = useState(false)
    const [submitError, setSubmitError] = useState<string | null>(null)
    const [result, setResult] = useState<QuizResult | null>(null)

    /* The question or the overview. Focused after Back/Next, so keyboard and screen reader users follow along. */
    const stepRef = useRef<HTMLDivElement>(null)
    const moveFocus = useRef(false)

    const validId = Number.isInteger(quizId) && quizId > 0
    const isLoading = validId && loaded.id !== quizId

    useEffect(() => {
        /* A quiz ID that is not a number cannot exist, so the API is not asked */
        if (!validId) return

        let active = true

        getQuizForPlay(quizId)
            .then((quiz) => {
                if (!active) return
                setLoaded({ id: quizId, quiz, error: null })
                setAnswers({})
                setStep(0)
            })
            .catch((err: unknown) => {
                if (active) setLoaded({ id: quizId, quiz: null, error: loadErrorMessage(err) })
            })

        return () => {
            active = false
        }
    }, [quizId, validId])

    useEffect(() => {
        if (moveFocus.current) {
            moveFocus.current = false
            stepRef.current?.focus()
        }
    }, [step])

    if (isLoading) {
        return <LoadingSpinner message="Loading quiz..." />
    }

    if (!validId || !loaded.quiz) {
        return (
            <div className="mx-auto text-center py-5" style={{ maxWidth: 520 }}>
                <ErrorAlert message={validId ? loaded.error : 'This quiz was not found, or you do not have access to it.'} />
                <Link to="/quizzes" className="btn btn-outline-primary">Back to quizzes</Link>
            </div>
        )
    }

    const quiz = loaded.quiz

    if (result) {
        return (
            <QuizResultCard
                result={result}
                onTryAgain={() => {
                    setResult(null)
                    setAnswers({})
                    setStep(0)
                    window.scrollTo(0, 0)
                }}
            />
        )
    }

    const total = quiz.questions.length
    const onOverview = step === total
    const current = onOverview ? null : quiz.questions[step]
    const answeredCount = quiz.questions.filter((question) => answers[question.questionId] !== undefined).length

    /* Nothing answered yet: just go. Otherwise ask first, since the attempt is not saved. */
    function leave() {
        if (answeredCount === 0) {
            navigate('/quizzes')
        } else {
            setShowLeave(true)
        }
    }

    function goTo(index: number) {
        moveFocus.current = true
        setStep(index)
    }

    function choose(questionId: number, optionId: number) {
        setAnswers((currentAnswers) => ({ ...currentAnswers, [questionId]: optionId }))
    }

    async function sendAnswers() {
        setSubmitError(null)
        setSubmitting(true)
        try {
            /* Skipped questions are sent as null and counted as wrong, like in Subapp 1 */
            const graded = await submitAttempt(
                quiz.quizId,
                quiz.questions.map((question) => ({
                    questionId: question.questionId,
                    selectedAnswerId: answers[question.questionId] ?? null,
                })),
            )
            setResult(graded)
            window.scrollTo(0, 0)
        } catch (err) {
            setSubmitError(err instanceof ApiError ? err.message : 'Your answers could not be sent. Please try again.')
        } finally {
            setSubmitting(false)
        }
    }

    /* Next and Enter go one step forward. Only the overview sends the answers. */
    function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()

        if (!onOverview) {
            goTo(step + 1)
            return
        }

        void sendAnswers()
    }

    return (
        <div className="page-narrow">
            <button
                type="button"
                className="btn btn-outline-secondary d-inline-flex align-items-center gap-2 mb-3"
                onClick={leave}
            >
                <i className="bi bi-arrow-left" aria-hidden="true"></i>
                Leave quiz
            </button>
            <h1 className="landing-title mb-4">{quiz.title}</h1>

            {/* Where the student is: empty on question 1, full on the overview */}
            <div className="mb-4">
                <div className="d-flex align-items-center justify-content-between mb-2">
                    <p className="small text-secondary mb-0" aria-live="polite">
                        {onOverview ? 'Overview' : `Question ${step + 1} of ${total}`}
                    </p>
                    {!onOverview && (
                        <button
                            type="button"
                            className="btn btn-outline-secondary btn-sm d-inline-flex align-items-center gap-1"
                            onClick={() => goTo(total)}
                        >
                            <i className="bi bi-grid-3x3-gap" aria-hidden="true"></i>Overview
                        </button>
                    )}
                </div>
                <div
                    className="progress"
                    style={{ height: 6 }}
                    role="progressbar"
                    aria-label="Quiz progress"
                    aria-valuenow={step}
                    aria-valuemin={0}
                    aria-valuemax={total}
                >
                    <div className="progress-bar" style={{ width: `${(step * 100) / total}%` }}></div>
                </div>
            </div>

            <form onSubmit={handleSubmit} noValidate>
                <div ref={stepRef} tabIndex={-1} className="card mb-4">
                    <div className="card-body">
                        {current ? (
                            <>
                                {/* Optional scenario or code example. pre-wrap keeps line breaks and indentation. */}
                                {current.context && (
                                    <p id="question-context" className="text-secondary mb-3" style={{ whiteSpace: 'pre-wrap' }}>
                                        {current.context}
                                    </p>
                                )}

                                <h2 id="question-title" className="h5">
                                    {step + 1}. {current.text}
                                </h2>

                                <div
                                    role="radiogroup"
                                    aria-labelledby="question-title"
                                    aria-describedby={current.context ? 'question-context' : undefined}
                                >
                                    {current.options.map((option) => {
                                        const inputId = `q${current.questionId}-a${option.id}`

                                        return (
                                            <div className="form-check mt-3" key={option.id}>
                                                {/* All options of a question share one name, so only one can be chosen */}
                                                <input
                                                    className="form-check-input"
                                                    type="radio"
                                                    id={inputId}
                                                    name={`question-${current.questionId}`}
                                                    checked={answers[current.questionId] === option.id}
                                                    onChange={() => choose(current.questionId, option.id)}
                                                />
                                                <label className="form-check-label" htmlFor={inputId}>
                                                    {option.text}
                                                </label>
                                            </div>
                                        )
                                    })}
                                </div>
                            </>
                        ) : (
                            <>
                                <h2 className="h5">Overview</h2>
                                <p className="text-secondary small mb-3">
                                    {answeredCount === total
                                        ? `All ${total} questions answered.`
                                        : `${answeredCount} of ${total} answered. Questions without an answer count as wrong.`}
                                </p>

                                {/* One button per question: blue when answered, grey when not. Click to go back to it. */}
                                <div className="d-flex flex-wrap gap-2 mb-3">
                                    {quiz.questions.map((question, index) => {
                                        const done = answers[question.questionId] !== undefined

                                        return (
                                            <button
                                                key={question.questionId}
                                                type="button"
                                                className={`btn ${done ? 'btn-primary' : 'btn-outline-secondary'}`}
                                                style={{ minWidth: '2.75rem' }}
                                                aria-label={`Question ${index + 1}, ${done ? 'answered' : 'not answered'}`}
                                                onClick={() => goTo(index)}
                                            >
                                                {index + 1}
                                            </button>
                                        )
                                    })}
                                </div>

                                <p className="small text-secondary mb-0">
                                    <span className="badge text-bg-primary me-1">&nbsp;</span>Answered
                                    <span className="badge border text-secondary ms-3 me-1">&nbsp;</span>Not answered
                                </p>
                            </>
                        )}
                    </div>
                </div>

                <ErrorAlert message={submitError} onClose={() => setSubmitError(null)} />

                <div className="d-flex flex-wrap gap-2">
                    {step > 0 && (
                        <button type="button" className="btn btn-outline-secondary" onClick={() => goTo(step - 1)}>
                            <i className="bi bi-arrow-left me-1" aria-hidden="true"></i>Back
                        </button>
                    )}

                    {onOverview ? (
                        <button type="submit" className="btn btn-hero btn-primary" disabled={submitting}>
                            {submitting && <span className="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>}
                            {submitting ? 'Submitting…' : 'Submit quiz'}
                        </button>
                    ) : (
                        <button type="submit" className="btn btn-hero btn-primary">
                            {step === total - 1 ? 'Overview' : 'Next'}
                            <i className="bi bi-arrow-right ms-1" aria-hidden="true"></i>
                        </button>
                    )}
                </div>
            </form>

            <ConfirmDialog
                show={showLeave}
                title="Leave this quiz?"
                message="Your answers from this attempt will not be saved."
                confirmText="Leave quiz"
                onConfirm={() => navigate('/quizzes')}
                onCancel={() => setShowLeave(false)}
            />
        </div>
    )
}