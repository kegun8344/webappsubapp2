import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import ConfirmDialog from '../components/ConfirmDialog'
import ErrorAlert from '../components/ErrorAlert'
import LoadingSpinner from '../components/LoadingSpinner'
import QuizQuestionsForm from '../components/QuizQuestionsForm'
import { ApiError } from '../services/apiClient'
import { clearMistakes, getReview, submitReview } from '../services/reviewService'
import type { QuizPlay, SubmittedAnswer } from '../types/play'
import type { ReviewResult } from '../types/review'

/* The review loaded for one address (and one round), so an old answer is never shown */
interface LoadedReview {
    key: string | null
    review: QuizPlay | null
    error: string | null
}

/*
 * "Review mistakes": the questions the user got wrong, as a practice quiz without points.
 * The same pages as Review/Index, Review/Empty and Review/Result in Subapp 1.
 * /review gives mistakes from all quizzes, /review?quizId=3 only from one quiz.
 */
export default function ReviewPage() {
    const navigate = useNavigate()
    const [searchParams] = useSearchParams()

    const quizIdParam = Number(searchParams.get('quizId'))
    const quizId = Number.isInteger(quizIdParam) && quizIdParam > 0 ? quizIdParam : null

    /* "Review more" loads a new round of questions */
    const [round, setRound] = useState(0)
    const loadKey = `${quizId ?? 'all'}|${round}`

    const [loaded, setLoaded] = useState<LoadedReview>({ key: null, review: null, error: null })
    const [result, setResult] = useState<ReviewResult | null>(null)
    const [submitting, setSubmitting] = useState(false)
    const [submitError, setSubmitError] = useState<string | null>(null)

    const [confirmClear, setConfirmClear] = useState(false)
    const [clearing, setClearing] = useState(false)
    const [clearError, setClearError] = useState<string | null>(null)

    const isLoading = loaded.key !== loadKey

    useEffect(() => {
        let active = true

        getReview(quizId)
            .then((review) => {
                if (active) setLoaded({ key: loadKey, review, error: null })
            })
            .catch((err: unknown) => {
                if (active) {
                    setLoaded({
                        key: loadKey,
                        review: null,
                        error: err instanceof ApiError ? err.message : 'Your mistakes could not be loaded. Please try again.',
                    })
                }
            })

        return () => {
            active = false
        }
    }, [quizId, loadKey])

    const closeClearDialog = useCallback(() => setConfirmClear(false), [])

    async function handleSubmit(answers: SubmittedAnswer[]) {
        setSubmitError(null)
        setSubmitting(true)
        try {
            setResult(await submitReview(quizId, answers))
            window.scrollTo(0, 0)
        } catch (err) {
            setSubmitError(err instanceof ApiError ? err.message : 'Your answers could not be sent. Please try again.')
        } finally {
            setSubmitting(false)
        }
    }

    async function handleClear() {
        if (!quizId) return

        setClearing(true)
        setClearError(null)
        try {
            const cleared = await clearMistakes(quizId)
            navigate('/quizzes', { state: { message: cleared.cleared === 1 ? '1 mistake cleared.' : `${cleared.cleared} mistakes cleared.` } })
        } catch (err) {
            setClearError(err instanceof ApiError ? err.message : 'The mistakes could not be cleared. Please try again.')
            setClearing(false)
            setConfirmClear(false)
        }
    }

    /* "Clear mistakes" for one quiz, a text button that asks first (_ClearMistakes.cshtml in Subapp 1) */
    const clearButton = quizId && (
        <button type="button" className="btn btn-link text-secondary" onClick={() => setConfirmClear(true)}>
            Clear mistakes
        </button>
    )

    const clearDialog = (
        <ConfirmDialog
            show={confirmClear}
            title="Clear the mistakes in this quiz?"
            message={<p className="text-secondary mb-0">They come back if you get them wrong again.</p>}
            confirmText="Clear"
            busy={clearing}
            onConfirm={() => void handleClear()}
            onCancel={closeClearDialog}
        />
    )

    if (result) {
        const reviewMore = result.remaining > 0 || !result.resultSaved

        return (
            <div className="mx-auto" style={{ maxWidth: 520 }}>
                <div className="card border-0 text-center shadow-sm">
                    <div className="card-body p-4 p-md-5">
                        <p className="text-secondary mb-1">Review completed</p>
                        <h1 className="h4 fw-bold mb-4">Your mistakes</h1>

                        {!result.resultSaved ? (
                            <div className="alert alert-warning" role="alert">
                                Your answers could not be saved, so the questions stay in your review list. Please try again.
                            </div>
                        ) : (
                            <>
                                <div className="display-3 fw-bold text-primary">
                                    {result.correctAnswers} / {result.totalQuestions}
                                </div>
                                <p className="fs-5 fw-semibold mb-1">
                                    {result.correctAnswers === result.totalQuestions ? 'All mistakes fixed!' : 'Getting there!'}
                                </p>
                                <p className="text-secondary mb-4">
                                    {result.remaining === 0
                                        ? 'You have no more mistakes to review.'
                                        : `${result.remaining} ${result.remaining === 1 ? 'question' : 'questions'} left to review.`}
                                </p>
                            </>
                        )}

                        <p className="small text-secondary">Review is for practice only, so it does not give points.</p>

                        <ErrorAlert message={clearError} onClose={() => setClearError(null)} />

                        <div className="d-grid gap-2">
                            {reviewMore && (
                                <button
                                    type="button"
                                    className="btn btn-primary btn-hero"
                                    onClick={() => {
                                        setResult(null)
                                        setRound((current) => current + 1)
                                        window.scrollTo(0, 0)
                                    }}
                                >
                                    Review more
                                </button>
                            )}
                            <Link to="/quizzes" className="btn btn-outline-primary">Back to quizzes</Link>

                            {/* Only when there are mistakes left in this quiz */}
                            {result.remaining > 0 && result.resultSaved && clearButton}
                        </div>
                    </div>
                </div>

                {clearDialog}
            </div>
        )
    }

    if (isLoading) {
        return <LoadingSpinner message="Loading your mistakes..." />
    }

    if (!loaded.review) {
        return (
            <div className="mx-auto text-center py-5" style={{ maxWidth: 520 }}>
                <ErrorAlert message={loaded.error} />
                <Link to="/quizzes" className="btn btn-outline-primary">Back to quizzes</Link>
            </div>
        )
    }

    const review = loaded.review

    if (review.questions.length === 0) {
        return (
            <div className="mx-auto text-center py-5" style={{ maxWidth: 520 }}>
                <h1 className="h4 fw-bold mb-2">Nothing to review</h1>
                <p className="text-secondary mb-4">
                    Questions you answer wrong end up here, so you can practise them again. Take a quiz first, and come back when you
                    have some mistakes to fix.
                </p>
                <Link to="/quizzes" className="btn btn-primary">Back to quizzes</Link>
            </div>
        )
    }

    return (
        <>
            {/* Review of one quiz: a way back, and a way to clear the mistakes without answering them */}
            {quizId && (
                <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-3">
                    <Link to="/quizzes" className="btn btn-outline-secondary btn-sm">
                        <i className="bi bi-arrow-left me-1" aria-hidden="true"></i>Back to quizzes
                    </Link>
                    {clearButton}
                </div>
            )}

            <ErrorAlert message={clearError} onClose={() => setClearError(null)} />

            <h1 className="landing-title mb-2">{review.title}</h1>
            <p className="lead text-secondary mb-4">
                {review.questions.length} {review.questions.length === 1 ? 'question' : 'questions'} you missed last time. Answer them right to
                clear them.
            </p>

            <QuizQuestionsForm
                key={loadKey}
                questions={review.questions}
                submitText="Submit quiz"
                submitting={submitting}
                submitError={submitError}
                onClearError={() => setSubmitError(null)}
                onSubmit={(answers) => void handleSubmit(answers)}
            />

            {clearDialog}
        </>
    )
}