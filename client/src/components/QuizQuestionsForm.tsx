import { useState, type FormEvent } from 'react'
import ErrorAlert from './ErrorAlert'
import type { PlayQuestion, SubmittedAnswer } from '../types/play'

interface QuizQuestionsFormProps {
    questions: PlayQuestion[]
    submitText: string
    submitting: boolean
    submitError: string | null
    onClearError: () => void
    onSubmit: (answers: SubmittedAnswer[]) => void
}

/*
 * The questions of a quiz with a radio button per answer, and the submit button.
 * Shared by taking a quiz and reviewing mistakes, the same as Take.cshtml is in Subapp 1.
 * Give it a new key to start again with no answers.
 */
export default function QuizQuestionsForm({ questions, submitText, submitting, submitError, onClearError, onSubmit }: QuizQuestionsFormProps) {
    /* The chosen option per question ID */
    const [answers, setAnswers] = useState<Record<number, number>>({})

    /* Turned on after a submit with unanswered questions, so they are marked in red */
    const [showMissing, setShowMissing] = useState(false)

    const answeredCount = questions.filter((question) => answers[question.questionId] !== undefined).length
    const allAnswered = answeredCount === questions.length

    function choose(questionId: number, optionId: number) {
        setAnswers((current) => ({ ...current, [questionId]: optionId }))
    }

    function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()

        /* Every question must be answered first. The server also accepts skipped questions, but counts them as wrong. */
        if (!allAnswered) {
            setShowMissing(true)
            const firstMissing = questions.find((question) => answers[question.questionId] === undefined)
            if (firstMissing) {
                document.getElementById(`question-${firstMissing.questionId}`)?.scrollIntoView({ behavior: 'smooth', block: 'center' })
            }
            return
        }

        onSubmit(questions.map((question) => ({ questionId: question.questionId, selectedAnswerId: answers[question.questionId] ?? null })))
    }

    return (
        <>
            {/* How far the user has come */}
            <div className="d-flex align-items-center gap-3 mb-4">
                <div
                    className="progress flex-grow-1"
                    style={{ height: '0.5rem' }}
                    role="progressbar"
                    aria-label="Answered questions"
                    aria-valuenow={answeredCount}
                    aria-valuemin={0}
                    aria-valuemax={questions.length}
                >
                    <div className="progress-bar" style={{ width: `${(answeredCount * 100) / questions.length}%` }}></div>
                </div>
                <span className="small text-secondary text-nowrap">
                    {answeredCount} of {questions.length} answered
                </span>
            </div>

            <form onSubmit={handleSubmit} noValidate>
                {questions.map((question, index) => {
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

                <ErrorAlert message={submitError} onClose={onClearError} />

                {showMissing && !allAnswered && (
                    <p className="text-danger" role="alert">
                        Answer all questions before you submit ({questions.length - answeredCount} left).
                    </p>
                )}

                <button type="submit" className="btn btn-hero btn-primary" disabled={submitting}>
                    {submitting && <span className="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>}
                    {submitting ? 'Submitting…' : submitText}
                </button>
            </form>
        </>
    )
}