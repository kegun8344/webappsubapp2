import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import ConfirmDialog from '../components/ConfirmDialog'
import ErrorAlert from '../components/ErrorAlert'
import LoadingSpinner from '../components/LoadingSpinner'
import QuizCard from '../components/QuizCard'
import { ApiError } from '../services/apiClient'
import { deleteQuiz, getModuleOptions, getQuizzes, getTags } from '../services/quizService'
import { difficultyLevels, type Difficulty, type ModuleOption, type QuizFilters, type QuizListItem, type QuizView, type Tag } from '../types/quiz'

/* How long to wait after the last key press before searching */
const SEARCH_DELAY_MS = 300

/*
 * The filters are kept in the address (?view=mine&difficulty=Easy), like the GET form in Subapp 1.
 * Then a filtered list can be bookmarked, and the back button works.
 * Unknown values in the address are ignored, and the API checks them again.
 */
function readFilters(params: URLSearchParams): QuizFilters {
    const view = params.get('view')
    const moduleId = Number(params.get('moduleId'))
    const tagId = Number(params.get('tagId'))
    const difficulty = params.get('difficulty')

    return {
        view: view === 'public' || view === 'mine' ? view : 'all',
        moduleId: moduleId > 0 ? moduleId : undefined,
        tagId: tagId > 0 ? tagId : undefined,
        search: params.get('search')?.trim() || undefined,
        difficulty: difficultyLevels.find((level) => level === difficulty),
    }
}

function errorMessage(err: unknown, fallback: string): string {
    return err instanceof ApiError ? err.message : fallback
}

/* The quizzes loaded for one address, so an old answer is never shown for new filters */
interface LoadedQuizzes {
    /* The address the quizzes belong to. null before the first answer has arrived. */
    key: string | null
    quizzes: QuizListItem[] | null
    error: string | null
}

const tabs: { view: QuizView; label: string }[] = [
    { view: 'all', label: 'All' },
    { view: 'public', label: 'Public' },
    { view: 'mine', label: 'My quizzes' },
]

