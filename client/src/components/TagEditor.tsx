import { useState, type FormEvent } from 'react'
import { ApiError } from '../services/apiClient'
import { attachTag, createTag, removeTag } from '../services/quizService'
import { tagColors, type Tag } from '../types/quiz'

interface TagEditorProps {
    quizId: number
    /* The tags on the quiz now */
    tags: Tag[]
    /* The catalog: shared tags and the user's own tags */
    catalog: Tag[]
    /* Teachers and administrators can put new tags in the shared catalog */
    canShare: boolean
    /* Called after a change, so the page can load the quiz again */
    onChanged: (message: string) => Promise<void>
}

/* Tags on a quiz (many-to-many): remove, add from the catalog, or create a new one. Same parts as the Edit quiz page in Subapp 1. */
export default function TagEditor({ quizId, tags, catalog, canShare, onChanged }: TagEditorProps) {
    const [selectedTagId, setSelectedTagId] = useState('')
    const [name, setName] = useState('')
    const [color, setColor] = useState('primary')
    const [shared, setShared] = useState(true)
    const [nameError, setNameError] = useState<string | null>(null)
    const [error, setError] = useState<string | null>(null)
    const [busy, setBusy] = useState(false)

    /* Tags the quiz already has are left out of the dropdown */
    const available = catalog.filter((tag) => !tags.some((current) => current.id === tag.id))

    /* Runs one change, shows the server's message if it fails */
    async function run(action: () => Promise<unknown>, message: string) {
        setBusy(true)
        setError(null)
        try {
            await action()
            await onChanged(message)
            return true
        } catch (err) {
            setError(err instanceof ApiError ? err.fieldErrors.name?.[0] ?? err.message : 'The tag could not be changed. Please try again.')
            return false
        } finally {
            setBusy(false)
        }
    }

    async function handleAttach(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()
        if (!selectedTagId) return

        const tag = available.find((item) => item.id === Number(selectedTagId))
        if (await run(() => attachTag(quizId, Number(selectedTagId)), `Tag "${tag?.name ?? ''}" added to the quiz.`)) {
            setSelectedTagId('')
        }
    }

    async function handleCreate(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()

        const trimmed = name.trim()
        if (!trimmed || trimmed.length > 30) {
            setNameError('A tag name must be between 1 and 30 characters.')
            return
        }

        if (await run(() => createTag(quizId, { name: trimmed, color, shared: canShare && shared }), `Tag "${trimmed}" added to the quiz.`)) {
            setName('')
        }
    }

    return (
        <>
            <h3 className="h6 fw-bold mb-2">Tags</h3>

            {error && <div className="alert alert-danger py-2" role="alert">{error}</div>}

            {/* The × removes the tag from this quiz only */}
            {tags.length > 0 ? (
                <div className="d-flex flex-wrap gap-2 mb-3">
                    {tags.map((tag) => (
                        <span key={tag.id} className={`badge text-bg-${tag.color} d-inline-flex align-items-center gap-1`}>
                            {tag.name}
                            <button
                                type="button"
                                className="btn btn-link p-0 lh-1 text-reset text-decoration-none"
                                aria-label={`Remove tag ${tag.name}`}
                                disabled={busy}
                                onClick={() => run(() => removeTag(quizId, tag.id), `Tag "${tag.name}" removed from the quiz.`)}
                            >
                                &times;
                            </button>
                        </span>
                    ))}
                </div>
            ) : (
                <p className="text-secondary small">This quiz has no tags yet.</p>
            )}

            {available.length > 0 && (
                <form className="mb-4" onSubmit={handleAttach}>
                    <label htmlFor="existing-tag" className="form-label small">Add a tag from the catalog</label>
                    <div className="d-flex gap-2">
                        <select id="existing-tag" className="form-select" value={selectedTagId} onChange={(event) => setSelectedTagId(event.target.value)}>
                            <option value="" disabled hidden>Choose a tag</option>
                            <optgroup label="Shared tags">
                                {available.filter((tag) => tag.isShared).map((tag) => (
                                    <option key={tag.id} value={tag.id}>{tag.name}</option>
                                ))}
                            </optgroup>
                            {available.some((tag) => !tag.isShared) && (
                                <optgroup label="My tags">
                                    {available.filter((tag) => !tag.isShared).map((tag) => (
                                        <option key={tag.id} value={tag.id}>{tag.name}</option>
                                    ))}
                                </optgroup>
                            )}
                        </select>
                        <button type="submit" className="btn btn-outline-primary" disabled={busy || !selectedTagId}>Add</button>
                    </div>
                </form>
            )}

            {/* Everyone can create their own tags. Teachers and administrators can also share them. */}
            <h3 className="h6 fw-bold mb-2">Create a new tag</h3>
            <form onSubmit={handleCreate} noValidate>
                <div className="mb-2">
                    <label htmlFor="new-tag-name" className="form-label small">New tag</label>
                    <input
                        id="new-tag-name"
                        className={`form-control${nameError ? ' is-invalid' : ''}`}
                        maxLength={30}
                        placeholder="e.g. Exam relevant"
                        value={name}
                        aria-invalid={Boolean(nameError)}
                        aria-describedby={nameError ? 'new-tag-name-error' : undefined}
                        onChange={(event) => {
                            setName(event.target.value)
                            setNameError(null)
                        }}
                    />
                    {nameError && <div id="new-tag-name-error" className="invalid-feedback">{nameError}</div>}
                </div>

                {/* Coloured buttons, so the colour can be seen before choosing it */}
                <fieldset className="mb-3">
                    <legend className="form-label small mb-1">Color</legend>
                    <div className="d-flex flex-wrap gap-2">
                        {tagColors.map((option) => (
                            <span key={option.value}>
                                <input
                                    type="radio"
                                    className="btn-check"
                                    name="new-tag-color"
                                    id={`tag-color-${option.value}`}
                                    value={option.value}
                                    checked={color === option.value}
                                    onChange={() => setColor(option.value)}
                                />
                                <label className={`btn btn-sm btn-outline-${option.value}`} htmlFor={`tag-color-${option.value}`}>
                                    {option.label}
                                </label>
                            </span>
                        ))}
                    </div>
                </fieldset>

                {canShare ? (
                    <div className="form-check mb-3">
                        <input className="form-check-input" type="checkbox" id="new-tag-shared" checked={shared} onChange={(event) => setShared(event.target.checked)} />
                        <label className="form-check-label small" htmlFor="new-tag-shared">
                            Add to the shared catalog (every user can use this tag)
                        </label>
                    </div>
                ) : (
                    <p className="text-secondary small">Only you can see and use tags you create.</p>
                )}

                <button type="submit" className="btn btn-outline-primary" disabled={busy}>Create tag</button>
            </form>
        </>
    )
}