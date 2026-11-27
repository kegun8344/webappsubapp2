import { useState } from 'react'
import { Link } from 'react-router-dom'
import type { Module } from '../types/module'

interface ModuleCardProps {
    module: Module
    /* Students see their progress, teachers and administrators do not have any */
    showProgress: boolean
    /* Teachers and administrators get the edit and delete menu */
    canManage: boolean
    onDelete: (module: Module) => void
}

/* One module card, the same markup and classes as Views/Module/Index.cshtml in Subapp 1 */
export default function ModuleCard({ module, showProgress, canManage, onDelete }: ModuleCardProps) {
    const [menuOpen, setMenuOpen] = useState(false)

    return (
        <div className="col-md-6 col-lg-4">
            <div className={`card border-0 shadow-sm h-100 module-card module-color-${module.colorIndex}`}>
                <div className="card-body d-flex flex-column">
                    <div className="d-flex justify-content-between align-items-start gap-2 mb-2">
                        <div className="d-flex align-items-center gap-3 min-w-0-lb">
                            <span className="module-icon" aria-hidden="true">
                                <i className={`bi ${module.iconKey}`}></i>
                            </span>
                            <h2 className="h5 fw-bold mb-0 text-break">{module.name}</h2>
                        </div>

                        {canManage && (
                            <div className="dropdown flex-shrink-0">
                                <button
                                    type="button"
                                    className="btn btn-more"
                                    aria-expanded={menuOpen}
                                    aria-label={`More options for ${module.name}`}
                                    onClick={() => setMenuOpen((open) => !open)}
                                    onBlur={() => setTimeout(() => setMenuOpen(false), 150)}
                                >
                                    <i className="bi bi-three-dots-vertical btn-icon icon-more" aria-hidden="true"></i>
                                </button>
                                <ul className={`dropdown-menu dropdown-menu-end shadow-sm${menuOpen ? ' show' : ''}`} style={{ right: 0, top: '100%' }}>
                                    <li>
                                        <Link className="dropdown-item" to={`/modules/${module.id}/edit`}>Edit</Link>
                                    </li>
                                    <li>
                                        <button
                                            type="button"
                                            className="dropdown-item text-danger"
                                            onMouseDown={(event) => event.preventDefault()}
                                            onClick={() => {
                                                setMenuOpen(false)
                                                onDelete(module)
                                            }}
                                        >
                                            Delete
                                        </button>
                                    </li>
                                </ul>
                            </div>
                        )}
                    </div>

                    <p className="text-secondary flex-grow-1">
                        {module.description ? module.description : <span className="fst-italic">No description.</span>}
                    </p>

                    {/*
                        Every card has the same rows below the description (progress, score, action),
                        so the progress bars line up across the cards whatever the module contains.
                    */}
                    {showProgress && (
                        <>
                            <div className="mb-2">
                                <div className="d-flex justify-content-between align-items-center small text-secondary mb-1">
                                    <span>Your progress</span>
                                    <span>
                                        {module.completedOfficialQuizCount} / {module.officialQuizCount}
                                    </span>
                                </div>
                                <div
                                    className="progress"
                                    role="progressbar"
                                    aria-label={`Progress in ${module.name}`}
                                    aria-valuenow={module.progressPercent}
                                    aria-valuemin={0}
                                    aria-valuemax={100}
                                    style={{ height: '.5rem' }}
                                >
                                    <div className={`progress-bar${module.isCompleted ? ' bg-success' : ''}`} style={{ width: `${module.progressPercent}%` }}></div>
                                </div>
                            </div>

                            {/* Mastery stars come from the average of the best scores */}
                            <div className="d-flex align-items-center gap-2 mb-3 card-slot small">
                                {module.averageBestScore !== null ? (
                                    <>
                                        <span className="module-stars" aria-hidden="true">
                                            {[1, 2, 3].map((star) => (
                                                <i key={star} className={`bi ${star <= module.masteryStars ? 'bi-star-fill' : 'bi-star'}`}></i>
                                            ))}
                                        </span>
                                        <span className="visually-hidden">Mastery: {module.masteryStars} of 3 stars.</span>
                                        <span className="text-secondary">
                                            Average score <strong className="text-body">{module.averageBestScore}%</strong>
                                        </span>
                                    </>
                                ) : module.officialQuizCount > 0 ? (
                                    <span className="text-secondary">No attempts yet</span>
                                ) : (
                                    <span className="text-secondary">No quizzes from teachers yet</span>
                                )}
                            </div>

                            {/* The easiest quiz the student has not taken yet */}
                            <div className="mb-3 card-slot">
                                {module.nextQuizId !== null ? (
                                    <Link
                                        className="btn btn-primary btn-sm d-flex align-items-center justify-content-center gap-2 w-100"
                                        to={`/quizzes/${module.nextQuizId}/play`}
                                        aria-label={`Take the quiz ${module.nextQuizTitle}`}
                                    >
                                        <i className="bi bi-play-fill" aria-hidden="true"></i>
                                        <span className="text-truncate">
                                            {module.completedOfficialQuizCount === 0 ? 'Start' : 'Next'}: {module.nextQuizTitle}
                                        </span>
                                    </Link>
                                ) : (
                                    module.isCompleted && (
                                        <div className="small text-success d-flex align-items-center justify-content-center gap-2 h-100">
                                            <i className="bi bi-check-circle-fill" aria-hidden="true"></i>
                                            All quizzes completed
                                        </div>
                                    )
                                )}
                            </div>
                        </>
                    )}

                    <div className="d-flex justify-content-between align-items-center gap-2">
                        <p className="small text-secondary mb-0">
                            <strong>{module.quizCount}</strong> {module.quizCount === 1 ? 'quiz' : 'quizzes'}
                        </p>
                        <Link className="btn btn-outline-primary btn-sm" to={`/quizzes?moduleId=${module.id}`} aria-label={`Explore quizzes in ${module.name}`}>
                            Explore
                        </Link>
                    </div>
                </div>
            </div>
        </div>
    )
}