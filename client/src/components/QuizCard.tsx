import { useState, type CSSProperties } from 'react'
import { Link } from 'react-router-dom'
import type { QuizListItem } from '../types/quiz'

interface QuizCardProps {
    quiz: QuizListItem
    /* Only students have progress: the score ring, the review button and "Start over" */
    isStudent: boolean
    onDelete: (quiz: QuizListItem) => void
}

/* One quiz card, with the same markup and classes as Views/Quiz/Index.cshtml in Subapp 1 */
export default function QuizCard({ quiz, isStudent, onDelete }: QuizCardProps) {
    /* The ⋮ menu. Opened with React state, so no Bootstrap JavaScript is needed. */
    const [menuOpen, setMenuOpen] = useState(false)

    const hasQuestions = quiz.questionCount > 0

    /* Teachers and admins never get "Start over" or the review button (the API sends 0 for them) */
    const mistakeCount = isStudent ? quiz.mistakeCount : 0
    const startText = mistakeCount > 0 ? 'Start over' : isStudent ? 'Start quiz' : 'Try it'
    const missedText = `${mistakeCount} ${mistakeCount === 1 ? 'question' : 'questions'} you missed`

    /* The score ring in site.css reads the score from the --score CSS variable */
    const ringStyle = { '--score': quiz.bestScore ?? 0 } as CSSProperties

    return (
        <li className="col-md-6 col-lg-4">
            <div className="card border-0 shadow-sm h-100">
                <div className="card-body d-flex flex-column">
                    {/* Visibility and tags. The API only sends tags the user may see. */}
                    <div className="d-flex flex-wrap align-items-center gap-1 mb-2">
                        {/* An unpublished teacher quiz is a draft, a student's own quiz is always private */}
                        {quiz.isPublic ? (
                            <span className="badge bg-success">PUBLIC</span>
                        ) : (
                            <span className="badge bg-secondary">{quiz.awardsPoints ? 'DRAFT' : 'PRIVATE'}</span>
                        )}

                    </div>

                    <div className="d-flex align-items-start justify-content-between gap-2 mb-1">
                        <h2 className="h5 fw-bold mb-0">{quiz.title}</h2>

                        {/* Edit and delete in a menu, only for the creator or an administrator */}
                        {hasQuestions && quiz.canManage && (
                            <div className="dropdown flex-shrink-0">
                                <button
                                    type="button"
                                    className="btn btn-more"
                                    aria-expanded={menuOpen}
                                    aria-label={`More options for ${quiz.title}`}
                                    onClick={() => setMenuOpen((open) => !open)}
                                    onBlur={() => setTimeout(() => setMenuOpen(false), 150)}
                                >
                                    <i className="bi bi-three-dots-vertical btn-icon icon-more" aria-hidden="true"></i>
                                </button>

                                <ul className={`dropdown-menu dropdown-menu-end shadow-sm${menuOpen ? ' show' : ''}`} style={{ right: 0, top: '100%' }}>
                                    <li>
                                        <Link className="dropdown-item d-flex align-items-center gap-2" to={`/quizzes/${quiz.id}/edit`}>
                                            <i className="bi bi-pencil-square btn-icon" aria-hidden="true"></i>
                                            Edit
                                        </Link>
                                    </li>
                                    <li>
                                        <button
                                            type="button"
                                            className="dropdown-item text-danger d-flex align-items-center gap-2"
                                            onMouseDown={(event) => event.preventDefault()}
                                            onClick={() => {
                                                setMenuOpen(false)
                                                onDelete(quiz)
                                            }}
                                        >
                                            <i className="bi bi-trash3 btn-icon" aria-hidden="true"></i>
                                            Delete
                                        </button>
                                    </li>
                                </ul>
                            </div>
                        )}
                    </div>

                    <p className="text-secondary small mb-2">
                        <span className="fw-semibold">{quiz.moduleName}</span>
                        <span aria-hidden="true"> · </span>
                        <span>{quiz.difficulty}</span>
                        <span aria-hidden="true"> · </span>
                        {hasQuestions ? (
                            <span>{quiz.questionCount} {quiz.questionCount === 1 ? 'question' : 'questions'}</span>
                        ) : (
                            <span className="fst-italic">No questions yet</span>
                        )}
                    </p>

                    <p className="text-secondary small mb-2">
                        {quiz.isMine ? (
                            <span>Created by you</span>
                        ) : quiz.createdByName ? (
                            <span>Created by {quiz.createdByName}</span>
                        ) : (
                            <span className="fst-italic">Creator unavailable</span>
                        )}
                    </p>

                    <p className="text-secondary small flex-grow-1 mb-0">{quiz.description}</p>
                </div>

                {/*
                    Footer: start or retake, the review button with the number of mistakes, and the best score
                    (students) or the attempts and average (teachers and admins). A quiz without questions
                    gets Edit and Delete instead.
                */}
                {(hasQuestions || quiz.canManage) && (
                    <div className="card-footer bg-transparent d-flex align-items-center justify-content-between gap-2 py-3">
                        <div className="d-flex flex-wrap-reverse align-items-center gap-2">
                            {hasQuestions ? (
                                <>
                                    <Link
                                        className="btn btn-primary d-inline-flex align-items-center gap-2"
                                        to={`/quizzes/${quiz.id}/play`}
                                        aria-label={`${startText} ${quiz.title}`}
                                    >
                                        {/* "Start over" when there are mistakes to review, so the two buttons read as two choices */}
                                        {mistakeCount > 0 ? (
                                            <i className="bi bi-arrow-repeat btn-icon icon-restart" aria-hidden="true"></i>
                                        ) : (
                                            <i className="bi bi-play-fill btn-icon icon-play" aria-hidden="true"></i>
                                        )}
                                        {startText}
                                    </Link>

                                    {/* The number works like a notification. It goes straight into the mistakes from this quiz. */}
                                    {mistakeCount > 0 && (
                                        <Link
                                            className="btn btn-outline-secondary d-inline-flex align-items-center gap-2"
                                            to={`/review?quizId=${quiz.id}`}
                                            title={`Review ${missedText} last time`}
                                            aria-label={`Review ${missedText} in ${quiz.title}`}
                                        >
                                            <i className="bi bi-journal-check btn-icon icon-review" aria-hidden="true"></i>
                                            <span className="fw-semibold" aria-hidden="true">{mistakeCount}</span>
                                        </Link>
                                    )}
                                </>
                            ) : (
                                <>
                                    <Link
                                        className="btn btn-secondary d-inline-flex align-items-center gap-2"
                                        to={`/quizzes/${quiz.id}/edit`}
                                        aria-label={`Edit ${quiz.title}`}
                                    >
                                        <i className="bi bi-pencil-square btn-icon icon-edit" aria-hidden="true"></i>
                                        Edit
                                    </Link>
                                    <button
                                        type="button"
                                        className="btn btn-outline-danger d-inline-flex align-items-center gap-2"
                                        onClick={() => onDelete(quiz)}
                                        aria-label={`Delete ${quiz.title}`}
                                    >
                                        <i className="bi bi-trash3 btn-icon" aria-hidden="true"></i>
                                        Delete
                                    </button>
                                </>
                            )}
                        </div>

                        {/* Teachers and admins: how students do on this quiz, where students see their ring */}
                        {!isStudent && quiz.attempts !== null && (
                            <span className="small text-secondary text-end">
                                {quiz.attempts === 0
                                    ? 'No attempts yet'
                                    : `${quiz.attempts} ${quiz.attempts === 1 ? 'attempt' : 'attempts'} · avg ${quiz.averageScore ?? 0} %`}
                            </span>
                        )}

                        {/* Best score: grey ring, green with a check mark at 100 %. Students only. */}
                        {isStudent &&
                            hasQuestions &&
                            (quiz.bestScore !== null ? (
                                <div
                                    className={`score-ring${quiz.bestScore >= 100 ? ' is-aced' : ''}`}
                                    style={ringStyle}
                                    role="img"
                                    aria-label={`Your best: ${quiz.bestScore}%`}
                                    title={`Your best: ${quiz.bestScore}%`}
                                    tabIndex={0}
                                >
                                    <span>{quiz.bestScore >= 100 && <i className="bi bi-check-lg" aria-hidden="true"></i>}</span>
                                </div>
                            ) : (
                                <span className="small text-secondary">Not taken yet</span>
                            ))}
                    </div>
                )}
            </div>
        </li>
    )
}