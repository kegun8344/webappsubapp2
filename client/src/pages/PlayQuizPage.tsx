import { useEffect, useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
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

/* Taking a quiz, the same layout as QuizPlayer/Take in Subapp 1. The result is shown on the same page afterwards. */
export default function PlayQuizPage() {
    const quizId = Number(useParams().id)

    const [loaded, setLoaded] = useState<LoadedQuiz>({ id: null, quiz: null, error: null })

    /* The chosen option per question ID */
    const [answers, setAnswers] = useState<Record<number, number>>({})

    /* Turned on after a submit with unanswered questions, so they are marked in red */
    const [showMissing, setShowMissing] = useState(false)
    const [submitting, setSubmitting] = useState(false)
    const [submitError, setSubmitError] = useState<string | null>(null)
    const [result, setResult] = useState<QuizResult | null>(null)

    const validId = Number.isInteger(quizId) && quizId > 0
    const isLoading = validId && loaded.id !== quizId

    useEffect(() => {
        /* A quiz ID that is not a number cannot exist, so the API is not asked */
        if (!validId) return

        let active = true

        getQuizForPlay(quizId)
            .then((quiz) => {
                if (active) setLoaded({ id: quizId, quiz, error: null })
            })
            .catch((err: unknown) => {
                if (active) setLoaded({ id: quizId, quiz: null, error: loadErrorMessage(err) })
            })

        return () => {
            active = false
        }
    }, [quizId, validId])

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
                    setShowMissing(false)
                    window.scrollTo(0, 0)
                }}
            />
        )
    }

    const answeredCount = quiz.questions.filter((question) => answers[question.questionId] !== undefined).length
    const allAnswered = answeredCount === quiz.questions.length

    function choose(questionId: number, optionId: number) {
        setAnswers((current) => ({ ...current, [questionId]: optionId }))
    }

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()
        setSubmitError(null)

        /* Every question must be answered first. The server also accepts skipped questions, but counts them as wrong. */
        if (!allAnswered) {
            setShowMissing(true)
            const firstMissing = quiz.questions.find((question) => answers[question.questionId] === undefined)
            if (firstMissing) {
                document.getElementById(`question-${firstMissing.questionId}`)?.scrollIntoView({ behavior: 'smooth', block: 'center' })
            }
            return
        }

        setSubmitting(true)
        try {
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

    return (
        <>
            <h1>{quiz.title}</h1>

            {/* How far the student has come */}
            <div className="d-flex align-items-center gap-3 mb-4">
                <div
                    className="progress flex-grow-1"
                    style={{ height: '0.5rem' }}
                    role="progressbar"
                    aria-label="Answered questions"
                    aria-valuenow={answeredCount}
                    aria-valuemin={0}
                    aria-valuemax={quiz.questions.length}
                >
                    <div className="progress-bar" style={{ width: `${(answeredCount * 100) / quiz.questions.length}%` }}></div>
                </div>
                <span className="small text-secondary text-nowrap">
                    {answeredCount} of {quiz.questions.length} answered
                </span>
            </div>

            <form onSubmit={handleSubmit} noValidate>
                {quiz.questions.map((question, index) => {
                    const missing = showMissing && answers[question.questionId] === undefined

                    return (
                        <fieldset
                            key={question.questionId}
                            id={`question-${question.questionId}`}
                            className={`card mb-4${missing ? ' border-danger' : ''}`}
                            aria-invalid={missing}
                            aria-describedby={missing ? `question-${question.questionId}-error` : undefined}
                        >
                            <div className="card-body">
                                <legend className="h5">
                                    {index + 1}. {question.text}
                                </legend>

                                {question.options.map((option) => {
                                    const inputId = `q${question.questionId}-a${option.id}`

                                    return (
                                        <div className="form-check mt-3" key={option.id}>
                                            {/* All options of a question share one name, so only one can be chosen */}
                                            <input
                                                className="form-check-input"
                                                type="radio"
                                                id={inputId}
                                                name={`question-${question.questionId}`}
                                                checked={answers[question.questionId] === option.id}
                                                onChange={() => choose(question.questionId, option.id)}
                                            />
                                            <label className="form-check-label" htmlFor={inputId}>
                                                {option.text}
                                            </label>
                                        </div>
                                    )
                                })}

                                {missing && (
                                    <p id={`question-${question.questionId}-error`} className="text-danger small mt-3 mb-0">
                                        Choose an answer to this question.
                                    </p>
                                )}
                            </div>
                        </fieldset>
                    )
                })}

                <ErrorAlert message={submitError} onClose={() => setSubmitError(null)} />

                {showMissing && !allAnswered && (
                    <p className="text-danger" role="alert">
                        Answer all questions before you submit ({quiz.questions.length - answeredCount} left).
                    </p>
                )}

                <button type="submit" className="btn btn-hero btn-primary" disabled={submitting}>
                    {submitting && <span className="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>}
                    {submitting ? 'Submitting…' : 'Submit quiz'}
                </button>
            </form>
        </>
    )
}