/* The quiz list with tabs, search and filters, same layout as Quiz/Index in Subapp 1 */
export default function QuizzesPage() {
    const [searchParams, setSearchParams] = useSearchParams()
    const queryKey = searchParams.toString()
    const filters = readFilters(searchParams)

    const [loaded, setLoaded] = useState<LoadedQuizzes>({ key: null, quizzes: null, error: null })
    const [modules, setModules] = useState<ModuleOption[]>([])
    const [tags, setTags] = useState<Tag[]>([])

    /* What is typed in the search box. The list is filtered a moment after the user stops typing. */
    const [searchText, setSearchText] = useState(filters.search ?? '')

    /* On small screens the filters are behind a button. They start open if one is in use. */
    const [filtersOpen, setFiltersOpen] = useState(Boolean(filters.moduleId || filters.tagId || filters.difficulty))

    const [quizToDelete, setQuizToDelete] = useState<QuizListItem | null>(null)
    const [deleting, setDeleting] = useState(false)
    const [actionError, setActionError] = useState<string | null>(null)
    const [successMessage, setSuccessMessage] = useState<string | null>(null)

    /* Loading means: the list on screen belongs to other filters than the ones in the address */
    const isLoading = loaded.key !== queryKey
    const quizzes = loaded.quizzes ?? []
    const hasActiveFilters = Boolean(filters.moduleId || filters.tagId || filters.difficulty || filters.search)

    /* Changes some filters and keeps the others. null removes a filter. */
    const updateFilters = useCallback(
        (changes: Record<string, string | null>) => {
            setSearchParams(
                (current) => {
                    const next = new URLSearchParams(current)
                    for (const [name, value] of Object.entries(changes)) {
                        if (value) next.set(name, value)
                        else next.delete(name)
                    }
                    return next
                },
                { replace: true },
            )
        },
        [setSearchParams],
    )

    /* Loads the quizzes every time the filters in the address change */
    useEffect(() => {
        let active = true

        getQuizzes(readFilters(new URLSearchParams(queryKey)))
            .then((result) => {
                if (active) setLoaded({ key: queryKey, quizzes: result, error: null })
            })
            .catch((err: unknown) => {
                if (active) setLoaded({ key: queryKey, quizzes: null, error: errorMessage(err, 'The quizzes could not be loaded.') })
            })

        return () => {
            active = false
        }
    }, [queryKey])

    /* The choices in the module and tag filters. If they fail, the filters are just empty. */
    useEffect(() => {
        let active = true

        getModuleOptions()
            .then((result) => {
                if (active) setModules(result)
            })
            .catch(() => {})

        getTags()
            .then((result) => {
                if (active) setTags(result)
            })
            .catch(() => {})

        return () => {
            active = false
        }
    }, [])

    /* Search while typing: waits until the user has stopped typing for a moment */
    useEffect(() => {
        const current = searchParams.get('search') ?? ''
        if (searchText.trim() === current) return

        const timer = setTimeout(() => updateFilters({ search: searchText.trim() || null }), SEARCH_DELAY_MS)
        return () => clearTimeout(timer)
    }, [searchText, searchParams, updateFilters])

    /* The success message hides itself after a few seconds, like the toast in Subapp 1 */
    useEffect(() => {
        if (!successMessage) return

        const timer = setTimeout(() => setSuccessMessage(null), 4000)
        return () => clearTimeout(timer)
    }, [successMessage])

    function handleSearch(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()
        updateFilters({ search: searchText.trim() || null })
    }

    function clearFilters() {
        setSearchText('')
        updateFilters({ search: null, moduleId: null, difficulty: null, tagId: null })
    }

    async function confirmDelete() {
        if (!quizToDelete) return

        setDeleting(true)
        setActionError(null)

        try {
            await deleteQuiz(quizToDelete.id)
            /* Remove the card straight away instead of loading the whole list again */
            setLoaded((current) => ({
                ...current,
                quizzes: current.quizzes?.filter((quiz) => quiz.id !== quizToDelete.id) ?? null,
            }))
            setSuccessMessage('Quiz deleted successfully.')
        } catch (err) {
            setActionError(errorMessage(err, 'The quiz could not be deleted. Please try again.'))
        } finally {
            setDeleting(false)
            setQuizToDelete(null)
        }
    }

    return (
        <>
            {successMessage && (
                <div className="alert alert-success d-flex align-items-center justify-content-between toast-message shadow" role="alert">
                    <span>{successMessage}</span>
                    <button type="button" className="btn-close" aria-label="Close" onClick={() => setSuccessMessage(null)}></button>
                </div>
            )}

            <ErrorAlert message={actionError} onClose={() => setActionError(null)} />

            <section className="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
                <div>
                    <h1 className="landing-title mb-2">Quizzes</h1>
                    <p className="lead text-secondary mb-4">Choose a quiz to get started.</p>
                </div>

                <Link className="btn btn-primary d-inline-flex align-items-center justify-content-center gap-2" to="/quizzes/new">
                    <i className="bi bi-plus-lg icon-plus" aria-hidden="true"></i>
                    Create quiz
                </Link>
            </section>

            {/* The tabs only narrow the quizzes the API has already decided the user may see */}
            <nav className="mb-4" aria-label="Quiz filters">
                <ul className="nav nav-pills gap-2">
                    {tabs.map((tab) => (
                        <li className="nav-item" key={tab.view}>
                            <button
                                type="button"
                                className={`nav-link${filters.view === tab.view ? ' active' : ''}`}
                                aria-current={filters.view === tab.view ? 'page' : undefined}
                                onClick={() => updateFilters({ view: tab.view === 'all' ? null : tab.view })}
                            >
                                {tab.label}
                            </button>
                        </li>
                    ))}
                </ul>
            </nav>

            <form className="mb-4" role="search" onSubmit={handleSearch}>
                <div className="row g-3 align-items-end">
                    <div className="col-12 col-lg-4">
                        <label htmlFor="search" className="form-label fw-semibold">Search</label>
                        <div className="input-group">
                            <input
                                id="search"
                                type="search"
                                className="form-control"
                                placeholder="Title or description"
                                maxLength={100}
                                value={searchText}
                                onChange={(event) => setSearchText(event.target.value)}
                            />
                            <button type="submit" className="btn btn-primary">Search</button>
                        </div>
                    </div>

                    {/* Small screens only: opens and closes the filters */}
                    <div className="col-12 d-lg-none">
                        <button
                            type="button"
                            className="btn btn-outline-secondary d-inline-flex align-items-center gap-2"
                            aria-expanded={filtersOpen}
                            aria-controls="quiz-filters"
                            onClick={() => setFiltersOpen((open) => !open)}
                        >
                            <i className="bi bi-sliders" aria-hidden="true"></i>
                            Filters
                        </button>
                    </div>

                    <div id="quiz-filters" className={`col-12 col-lg-8 d-lg-block mt-0 mt-lg-3${filtersOpen ? '' : ' d-none'}`}>
                        <div className="row g-3 align-items-end pt-3 pt-lg-0">
                            <div className="col-6 col-lg-3">
                                <label htmlFor="moduleId" className="form-label fw-semibold">Module</label>
                                <select
                                    id="moduleId"
                                    className="form-select"
                                    value={filters.moduleId ?? ''}
                                    onChange={(event) => updateFilters({ moduleId: event.target.value || null })}
                                >
                                    <option value="">All modules</option>
                                    {modules.map((module) => (
                                        <option key={module.id} value={module.id}>{module.name}</option>
                                    ))}
                                </select>
                            </div>

                            <div className="col-6 col-lg-3">
                                <label htmlFor="difficulty" className="form-label fw-semibold">Difficulty</label>
                                <select
                                    id="difficulty"
                                    className="form-select"
                                    value={filters.difficulty ?? ''}
                                    onChange={(event) => updateFilters({ difficulty: (event.target.value as Difficulty) || null })}
                                >
                                    <option value="">Any</option>
                                    {difficultyLevels.map((level) => (
                                        <option key={level} value={level}>{level}</option>
                                    ))}
                                </select>
                            </div>

                            <div className="col-6 col-lg-3">
                                <label htmlFor="tagId" className="form-label fw-semibold">Tag</label>
                                <select
                                    id="tagId"
                                    className="form-select"
                                    value={filters.tagId ?? ''}
                                    onChange={(event) => updateFilters({ tagId: event.target.value || null })}
                                >
                                    <option value="">Any</option>
                                    {/* Shared tags first, then the user's own, like the groups in Subapp 1 */}
                                    <optgroup label="Shared tags">
                                        {tags.filter((tag) => tag.isShared).map((tag) => (
                                            <option key={tag.id} value={tag.id}>{tag.name}</option>
                                        ))}
                                    </optgroup>
                                    {tags.some((tag) => !tag.isShared) && (
                                        <optgroup label="My tags">
                                            {tags.filter((tag) => !tag.isShared).map((tag) => (
                                                <option key={tag.id} value={tag.id}>{tag.name}</option>
                                            ))}
                                        </optgroup>
                                    )}
                                </select>
                            </div>

                            <div className="col-6 col-lg-3">
                                {hasActiveFilters && (
                                    <button type="button" className="btn btn-outline-secondary" onClick={clearFilters}>
                                        Clear
                                    </button>
                                )}
                            </div>
                        </div>
                    </div>
                </div>

                {/* Only shown while a search or a filter is in use */}
                <p className={`text-secondary small mt-3 mb-0${hasActiveFilters && !isLoading ? '' : ' invisible'}`} role="status">
                    <strong>{quizzes.length}</strong> {quizzes.length === 1 ? 'quiz found' : 'quizzes found'}
                </p>
            </form>

            {isLoading ? (
                <LoadingSpinner message="Loading quizzes..." />
            ) : loaded.error ? (
                <ErrorAlert message={loaded.error} />
            ) : quizzes.length === 0 ? (
                <EmptyState view={filters.view} hasActiveFilters={hasActiveFilters} onClear={clearFilters} />
            ) : (
                <ul className="row g-3 list-unstyled">
                    {quizzes.map((quiz) => (
                        <QuizCard key={quiz.id} quiz={quiz} onDelete={setQuizToDelete} />
                    ))}
                </ul>
            )}

            <ConfirmDialog
                show={quizToDelete !== null}
                title="Delete quiz"
                message={
                    <>
                        <p>
                            Are you sure you want to delete <strong>{quizToDelete?.title}</strong>?
                        </p>
                        <p className="text-secondary small mb-0">The questions in the quiz are deleted too. This cannot be undone.</p>
                    </>
                }
                busy={deleting}
                onConfirm={confirmDelete}
                onCancel={() => setQuizToDelete(null)}
            />
        </>
    )
}

