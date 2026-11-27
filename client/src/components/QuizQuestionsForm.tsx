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
    /* Told how many questions are answered, e.g. so the page can ask before leaving */
    onAnsweredChange?: (answeredCount: number) => void
}

/*
 * The quiz player: one question per page, then an overview before submitting.
 * The same as Take.cshtml and quiz-steps.js in Subapp 1, and shared by taking a quiz and reviewing mistakes.
 * A quiz with only one question is shown on one page. Give it a new key to start again with no answers.
 */
export default function QuizQuestionsForm({
    questions,
    submitText,
    submitting,
    submitError,
    onClearError,
    onSubmit,
    onAnsweredChange,
}: QuizQuestionsFormProps) {
    /* The chosen option per question ID */
    const [answers, setAnswers] = useState<Record<number, number>>({})

    /* 0 .. questions.length - 1 is a question, questions.length is the overview */
    const [current, setCurrent] = useState(0)

    const stepped = questions.length >= 2
    const overviewIndex = questions.length
    const onOverview = stepped && current === overviewIndex
    const lastQuestion = current === questions.length - 1

    const answeredCount = questions.filter((question) => answers[question.questionId] !== undefined).length

    /* Focus moves to the new question or the overview, so keyboard and screen reader users follow along */
    const stepHeading = useRef<HTMLHeadingElement>(null)
    const overviewHeading = useRef<HTMLHeadingElement>(null)
    const moveFocus = useRef(false)

    useEffect(() => {
        if (!moveFocus.current) return
        moveFocus.current = false
        ;(onOverview ? overviewHeading.current : stepHeading.current)?.focus()
    }, [current, onOverview])

    function go(index: number) {
        moveFocus.current = true
        setCurrent(index)
    }

    function choose(questionId: number, optionId: number) {
        const next = { ...answers, [questionId]: optionId }
        setAnswers(next)
        onAnsweredChange?.(questions.filter((question) => next[question.questionId] !== undefined).length)
    }

    function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()

        /* Enter on a radio button would submit. Go to the next step instead, until the overview. */
        if (stepped && !onOverview) {
            go(current + 1)
            return
        }

        /* Unanswered questions are allowed, but count as wrong (the overview says so) */
        onSubmit(questions.map((question) => ({ questionId: question.questionId, selectedAnswerId: answers[question.questionId] ?? null })))
    }

    /* One question card: the optional context, the question and its options */
    function questionCard(question: PlayQuestion, index: number) {
        return (
            /* role="group" with the question as its name, so screen readers read the question with the options */
            <div key={question.questionId} className="card mb-4" role="group" aria-labelledby={`question-${question.questionId}-title`}>
                <div className="card-body">
                    {/* pre-wrap keeps the line breaks and indentation of code examples */}
                    {question.context && (
                        <p className="text-secondary mb-3" style={{ whiteSpace: 'pre-wrap' }}>
                            {question.context}
                        </p>
                    )}

                    {/* An h2 styled as h5 keeps the heading order right. tabIndex lets the focus move here. */}
                    <h2 id={`question-${question.questionId}-title`} ref={stepped ? stepHeading : undefined} className="h5" tabIndex={-1}>
                        {index + 1}. {question.text}
                    </h2>

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
                </div>
            </div>
        )
    }

    const missing = questions.length - answeredCount

    return (
        <form onSubmit={handleSubmit} noValidate>
            {stepped && (
                <div className="mb-4">
                    <div className="d-flex align-items-center justify-content-between mb-2">
                        <p className="small text-secondary mb-0" aria-live="polite">
                            {onOverview ? 'Overview' : `Question ${current + 1} of ${questions.length}`}
                        </p>
                        {!onOverview && (
                            <button
                                type="button"
                                className="btn btn-outline-secondary btn-sm d-inline-flex align-items-center gap-1"
                                onClick={() => go(overviewIndex)}
                            >
                                <i className="bi bi-grid-3x3-gap" aria-hidden="true"></i>Overview
                            </button>
                        )}
                    </div>
                    {/* Empty on question 1, full on the overview */}
                    <div
                        className="progress"
                        role="progressbar"
                        aria-label="Quiz progress"
                        aria-valuemin={0}
                        aria-valuemax={questions.length}
                        aria-valuenow={current}
                        style={{ height: 6 }}
                    >
                        <div className="progress-bar" style={{ width: `${(current / questions.length) * 100}%` }}></div>
                    </div>
                </div>
            )}

            {stepped ? !onOverview && questionCard(questions[current], current) : questions.map(questionCard)}

            {/* Overview before submitting: which questions are answered. A button per question goes back to it. */}
            {onOverview && (
                <div className="card mb-4">
                    <div className="card-body">
                        <h2 ref={overviewHeading} className="h5" tabIndex={-1}>Overview</h2>
                        <p className="text-secondary small mb-3">
                            {missing > 0
                                ? `${answeredCount} of ${questions.length} answered. Questions without an answer count as wrong.`
                                : `All ${questions.length} questions answered.`}
                        </p>
                        <div className="d-flex flex-wrap gap-2 mb-3">
                            {questions.map((question, index) => {
                                const done = answers[question.questionId] !== undefined

                                return (
                                    <button
                                        key={question.questionId}
                                        type="button"
                                        className={done ? 'btn btn-primary' : 'btn btn-outline-secondary'}
                                        style={{ minWidth: '2.75rem' }}
                                        aria-label={`Question ${index + 1}, ${done ? 'answered' : 'not answered'}`}
                                        onClick={() => go(index)}
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
                    </div>
                </div>
            )}

            <ErrorAlert message={submitError} onClose={onClearError} />

            <div className="d-flex flex-wrap gap-2">
                {stepped && current > 0 && (
                    <button type="button" className="btn btn-outline-secondary" onClick={() => go(current - 1)}>
                        <i className="bi bi-arrow-left me-1" aria-hidden="true"></i>Back
                    </button>
                )}

                {stepped && !onOverview ? (
                    <button type="button" className="btn btn-hero btn-primary" onClick={() => go(current + 1)}>
                        {lastQuestion ? 'Overview' : 'Next'}
                        <i className="bi bi-arrow-right ms-1" aria-hidden="true"></i>
                    </button>
                ) : (
                    <button type="submit" className="btn btn-hero btn-primary" disabled={submitting}>
                        {submitting && <span className="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>}
                        {submitting ? 'Submitting…' : submitText}
                    </button>
                )}
            </div>
        </form>
    )
}