import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import ConfirmDialog from '../components/ConfirmDialog'
import ErrorAlert from '../components/ErrorAlert'
import LoadingSpinner from '../components/LoadingSpinner'
import QuizQuestionsForm from '../components/QuizQuestionsForm'
import QuizResultCard from '../components/QuizResultCard'
import { ApiError } from '../services/apiClient'
import { getQuizForPlay, submitAttempt } from '../services/playService'
import type { QuizPlay, QuizResult, SubmittedAnswer } from '../types/play'

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

/* Taking a quiz, the same as QuizPlayer/Take in Subapp 1. The questions are in QuizQuestionsForm. The result is shown on the same page afterwards. */
export default function PlayQuizPage() {
    const quizId = Number(useParams().id)
    const navigate = useNavigate()

    const [loaded, setLoaded] = useState<LoadedQuiz>({ id: null, quiz: null, error: null })

    /* Changing the key starts the question form again with no answers ("Try again") */
    const [formKey, setFormKey] = useState(0)
    const [submitting, setSubmitting] = useState(false)
    const [submitError, setSubmitError] = useState<string | null>(null)
    const [result, setResult] = useState<QuizResult | null>(null)

    /* How many questions are answered (from the form), and the "Leave this quiz?" popup */
    const [answeredCount, setAnsweredCount] = useState(0)
    const [showLeave, setShowLeave] = useState(false)

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
                    setFormKey((key) => key + 1)
                    setAnsweredCount(0)
                    window.scrollTo(0, 0)
                }}
            />
        )
    }

    async function handleSubmit(submitted: SubmittedAnswer[]) {
        setSubmitError(null)
        setSubmitting(true)
        try {
            setResult(await submitAttempt(quiz.quizId, submitted))
            window.scrollTo(0, 0)
        } catch (err) {
            setSubmitError(err instanceof ApiError ? err.message : 'Your answers could not be sent. Please try again.')
        } finally {
            setSubmitting(false)
        }
    }

    /* Nothing answered yet: just go. Otherwise ask first, since the attempt is not saved. */
    function leave() {
        if (answeredCount === 0) {
            navigate('/quizzes')
        } else {
            setShowLeave(true)
        }
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

            <QuizQuestionsForm
                key={formKey}
                questions={quiz.questions}
                submitText="Submit quiz"
                submitting={submitting}
                submitError={submitError}
                onClearError={() => setSubmitError(null)}
                onSubmit={(submitted) => void handleSubmit(submitted)}
                onAnsweredChange={setAnsweredCount}
            />

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