interface EmptyStateProps {
    view: QuizView
    hasActiveFilters: boolean
    onClear: () => void
}

/* What to show when the list is empty. The text depends on why it is empty. */
function EmptyState({ view, hasActiveFilters, onClear }: EmptyStateProps) {
    const createButton = (
        <Link className="btn btn-primary d-inline-flex align-items-center justify-content-center gap-2" to="/quizzes/new">
            <i className="bi bi-plus-lg icon-plus" aria-hidden="true"></i>
            Create quiz
        </Link>
    )

    return (
        <div className="mx-auto text-center py-5" style={{ maxWidth: 520 }}>
            {hasActiveFilters ? (
                <>
                    <h2 className="h4 fw-bold mb-2">No quizzes match your search</h2>
                    <p className="text-secondary mb-4">Try other words, or remove some of the filters.</p>
                    <button type="button" className="btn btn-outline-primary" onClick={onClear}>
                        Clear search and filters
                    </button>
                </>
            ) : view === 'public' ? (
                <>
                    <h2 className="h4 fw-bold mb-2">No public quizzes found</h2>
                    <p className="text-secondary mb-0">There are currently no public quizzes available.</p>
                </>
            ) : view === 'mine' ? (
                <>
                    <h2 className="h4 fw-bold mb-2">You haven't created any quizzes yet</h2>
                    <p className="text-secondary mb-4">Create a quiz to start building your own practice material.</p>
                    {createButton}
                </>
            ) : (
                <>
                    <h2 className="h4 fw-bold mb-2">There are no quizzes available yet</h2>
                    <p className="text-secondary mb-4">Create the first quiz to get started.</p>
                    {createButton}
                </>
            )}
        </div>
    )
}