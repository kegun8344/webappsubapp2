import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import ErrorAlert from '../components/ErrorAlert'
import LoadingSpinner from '../components/LoadingSpinner'
import { ApiError } from '../services/apiClient'
import { deleteUser, getDeletionPreview } from '../services/adminService'
import type { UserDeletionPreview } from '../types/admin'

interface LoadedPreview {
    id: string | null
    preview: UserDeletionPreview | null
    error: string | null
}

/*
 * "Delete user?" with what is lost, the same as AdminUsers/Delete in Subapp 1.
 * If the user cannot be deleted, the server's reason is shown instead.
 */
export default function DeleteUserPage() {
    const userId = useParams().id ?? ''
    const navigate = useNavigate()

    const [loaded, setLoaded] = useState<LoadedPreview>({ id: null, preview: null, error: null })
    const [deleting, setDeleting] = useState(false)
    const [deleteError, setDeleteError] = useState<string | null>(null)

    const isLoading = loaded.id !== userId

    useEffect(() => {
        let active = true

        getDeletionPreview(userId)
            .then((preview) => {
                if (active) setLoaded({ id: userId, preview, error: null })
            })
            .catch((err: unknown) => {
                if (!active) return

                const error = err instanceof ApiError && err.status === 404
                    ? 'This user was not found.'
                    : err instanceof ApiError
                        ? err.message
                        : 'The user could not be loaded. Please try again.'

                setLoaded({ id: userId, preview: null, error })
            })

        return () => {
            active = false
        }
    }, [userId])

    async function onDelete(preview: UserDeletionPreview) {
        setDeleting(true)
        setDeleteError(null)

        try {
            await deleteUser(preview.id)
            navigate('/users', { state: { message: `The user ${preview.email} was deleted.` } })
        } catch (err) {
            setDeleteError(err instanceof ApiError ? err.message : 'The user could not be deleted. Please try again.')
            setDeleting(false)
        }
    }

    if (isLoading) {
        return <LoadingSpinner />
    }

    const preview = loaded.preview

    if (!preview) {
        return (
            <div className="mx-auto text-center py-5" style={{ maxWidth: 520 }}>
                <ErrorAlert message={loaded.error} />
                <Link to="/users" className="btn btn-outline-primary">Back to users</Link>
            </div>
        )
    }

    return (
        <div className="mx-auto py-4" style={{ maxWidth: 600 }}>
            <h1 className="landing-title mb-3">Delete user?</h1>

            {deleteError && <ErrorAlert message={deleteError} onClose={() => setDeleteError(null)} />}

            <div className="card border-0 shadow-sm mb-4">
                <div className="card-body">
                    <p className="mb-1 fw-semibold text-break">{preview.email}</p>
                    <p className="text-secondary small mb-3">Role: {preview.roles.length === 0 ? 'None' : preview.roles.join(', ')}</p>

                    <p className="mb-2">This is what happens when the account is deleted:</p>
                    <ul className="mb-0">
                        <li>
                            <strong>{preview.privateQuizzes}</strong> private {preview.privateQuizzes === 1 ? 'quiz is' : 'quizzes are'} deleted.
                        </li>
                        <li>
                            <strong>{preview.publicQuizzes}</strong> public {preview.publicQuizzes === 1 ? 'quiz stays' : 'quizzes stay'} available, but without a creator.
                        </li>
                        <li>
                            <strong>{preview.attempts}</strong> quiz {preview.attempts === 1 ? 'attempt and' : 'attempts and'} <strong>{preview.points}</strong> points are removed,
                            so the user leaves the leaderboard.
                        </li>
                        <li>Personal tags created by the user are deleted.</li>
                    </ul>
                </div>
            </div>

            <p className="text-danger fw-semibold">This cannot be undone.</p>

            <div className="d-flex gap-2">
                <button type="button" className="btn btn-danger" disabled={deleting} onClick={() => void onDelete(preview)}>
                    {deleting ? 'Deleting...' : 'Delete user'}
                </button>
                <Link to="/users" className="btn btn-outline-secondary">Cancel</Link>
            </div>
        </div>
    )
}