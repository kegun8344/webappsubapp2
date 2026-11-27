import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useLocation, useSearchParams } from 'react-router-dom'
import ConfirmDialog from '../components/ConfirmDialog'
import ErrorAlert from '../components/ErrorAlert'
import LoadingSpinner from '../components/LoadingSpinner'
import SuccessToast from '../components/SuccessToast'
import { ApiError } from '../services/apiClient'
import { changeUser, getUsers } from '../services/adminService'
import type { AdminUser, UserRoleFilter } from '../types/admin'

const FILTER_ROLES = ['Student', 'Teacher'] as const

/* The users loaded for one address (and one reload), so an old answer is never shown for new filters */
interface LoadedUsers {
    key: string | null
    users: AdminUser[] | null
    error: string | null
}

/* The popup that asks before a role change or a lock. Unlock needs no popup, like in Subapp 1. */
type PendingAction =
    | { kind: 'make-teacher'; user: AdminUser }
    | { kind: 'remove-teacher'; user: AdminUser }
    | { kind: 'lock'; user: AdminUser }

/* Only known roles are used as a filter, the same as the API */
function readRole(value: string | null): UserRoleFilter {
    return FILTER_ROLES.find((role) => role.toLowerCase() === value?.toLowerCase()) ?? null
}

function roleBadgeClass(role: string): string {
    if (role === 'Admin') return 'bg-danger'
    if (role === 'Teacher') return 'bg-primary'
    return 'bg-secondary'
}

/* The text in the popup for each action, the same as the modals in AdminUsers/Index in Subapp 1 */
function dialogText(action: PendingAction) {
    const email = <strong>{action.user.email}</strong>

    switch (action.kind) {
        case 'make-teacher':
            return {
                title: 'Make teacher?',
                confirmText: 'Yes, make teacher',
                confirmClass: 'btn-primary',
                message: (
                    <>
                        <p className="mb-2">Are you sure you want to make {email} a teacher?</p>
                        <p className="text-secondary small mb-0">Teachers can create public quizzes that give points. The user is logged out.</p>
                    </>
                ),
            }
        case 'remove-teacher':
            return {
                title: 'Make student?',
                confirmText: 'Yes, make student',
                confirmClass: 'btn-danger',
                message: (
                    <>
                        <p className="mb-2">Are you sure you want to make {email} a student?</p>
                        <p className="text-secondary small mb-0">Quizzes they have already made are kept. The user is logged out.</p>
                    </>
                ),
            }
        case 'lock':
            return {
                title: 'Lock account?',
                confirmText: 'Yes, lock',
                confirmClass: 'btn-danger',
                message: (
                    <>
                        <p className="mb-2">Are you sure you want to lock {email}?</p>
                        <p className="text-secondary small mb-0">They are logged out and cannot log in until you unlock the account. Nothing is deleted.</p>
                    </>
                ),
            }
    }
}

/* What the green message says after each action */
function successText(kind: PendingAction['kind'] | 'unlock', email: string): string {
    switch (kind) {
        case 'make-teacher':
            return `${email} is now a teacher and has been logged out.`
        case 'remove-teacher':
            return `${email} is now a student and has been logged out.`
        case 'lock':
            return `${email} is locked and has been logged out.`
        case 'unlock':
            return `${email} can log in again.`
    }
}

/*
 * The administrator's Users page, the same as AdminUsers/Index in Subapp 1:
 * a role filter (Teachers gives the teacher overview), search on email, and per user
 * make teacher / make student, lock / unlock and delete. The filters are kept in the address.
 */
