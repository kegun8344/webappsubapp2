/*
 * Shows an error message from the API.
 * Renders nothing when there is no message (conditional rendering).
 */
interface ErrorAlertProps {
    message: string | null
    onClose?: () => void
}

export default function ErrorAlert({ message, onClose }: ErrorAlertProps) {
    if (!message) {
        return null
    }

    return (
        <div className="alert alert-danger d-flex align-items-center justify-content-between gap-2" role="alert">
            <span>{message}</span>

            {/* The close button is only shown when the page can hide the message */}
            {onClose && (
                <button type="button" className="btn-close" aria-label="Close" onClick={onClose}></button>
            )}
        </div>
    )
}