import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import ErrorAlert from '../components/ErrorAlert'
import LoadingSpinner from '../components/LoadingSpinner'
import { ApiError } from '../services/apiClient'
import { getQuizAnalysis } from '../services/analysisService'
import type { QuestionAnalysis, QuizAnalysis } from '../types/analysis'

interface LoadedAnalysis {
    id: number | null
    analysis: QuizAnalysis | null
    error: string | null
}

function loadErrorMessage(err: unknown): string {
    if (err instanceof ApiError && err.status === 404) return 'This quiz was not found.'
    if (err instanceof ApiError && err.status === 403) return 'You can only see the analysis of your own quizzes.'
    return 'The analysis could not be loaded. Please try again.'
}

/* Red for hard questions, yellow for too easy ones, green for the rest */
function barColor(flag: QuestionAnalysis['flag']): string {
    if (flag === 'Hard') return 'bg-danger'
    if (flag === 'Too easy') return 'bg-warning'
    return 'bg-success'
}

function flagBadgeClass(flag: NonNullable<QuestionAnalysis['flag']>): string {
    return flag === 'Hard'
        ? 'bg-danger-subtle text-danger-emphasis border border-danger-subtle'
        : 'bg-warning-subtle text-warning-emphasis border border-warning-subtle'
}

/* One question: the share of correct answers and the wrong answer most students chose */
function QuestionCard({ question }: { question: QuestionAnalysis }) {
    return (
        <li className="card border-0 shadow-sm">
            <div className="card-body p-4">
                <div className="d-flex flex-wrap justify-content-between align-items-start gap-2 mb-2">
                    <h2 className="h6 fw-bold mb-0 text-break">
                        <span className="text-secondary me-1">{question.number}.</span>
                        {question.text}
                    </h2>
                    {question.flag && <span className={`badge ${flagBadgeClass(question.flag)}`}>{question.flag}</span>}
                </div>

                {question.percentCorrect === null ? (
                    <p className="small text-secondary mb-0">No students have answered this question yet.</p>
                ) : (
                    <>
                        <div
                            className="progress mb-2"
                            role="progressbar"
                            aria-label={`Correct answers for question ${question.number}`}
                            aria-valuenow={question.percentCorrect}
                            aria-valuemin={0}
                            aria-valuemax={100}
                            style={{ height: '0.6rem' }}
                        >
                            <div className={`progress-bar ${barColor(question.flag)}`} style={{ width: `${question.percentCorrect}%` }}></div>
                        </div>
                        <p className="small text-secondary mb-0">
                            <strong className="text-body">{question.percentCorrect} %</strong> correct · {question.correct} of {question.answers}{' '}
                            {question.answers === 1 ? 'answer' : 'answers'}
                        </p>

                        {question.mostChosenWrongAnswer && (
                            <p className="small mb-0 mt-2">
                                <span className="text-secondary">Most chosen wrong answer:</span> <strong>{question.mostChosenWrongAnswer}</strong>{' '}
                                <span className="text-secondary">({question.mostChosenWrongCount})</span>
                            </p>
                        )}
                    </>
                )}
            </div>
        </li>
    )
}

/* Question analysis for the quiz owner or an administrator, the same as Quiz/Analysis in Subapp 1 */
export default function QuizAnalysisPage() {
    const quizId = Number(useParams().id)

    const [loaded, setLoaded] = useState<LoadedAnalysis>({ id: null, analysis: null, error: null })
    const isLoading = loaded.id !== quizId

    useEffect(() => {
        let active = true

        getQuizAnalysis(quizId)
            .then((analysis) => {
                if (active) setLoaded({ id: quizId, analysis, error: null })
            })
            .catch((err: unknown) => {
                if (active) setLoaded({ id: quizId, analysis: null, error: loadErrorMessage(err) })
            })

        return () => {
            active = false
        }
    }, [quizId])

    return (
        <>
            <Link to="/" className="btn btn-outline-secondary d-inline-flex align-items-center gap-2 mb-3">
                <i className="bi bi-arrow-left" aria-hidden="true"></i>
                Back
            </Link>

            <h1 className="landing-title mb-2">Analysis</h1>

            {isLoading ? (
                <LoadingSpinner />
            ) : !loaded.analysis ? (
                <ErrorAlert message={loaded.error} />
            ) : (
                <>
                    <p className="lead text-secondary mb-4">{loaded.analysis.title}</p>

                    {loaded.analysis.questions.length === 0 ? (
                        <div className="card border-0 shadow-sm">
                            <div className="card-body p-4">
                                <p className="text-secondary mb-3">This quiz has no questions yet.</p>
                                <Link to={`/quizzes/${loaded.analysis.quizId}/edit`} className="btn btn-primary">Add questions</Link>
                            </div>
                        </div>
                    ) : (
                        <>
                            <p className="small text-secondary mb-3">
                                Only answers by students count. A question is flagged as <strong>Hard</strong> below {loaded.analysis.hardBelowPercent} % correct
                                and <strong>Too easy</strong> above {loaded.analysis.easyAbovePercent} %, once at least {loaded.analysis.minAnswersForFlag} students
                                have answered it.
                            </p>

                            <ol className="list-unstyled mb-0 d-grid gap-3">
                                {loaded.analysis.questions.map((question) => (
                                    <QuestionCard key={question.number} question={question} />
                                ))}
                            </ol>
                        </>
                    )}
                </>
            )}
        </>
    )
}