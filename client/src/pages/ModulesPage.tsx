import { useCallback, useEffect, useState, type CSSProperties } from 'react'
import { Link, useLocation } from 'react-router-dom'
import ConfirmDialog from '../components/ConfirmDialog'
import ErrorAlert from '../components/ErrorAlert'
import LoadingSpinner from '../components/LoadingSpinner'
import ModuleCard from '../components/ModuleCard'
import SuccessToast from '../components/SuccessToast'
import { useAuth } from '../hooks/useAuth'
import { ApiError } from '../services/apiClient'
import { deleteModule, getModules, getModuleSummary } from '../services/moduleService'
import type { Module, ModuleSummary } from '../types/module'

/* The module page: the student's learning journey and the module cards. Same layout as Module/Index in Subapp 1. */
export default function ModulesPage() {
    const { hasRole } = useAuth()
    const location = useLocation()

    const isStudent = hasRole('Student')
    const canManage = hasRole('Teacher') || hasRole('Admin')

    const [modules, setModules] = useState<Module[] | null>(null)
    const [summary, setSummary] = useState<ModuleSummary | null>(null)
    /* True when the journey banner is loaded (or failed), so the page can show everything at once */
    const [summaryLoaded, setSummaryLoaded] = useState(false)
    const [loadError, setLoadError] = useState<string | null>(null)

    /* A message from the page that sent the user here, e.g. "Module created" */
    const [message, setMessage] = useState<string | null>((location.state as { message?: string } | null)?.message ?? null)
    const [actionError, setActionError] = useState<string | null>(null)
    const [moduleToDelete, setModuleToDelete] = useState<Module | null>(null)
    const [deleting, setDeleting] = useState(false)

    useEffect(() => {
        let active = true

        getModules()
            .then((result) => {
                if (active) setModules(result)
            })
            .catch(() => {
                if (active) setLoadError('The modules could not be loaded. Please try again.')
            })

        /* The journey is only for students. If it fails, the cards are still shown. */
        if (isStudent) {
            getModuleSummary()
                .then((result) => {
                    if (active) {
                        setSummary(result)
                        setSummaryLoaded(true)
                    }
                })
                .catch(() => {
                    if (active) setSummaryLoaded(true)
                })
        }

        return () => {
            active = false
        }
    }, [isStudent])

    const closeMessage = useCallback(() => setMessage(null), [])

    async function confirmDelete() {
        if (!moduleToDelete) return

        setDeleting(true)
        setActionError(null)
        try {
            await deleteModule(moduleToDelete.id)
            setModules((current) => current?.filter((module) => module.id !== moduleToDelete.id) ?? null)
            setMessage('Module deleted successfully.')
        } catch (err) {
            /* 409: the module still contains quizzes (the rule is checked on the server) */
            setActionError(err instanceof ApiError ? err.message : 'The module could not be deleted. Please try again.')
        } finally {
            setDeleting(false)
            setModuleToDelete(null)
        }
    }

    /* The title shows at once. The banner and the cards wait for each other, so they fade in together. */
    const ready = modules !== null && (!isStudent || summaryLoaded)
    const showJourney = isStudent && summary !== null && summary.totalOfficialQuizzes > 0

    /* The journey ring in site.css reads the percentage from the --p CSS variable */
    const ringStyle = { '--p': summary?.progressPercent ?? 0 } as CSSProperties

    return (
        <>
            <SuccessToast message={message} onClose={closeMessage} />
            <ErrorAlert message={actionError} onClose={() => setActionError(null)} />

            <section className="d-flex flex-wrap justify-content-between align-items-center gap-3">
                <div>
                    <h1 className="landing-title mb-2">Modules</h1>
                    <p className="lead text-secondary mb-4">Explore the learning areas in ITPE3200 Web Applications.</p>
                </div>

                {canManage && (
                    <Link className="btn btn-primary d-inline-flex align-items-center justify-content-center gap-2" to="/modules/new">
                        <i className="bi bi-plus-lg icon-plus" aria-hidden="true"></i>
                        Create module
                    </Link>
                )}
            </section>

            {ready && showJourney && summary && (
                <section className="card border-0 shadow-sm journey mb-4" aria-labelledby="journey-heading">
                    <div className="card-body p-4">
                        <div className="row g-4 align-items-center">
                            <div className="col-auto">
                                <div className="journey-ring" style={ringStyle} role="img" aria-label={`${summary.progressPercent} percent of all quizzes completed`}>
                                    <span className="journey-ring-value">{summary.progressPercent}%</span>
                                </div>
                            </div>

                            <div className="col">
                                <h2 id="journey-heading" className="h5 fw-bold mb-1">
                                    {summary.displayName ? `Welcome back, ${summary.displayName}` : 'Your learning journey'}
                                </h2>
                                <p className="text-secondary mb-3">{summary.message}</p>
                                <ul className="list-inline mb-0 journey-stats">
                                    <li className="list-inline-item">
                                        <strong>{summary.totalCompletedQuizzes}</strong> of {summary.totalOfficialQuizzes} quizzes
                                    </li>
                                    <li className="list-inline-item">
                                        <strong>{summary.modulesCompleted}</strong> of {summary.modulesWithQuizzes} modules completed
                                    </li>
                                    {summary.averageScore !== null && (
                                        <li className="list-inline-item">
                                            Average score <strong>{summary.averageScore}%</strong>
                                        </li>
                                    )}
                                </ul>
                            </div>

                            {summary.continueQuizId !== null && (
                                <div className="col-12 col-lg-auto">
                                    <Link
                                        className="btn btn-primary btn-lg d-inline-flex align-items-center justify-content-center gap-2 w-100"
                                        to={`/quizzes/${summary.continueQuizId}/play`}
                                        aria-label={`Continue with ${summary.continueQuizTitle} in ${summary.continueModuleName}`}
                                    >
                                        <i className="bi bi-play-fill" aria-hidden="true"></i>
                                        Continue
                                    </Link>
                                    <p className="small text-secondary text-lg-center mt-2 mb-0">{summary.continueQuizTitle}</p>
                                </div>
                            )}
                        </div>
                    </div>
                </section>
            )}

            {loadError ? (
                <ErrorAlert message={loadError} />
            ) : modules === null || !ready ? (
                <LoadingSpinner message="Loading modules..." />
            ) : modules.length === 0 ? (
                <div className="mx-auto text-center py-5" style={{ maxWidth: 520 }}>
                    <h2 className="h4 fw-bold mb-2">No modules available</h2>
                    <p className="text-secondary mb-0">The ITPE3200 learning structure has not been created yet.</p>
                </div>
            ) : (
                <div className="row g-3">
                    {modules.map((module) => (
                        <ModuleCard key={module.id} module={module} showProgress={isStudent} canManage={canManage} onDelete={setModuleToDelete} />
                    ))}
                </div>
            )}

            <ConfirmDialog
                show={moduleToDelete !== null}
                title="Delete module"
                message={
                    moduleToDelete && moduleToDelete.quizCount > 0 ? (
                        <p className="mb-0">
                            <strong>{moduleToDelete.name}</strong> contains {moduleToDelete.quizCount}{' '}
                            {moduleToDelete.quizCount === 1 ? 'quiz' : 'quizzes'}. A module with quizzes cannot be deleted. Move or delete the quizzes first.
                        </p>
                    ) : (
                        <p className="mb-0">
                            You are about to permanently delete <strong>{moduleToDelete?.name}</strong>.
                        </p>
                    )
                }
                confirmText="Delete module"
                busy={deleting}
                onConfirm={confirmDelete}
                onCancel={() => setModuleToDelete(null)}
            />
        </>
    )
}