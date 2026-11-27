import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import ErrorAlert from '../components/ErrorAlert'
import LoadingSpinner from '../components/LoadingSpinner'
import { ApiError } from '../services/apiClient'
import { createQuestion, updateQuestion } from '../services/questionService'
import { getQuiz } from '../services/quizService'
import type { Question, QuizDetail } from '../types/quiz'
import { hasQuestionErrors, MAX_ANSWERS, MIN_ANSWERS, validateQuestion, type QuestionFormErrors } from '../utils/quizValidation'

interface LoadedQuiz {
    id: number | null
    quiz: QuizDetail | null
    error: string | null
}

/*
 * Add question and Edit question, the same page as Question/QuestionForm in Subapp 1.
 * The quiz is loaded first, so the page knows the quiz title and, when editing, the question.
 */
export default function QuestionFormPage() {
    const params = useParams()
    const quizId = Number(params.quizId)
    const questionId = params.questionId ? Number(params.questionId) : null

    const [loaded, setLoaded] = useState<LoadedQuiz>({ id: null, quiz: null, error: null })

    useEffect(() => {
        let active = true

        getQuiz(quizId)
            .then((quiz) => {
                if (active) setLoaded({ id: quizId, quiz, error: null })
            })
            .catch((err: unknown) => {
                if (active) {
                    setLoaded({
                        id: quizId,
                        quiz: null,
                        error: err instanceof ApiError && err.status === 404 ? 'This quiz was not found.' : 'The quiz could not be loaded.',
                    })
                }
            })

        return () => {
            active = false
        }
    }, [quizId])

    if (loaded.id !== quizId) {
        return <LoadingSpinner message="Loading quiz..." />
    }

    const quiz = loaded.quiz
    const question = questionId === null ? null : quiz?.questions.find((item) => item.id === questionId)

    /* The quiz could not be loaded, the user may not edit it, or the question is not in this quiz */
    const problem = !quiz
        ? loaded.error
        : !quiz.canManage
          ? 'Only the creator of this quiz or an administrator can change its questions.'
          : questionId !== null && !question
            ? 'This question was not found in the quiz.'
            : null

    if (!quiz || problem) {
        return (
            <div className="mx-auto text-center py-5" style={{ maxWidth: 520 }}>
                <ErrorAlert message={problem} />
                <Link to={quiz ? `/quizzes/${quiz.id}/edit` : '/quizzes'} className="btn btn-outline-primary">
                    {quiz ? 'Back to quiz' : 'Back to quizzes'}
                </Link>
            </div>
        )
    }

    /* The form gets the starting values once, when the quiz has loaded */
    return <QuestionForm key={questionId ?? 'new'} quiz={quiz} question={question ?? null} />
}

/* One answer row. The key keeps React from mixing up the inputs when a row in the middle is removed. */
interface AnswerRow {
    key: number
    text: string
}

interface QuestionFormProps {
    quiz: QuizDetail
    /* null when adding a new question */
    question: Question | null
}