export default function UsersPage() {
    const location = useLocation()
    const [searchParams, setSearchParams] = useSearchParams()

    const role = readRole(searchParams.get('role'))
    const search = (searchParams.get('search') ?? '').trim()

    /* Changing reloadCount loads the list again after an action */
    const [reloadCount, setReloadCount] = useState(0)
    const queryKey = `${role ?? ''}|${search}|${reloadCount}`

    const [loaded, setLoaded] = useState<LoadedUsers>({ key: null, users: null, error: null })
    const [searchText, setSearchText] = useState(search)

    /* A message from the page that sent the user here, e.g. after a deletion */
    const [message, setMessage] = useState<string | null>((location.state as { message?: string } | null)?.message ?? null)
    const [actionError, setActionError] = useState<string | null>(null)

    const [pending, setPending] = useState<PendingAction | null>(null)
    const [busy, setBusy] = useState(false)

    const isLoading = loaded.key !== queryKey
    const users = loaded.users ?? []

    useEffect(() => {
        let active = true

        getUsers(role, search)
            .then((result) => {
                if (active) setLoaded({ key: queryKey, users: result, error: null })
            })
            .catch((err: unknown) => {
                if (active) {
                    setLoaded({
                        key: queryKey,
                        users: null,
                        error: err instanceof ApiError ? err.message : 'The users could not be loaded. Please try again.',
                    })
                }
            })

        return () => {
            active = false
        }
    }, [role, search, queryKey])

    const closeMessage = useCallback(() => setMessage(null), [])
    const closeDialog = useCallback(() => setPending(null), [])

    /* The role filter is kept when searching, and the search when changing the role */
    function filterLink(filterRole: UserRoleFilter): string {
        const params = new URLSearchParams()
        if (filterRole) params.set('role', filterRole)
        if (search) params.set('search', search)

        const query = params.toString()
        return query ? `/users?${query}` : '/users'
    }

    function onSearch(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()

        const params = new URLSearchParams(searchParams)
        const text = searchText.trim()

        if (text) {
            params.set('search', text)
        } else {
            params.delete('search')
        }

        setSearchParams(params)
    }

    /* Sends the action to the API, then shows the result and loads the list again */
    async function runAction(kind: PendingAction['kind'] | 'unlock', user: AdminUser) {
        setBusy(true)
        setActionError(null)

        try {
            await changeUser(user.id, kind)
            setMessage(successText(kind, user.email))
            setReloadCount((count) => count + 1)
        } catch (err) {
            setActionError(err instanceof ApiError ? err.message : 'The change could not be saved. Please try again.')
        } finally {
            setBusy(false)
            setPending(null)
        }
    }

    const dialog = pending ? dialogText(pending) : null

    return (
        <>
            <SuccessToast message={message} onClose={closeMessage} />

            {actionError && <ErrorAlert message={actionError} onClose={() => setActionError(null)} />}

            <h1 className="landing-title mb-2">Users</h1>
            <p className="lead text-secondary mb-4">Everyone registered in StudyQuest, with their role, quizzes and level.</p>

            {/* Role filter. Choosing Teachers gives the teacher overview. */}
            <nav className="mb-4" aria-label="User filters">
                <ul className="nav nav-pills gap-2">
                    <li className="nav-item">
                        <Link className={`nav-link ${role === null ? 'active' : ''}`} to={filterLink(null)} aria-current={role === null ? 'page' : undefined}>
                            All
                        </Link>
                    </li>
                    {FILTER_ROLES.map((filterRole) => (
                        <li className="nav-item" key={filterRole}>
                            <Link
                                className={`nav-link ${role === filterRole ? 'active' : ''}`}
                                to={filterLink(filterRole)}
                                aria-current={role === filterRole ? 'page' : undefined}
                            >
                                {filterRole}s
                            </Link>
                        </li>
                    ))}
                </ul>
            </nav>

            {/* Search on email */}
            <form onSubmit={onSearch} className="mb-3" role="search" style={{ maxWidth: '24rem' }}>
                <label htmlFor="user-search" className="visually-hidden">Search by email</label>
                <div className="input-group">
                    <span className="input-group-text bg-white"><i className="bi bi-search" aria-hidden="true"></i></span>
                    <input
                        id="user-search"
                        type="search"
                        className="form-control"
                        placeholder="Search by email"
                        autoComplete="off"
                        maxLength={256}
                        value={searchText}
                        onChange={(event) => setSearchText(event.target.value)}
                    />
                    <button type="submit" className="btn btn-primary">Search</button>
                </div>
            </form>

            {isLoading ? (
                <LoadingSpinner />
            ) : loaded.error ? (
                <ErrorAlert message={loaded.error} />
            ) : (
                <>
                    <p className="text-secondary small mb-2">
                        <strong>{users.length}</strong> {users.length === 1 ? 'user' : 'users'}
                    </p>

                    {users.length === 0 ? (
                        <p className="text-secondary fst-italic">No users match this filter.</p>
                    ) : (
                        <div className="table-responsive">
                            <table className="table table-hover align-middle">
                                <thead>
                                    <tr>
                                        <th scope="col">Email</th>
                                        <th scope="col">Role</th>
                                        <th scope="col" className="text-end">Quizzes created</th>
                                        <th scope="col" className="text-end">Published</th>
                                        <th scope="col" className="text-end">Quizzes completed</th>
                                        <th scope="col" className="text-end">Level</th>
                                        <th scope="col"><span className="visually-hidden">Actions</span></th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {users.map((user) => {
                                        const isTeacher = user.roles.includes('Teacher')

                                        return (
                                            <tr key={user.id}>
                                                <td className="text-break">{user.email}</td>
                                                <td>
                                                    {user.isLocked && (
                                                        <span className="badge bg-dark me-1">
                                                            <i className="bi bi-lock-fill me-1" aria-hidden="true"></i>Locked
                                                        </span>
                                                    )}
                                                    {user.roles.map((roleName) => (
                                                        <span key={roleName} className={`badge ${roleBadgeClass(roleName)} me-1`}>{roleName}</span>
                                                    ))}
                                                </td>
                                                <td className="text-end">{user.quizzesCreated}</td>
                                                <td className="text-end">{user.quizzesPublished}</td>
                                                <td className="text-end">{user.quizzesCompleted}</td>
                                                <td className="text-end">
                                                    <span className="fw-semibold">{user.level}</span>{' '}
                                                    <span className="text-secondary small">({user.totalPoints} pts)</span>
                                                </td>

                                                {/* Change role, lock or unlock, then delete. Administrators are never listed. */}
                                                <td className="text-end text-nowrap">
                                                    {isTeacher ? (
                                                        <button
                                                            type="button"
                                                            className="btn btn-outline-secondary btn-sm me-1"
                                                            aria-label={`Make ${user.email} a student`}
                                                            onClick={() => setPending({ kind: 'remove-teacher', user })}
                                                        >
                                                            Make student
                                                        </button>
                                                    ) : (
                                                        <button
                                                            type="button"
                                                            className="btn btn-outline-primary btn-sm me-1"
                                                            aria-label={`Make ${user.email} a teacher`}
                                                            onClick={() => setPending({ kind: 'make-teacher', user })}
                                                        >
                                                            Make teacher
                                                        </button>
                                                    )}

                                                    {user.isLocked ? (
                                                        <button
                                                            type="button"
                                                            className="btn btn-outline-primary btn-sm me-1"
                                                            aria-label={`Unlock ${user.email}`}
                                                            disabled={busy}
                                                            onClick={() => void runAction('unlock', user)}
                                                        >
                                                            Unlock
                                                        </button>
                                                    ) : (
                                                        <button
                                                            type="button"
                                                            className="btn btn-outline-secondary btn-sm me-1"
                                                            aria-label={`Lock ${user.email}`}
                                                            onClick={() => setPending({ kind: 'lock', user })}
                                                        >
                                                            Lock
                                                        </button>
                                                    )}

                                                    {user.canDelete && (
                                                        <Link className="btn btn-outline-danger btn-sm" to={`/users/${encodeURIComponent(user.id)}/delete`} aria-label={`Delete user ${user.email}`}>
                                                            Delete
                                                        </Link>
                                                    )}
                                                </td>
                                            </tr>
                                        )
                                    })}
                                </tbody>
                            </table>
                        </div>
                    )}
                </>
            )}

            <ConfirmDialog
                show={pending !== null}
                title={dialog?.title ?? ''}
                message={dialog?.message}
                confirmText={dialog?.confirmText}
                confirmClass={dialog?.confirmClass}
                busy={busy}
                onConfirm={() => {
                    if (pending) void runAction(pending.kind, pending.user)
                }}
                onCancel={closeDialog}
            />
        </>
    )
}