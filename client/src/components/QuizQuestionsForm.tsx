import { useEffect, useRef, useState, type FormEvent } from 'react'
import ErrorAlert from './ErrorAlert'
import type { PlayQuestion, SubmittedAnswer } from '../types/play'

interface QuizQuestionsFormProps {
    questions: PlayQuestion[]
    submitText: string
    submitting: boolean
    submitError: string | null
    onClearError: () => void
    onSubmit: (answers: SubmittedAnswer[]) => void
    /* Told how many questions are answered, e.g. for "Leave quiz" */
    onAnsweredChange?: (count: number) => void
}

/*
 * The questions of a quiz, one per page with Back and Next, and an overview before submitting.
 * Shared by taking a quiz and reviewing mistakes, the same as Take.cshtml is in Subapp 1.
 * Give it a new key to start again with no answers.
 */
export default function QuizQuestionsForm({ questions, submitText, submitting, submitError, onClearError, onSubmit, onAnsweredChange }: QuizQuestionsFormProps) {
    /* The chosen option per question ID */
    const [answers, setAnswers] = useState<Record<number, number>>({})

    /* Which question is shown. The number of questions means the overview. */
    const [step, setStep] = useState(0)

    /* The question or the overview. Focused after Back/Next, so keyboard and screen reader users follow along. */
    const stepRef = useRef<HTMLDivElement>(null)
    const moveFocus = useRef(false)

    const total = questions.length
    const onOverview = step === total
    const current = onOverview ? null : questions[step]
    const answeredCount = questions.filter((question) => answers[question.questionId] !== undefined).length

    useEffect(() => {
        onAnsweredChange?.(answeredCount)
    }, [answeredCount, onAnsweredChange])

    useEffect(() => {
        if (moveFocus.current) {
            moveFocus.current = false
            stepRef.current?.focus()
        }
    }, [step])

    function goTo(index: number) {
        moveFocus.current = true
        setStep(index)
    }

    function choose(questionId: number, optionId: number) {
        setAnswers((currentAnswers) => ({ ...currentAnswers, [questionId]: optionId }))
    }

    /* Next and Enter go one step forward. Only the overview sends the answers. Skipped questions count as wrong. */
    function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()

        if (!onOverview) {
            goTo(step + 1)
            return
        }

        onSubmit(questions.map((question) => ({ questionId: question.questionId, selectedAnswerId: answers[question.questionId] ?? null })))
    }

    return (
        <>
            {/* Where the user is: empty on question 1, full on the overview */}
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
                                    {questions.map((question, index) => {
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

                <ErrorAlert message={submitError} onClose={onClearError} />

                <div className="d-flex flex-wrap gap-2">
                    {step > 0 && (
                        <button type="button" className="btn btn-outline-secondary" onClick={() => goTo(step - 1)}>
                            <i className="bi bi-arrow-left me-1" aria-hidden="true"></i>Back
                        </button>
                    )}

                    {onOverview ? (
                        <button type="submit" className="btn btn-hero btn-primary" disabled={submitting}>
                            {submitting && <span className="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>}
                            {submitting ? 'Submitting…' : submitText}
                        </button>
                    ) : (
                        <button type="submit" className="btn btn-hero btn-primary">
                            {step === total - 1 ? 'Overview' : 'Next'}
                            <i className="bi bi-arrow-right ms-1" aria-hidden="true"></i>
                        </button>
                    )}
                </div>
            </form>
        </>
    )
}