function QuestionForm({ quiz, question }: QuestionFormProps) {
    const navigate = useNavigate()
    const isEdit = question !== null

    /* A new question starts with two empty answers, like in Subapp 1 */
    const startAnswers = question ? question.options.map((option) => option.text) : ['', '']
    const startCorrect = question ? question.options.findIndex((option) => option.isCorrect) : -1

    const [text, setText] = useState(question?.text ?? '')
    const [answers, setAnswers] = useState<AnswerRow[]>(startAnswers.map((answer, index) => ({ key: index, text: answer })))
    const [correctIndex, setCorrectIndex] = useState<number | null>(startCorrect >= 0 ? startCorrect : null)
    const [errors, setErrors] = useState<QuestionFormErrors>({ answerFields: [] })
    const [serverError, setServerError] = useState<string | null>(null)
    const [submitting, setSubmitting] = useState(false)

    /* Told to screen reader users, e.g. "Answer 3 added" */
    const [status, setStatus] = useState('')

    /* The next free row key. Only changed in event handlers. */
    const nextKey = useRef(startAnswers.length)

    function updateAnswer(index: number, value: string) {
        setAnswers((rows) => rows.map((row, i) => (i === index ? { ...row, text: value } : row)))
        setErrors((current) => ({
            ...current,
            answers: undefined,
            answerFields: current.answerFields.map((error, i) => (i === index ? undefined : error)),
        }))
    }

    function addAnswer() {
        if (answers.length >= MAX_ANSWERS) return

        setAnswers((rows) => [...rows, { key: nextKey.current++, text: '' }])
        setStatus(`Answer ${answers.length + 1} added.`)
    }

    function removeAnswer(index: number) {
        if (answers.length <= MIN_ANSWERS) return

        setAnswers((rows) => rows.filter((_, i) => i !== index))
        setErrors((current) => ({ ...current, answerFields: current.answerFields.filter((_, i) => i !== index) }))

        /* Keep the correct answer pointing at the same text after the rows move up */
        setCorrectIndex((current) => {
            if (current === null || current === index) return null
            return current > index ? current - 1 : current
        })
        setStatus(`Answer ${index + 1} removed.`)
    }

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()
        setServerError(null)

        const texts = answers.map((row) => row.text)
        const found = validateQuestion(text, texts, correctIndex)
        setErrors(found)
        if (hasQuestionErrors(found) || correctIndex === null) return

        const body = { text: text.trim(), answers: texts.map((answer) => answer.trim()), correctAnswerIndex: correctIndex }

        setSubmitting(true)
        try {
            if (question) {
                await updateQuestion(question.id, body)
            } else {
                await createQuestion(quiz.id, body)
            }
            navigate(`/quizzes/${quiz.id}/edit`, { state: { message: isEdit ? 'Question updated.' : 'Question added.' } })
        } catch (err) {
            if (err instanceof ApiError) {
                /* The same rules are checked on the server (QuestionRules) */
                setErrors((current) => ({
                    ...current,
                    text: err.fieldErrors.text?.[0],
                    answers: err.fieldErrors.answers?.[0],
                    correctAnswerIndex: err.fieldErrors.correctAnswerIndex?.[0],
                }))
                setServerError(err.message)
            } else {
                setServerError('The question could not be saved. Please try again.')
            }
            setSubmitting(false)
        }
    }

    return (
        <div className="mx-auto" style={{ maxWidth: 700 }}>
            <Link to={`/quizzes/${quiz.id}/edit`} className="btn btn-outline-secondary d-inline-flex align-items-center gap-2">
                <i className="bi bi-arrow-left btn-icon icon-back" aria-hidden="true"></i>
                Back to quiz
            </Link>

            <h1 className="fw-bold mt-2 mb-1">{isEdit ? 'Edit question' : 'Add question'}</h1>
            <p className="text-secondary mb-4">
                Quiz: <strong>{quiz.title}</strong>
            </p>

            <form onSubmit={handleSubmit} noValidate>
                {serverError && <div className="alert alert-danger" role="alert">{serverError}</div>}

                <div className="mb-4">
                    <label htmlFor="question-text" className="form-label fw-semibold">Question</label>
                    <textarea
                        id="question-text"
                        className={`form-control${errors.text ? ' is-invalid' : ''}`}
                        rows={2}
                        maxLength={500}
                        value={text}
                        aria-invalid={Boolean(errors.text)}
                        aria-describedby={errors.text ? 'question-error' : undefined}
                        onChange={(event) => {
                            setText(event.target.value)
                            setErrors((current) => ({ ...current, text: undefined }))
                        }}
                    />
                    {errors.text && <div id="question-error" className="invalid-feedback">{errors.text}</div>}
                </div>

                <fieldset className="mb-4" aria-describedby="answers-help">
                    <legend className="form-label fw-semibold fs-6 mb-1">Answers</legend>
                    <p id="answers-help" className="text-secondary small mb-2">
                        Write {MIN_ANSWERS}–{MAX_ANSWERS} answers and select the correct one.
                    </p>

                    {answers.map((row, index) => {
                        const error = errors.answerFields[index]

                        return (
                            <div className="answer-row mb-2" key={row.key}>
                                <div className="d-flex align-items-center gap-2">
                                    <input
                                        className="form-check-input mt-0"
                                        type="radio"
                                        name="correct-answer"
                                        checked={correctIndex === index}
                                        aria-label={`Answer ${index + 1} is correct`}
                                        onChange={() => {
                                            setCorrectIndex(index)
                                            setErrors((current) => ({ ...current, correctAnswerIndex: undefined }))
                                        }}
                                    />
                                    <input
                                        className={`form-control${error ? ' is-invalid' : ''}`}
                                        maxLength={300}
                                        value={row.text}
                                        aria-label={`Answer ${index + 1}`}
                                        aria-invalid={Boolean(error)}
                                        aria-describedby={error ? `answer-error-${row.key}` : undefined}
                                        onChange={(event) => updateAnswer(index, event.target.value)}
                                    />
                                    {/* The last two rows cannot be removed, a question needs at least two answers */}
                                    <button
                                        type="button"
                                        className="btn-close"
                                        aria-label={`Remove answer ${index + 1}`}
                                        disabled={answers.length <= MIN_ANSWERS}
                                        onClick={() => removeAnswer(index)}
                                    ></button>
                                </div>
                                {error && (
                                    <span id={`answer-error-${row.key}`} className="text-danger small">
                                        {error}
                                    </span>
                                )}
                            </div>
                        )
                    })}

                    {answers.length < MAX_ANSWERS && (
                        <button type="button" className="btn btn-outline-primary btn-sm mt-1" onClick={addAnswer}>
                            + Add answer
                        </button>
                    )}

                    <div className="visually-hidden" aria-live="polite">{status}</div>

                    {(errors.answers || errors.correctAnswerIndex) && (
                        <span className="text-danger small d-block mt-2" role="alert">
                            {errors.answers ?? errors.correctAnswerIndex}
                        </span>
                    )}
                </fieldset>

                <div className="d-flex gap-2">
                    <button type="submit" className="btn btn-primary" disabled={submitting}>
                        {submitting && <span className="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>}
                        {isEdit ? 'Save changes' : 'Save question'}
                    </button>
                    <Link to={`/quizzes/${quiz.id}/edit`} className="btn btn-outline-secondary">Cancel</Link>
                </div>
            </form>
        </div>
    )
}