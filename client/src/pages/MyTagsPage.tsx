import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import ConfirmDialog from '../components/ConfirmDialog'
import ErrorAlert from '../components/ErrorAlert'
import LoadingSpinner from '../components/LoadingSpinner'
import SuccessToast from '../components/SuccessToast'
import { ApiError } from '../services/apiClient'
import { deleteMyTag, getMyTags, renameMyTag } from '../services/quizService'
import type { MyTag } from '../types/quiz'

/* The user's own tags: rename or delete them. The same page as Tag/Manage in Subapp 1. */
export default function MyTagsPage() {
    const [tags, setTags] = useState<MyTag[] | null>(null)
    const [loadError, setLoadError] = useState<string | null>(null)
    const [error, setError] = useState<string | null>(null)
    const [message, setMessage] = useState<string | null>(null)

    /* The tag being renamed, and the new name in the text field */
    const [editingId, setEditingId] = useState<number | null>(null)
    const [newName, setNewName] = useState('')
    const [nameError, setNameError] = useState<string | null>(null)

    /* The tag waiting for "Delete tag?" */
    const [deleting, setDeleting] = useState<MyTag | null>(null)
    const [busy, setBusy] = useState(false)

    const closeMessage = useCallback(() => setMessage(null), [])

    useEffect(() => {
        let active = true

        getMyTags()
            .then((result) => {
                if (active) setTags(result)
            })
            .catch(() => {
                if (active) setLoadError('Your tags could not be loaded. Please try again.')
            })

        return () => {
            active = false
        }
    }, [])

    function startRename(tag: MyTag) {
        setEditingId(tag.id)
        setNewName(tag.name)
        setNameError(null)
        setError(null)
    }

    async function handleRename(event: FormEvent<HTMLFormElement>, tag: MyTag) {
        event.preventDefault()

        const trimmed = newName.trim()
        if (trimmed.length === 0 || trimmed.length > 30) {
            setNameError('A tag name must be between 1 and 30 characters.')
            return
        }

        setBusy(true)
        try {
            const renamed = await renameMyTag(tag.id, trimmed)
            setTags((current) => current?.map((item) => (item.id === tag.id ? { ...item, name: renamed.name } : item)) ?? null)
            setEditingId(null)
            setMessage(`Tag renamed to "${renamed.name}".`)
        } catch (err) {
            /* E.g. "A tag called "Theory" already exists." from the server */
            setNameError(err instanceof ApiError ? (err.fieldErrors.name?.[0] ?? err.message) : 'The tag could not be renamed. Please try again.')
        } finally {
            setBusy(false)
        }
    }

    async function handleDelete() {
        if (!deleting) return

        const tag = deleting
        setBusy(true)
        try {
            await deleteMyTag(tag.id)
            setTags((current) => current?.filter((item) => item.id !== tag.id) ?? null)
            setMessage(`Tag "${tag.name}" deleted.`)
        } catch {
            setError('The tag could not be deleted. Please try again.')
        } finally {
            setBusy(false)
            setDeleting(null)
        }
    }

    if (!tags && !loadError) {
        return <LoadingSpinner message="Loading your tags..." />
    }

    return (
        <div className="page-narrow">
            <SuccessToast message={message} onClose={closeMessage} />

            <Link to="/quizzes" className="btn btn-outline-secondary d-inline-flex align-items-center gap-2 mb-3">
                <i className="bi bi-arrow-left" aria-hidden="true"></i>
                Back to quizzes
            </Link>

            <h1 className="landing-title mb-2">My tags</h1>
            <p className="lead text-secondary mb-4">
                Tags only you can see. Course tags are shared with everyone and cannot be changed here.
            </p>

            <ErrorAlert message={loadError} />
            <ErrorAlert message={error} onClose={() => setError(null)} />

            {tags && tags.length === 0 && (
                <div className="card border-0 shadow-sm">
                    <div className="card-body p-4">
                        <p className="text-secondary mb-0">
                            You have no tags of your own. You can create them on the edit page of your quizzes.
                        </p>
                    </div>
                </div>
            )}

            {tags && tags.length > 0 && (
                <ul className="list-group shadow-sm">
                    {tags.map((tag) => (
                        <li key={tag.id} className="list-group-item py-3">
                            {editingId === tag.id ? (
                                /* Rename happens in the row itself, Enter saves and Cancel goes back */
                                <form className="d-flex flex-wrap align-items-start gap-2" onSubmit={(event) => handleRename(event, tag)} noValidate>
                                    <div className="flex-grow-1">
                                        <label htmlFor={`rename-${tag.id}`} className="visually-hidden">New name for {tag.name}</label>
                                        <input
                                            id={`rename-${tag.id}`}
                                            className={`form-control${nameError ? ' is-invalid' : ''}`}
                                            maxLength={30}
                                            value={newName}
                                            autoFocus
                                            aria-invalid={Boolean(nameError)}
                                            aria-describedby={nameError ? `rename-error-${tag.id}` : undefined}
                                            onChange={(event) => {
                                                setNewName(event.target.value)
                                                setNameError(null)
                                            }}
                                        />
                                        {nameError && <div id={`rename-error-${tag.id}`} className="invalid-feedback">{nameError}</div>}
                                    </div>
                                    <button type="submit" className="btn btn-primary" disabled={busy}>Save</button>
                                    <button type="button" className="btn btn-outline-secondary" onClick={() => setEditingId(null)}>Cancel</button>
                                </form>
                            ) : (
                                <div className="d-flex align-items-center gap-3">
                                    {/* Tags are only for filtering, so they are all grey like in Subapp 1 */}
                                    <span className="badge text-bg-secondary">{tag.name}</span>
                                    <span className="flex-grow-1 small text-secondary">
                                        Used on {tag.quizCount} {tag.quizCount === 1 ? 'quiz' : 'quizzes'}
                                    </span>
                                    <button type="button" className="btn btn-outline-secondary btn-sm" aria-label={`Rename tag ${tag.name}`} onClick={() => startRename(tag)}>
                                        Rename
                                    </button>
                                    <button type="button" className="btn btn-outline-danger btn-sm" aria-label={`Delete tag ${tag.name}`} onClick={() => setDeleting(tag)}>
                                        Delete
                                    </button>
                                </div>
                            )}
                        </li>
                    ))}
                </ul>
            )}

            <ConfirmDialog
                show={deleting !== null}
                title={`Delete "${deleting?.name ?? ''}"?`}
                message={deleting ? `This tag will be removed from ${deleting.quizCount} ${deleting.quizCount === 1 ? 'quiz' : 'quizzes'}. This cannot be undone.` : ''}
                confirmText="Delete tag"
                busy={busy}
                onConfirm={handleDelete}
                onCancel={() => setDeleting(null)}
            />
        </div>
    )
}