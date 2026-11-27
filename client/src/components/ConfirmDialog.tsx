import { useEffect, useRef, type ReactNode } from 'react'

/*
 * "Are you sure?" popup with a red confirm button, like the delete popups in Subapp 1.
 * Controlled by the page through "show", so it needs no Bootstrap JavaScript.
 */
interface ConfirmDialogProps {
    show: boolean
    title: string
    message: ReactNode
    confirmText?: string
    /* The colour of the confirm button, red by default. E.g. 'btn-primary' for "Make teacher". */
    confirmClass?: string
    busy?: boolean
    onConfirm: () => void
    onCancel: () => void
}

export default function ConfirmDialog({
    show,
    title,
    message,
    confirmText = 'Delete',
    confirmClass = 'btn-danger',
    busy = false,
    onConfirm,
    onCancel,
}: ConfirmDialogProps) {
    const cancelButton = useRef<HTMLButtonElement>(null)

    /* Start on Cancel, so pressing Enter by accident does not delete anything */
    useEffect(() => {
        if (show) {
            cancelButton.current?.focus()
        }
    }, [show])

    /* Escape closes the popup, like a normal Bootstrap modal */
    useEffect(() => {
        if (!show) {
            return
        }

        const onKeyDown = (event: KeyboardEvent) => {
            if (event.key === 'Escape' && !busy) {
                onCancel()
            }
        }

        document.addEventListener('keydown', onKeyDown)
        return () => document.removeEventListener('keydown', onKeyDown)
    }, [show, busy, onCancel])

    if (!show) {
        return null
    }

    return (
        <>
            <div className="modal d-block" tabIndex={-1} role="dialog" aria-modal="true" aria-labelledby="confirm-dialog-title">
                <div className="modal-dialog modal-dialog-centered">
                    <div className="modal-content">
                        <div className="modal-header">
                            <h2 className="modal-title fs-5 fw-bold" id="confirm-dialog-title">{title}</h2>
                            <button type="button" className="btn-close" aria-label="Close" onClick={onCancel} disabled={busy}></button>
                        </div>

                        <div className="modal-body">{message}</div>

                        <div className="modal-footer">
                            <button ref={cancelButton} type="button" className="btn btn-outline-secondary" onClick={onCancel} disabled={busy}>
                                Cancel
                            </button>
                            <button type="button" className={`btn ${confirmClass}`} onClick={onConfirm} disabled={busy}>
                                {busy ? 'Working...' : confirmText}
                            </button>
                        </div>
                    </div>
                </div>
            </div>

            {/* Dark background behind the popup */}
            <div className="modal-backdrop show"></div>
        </>
    )
}