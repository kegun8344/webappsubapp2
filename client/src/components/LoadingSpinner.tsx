/* Shown while data is loading from the API */
interface LoadingSpinnerProps {
    message?: string
}

export default function LoadingSpinner({ message = 'Loading...' }: LoadingSpinnerProps) {
    return (
        <div className="d-flex justify-content-center align-items-center gap-2 py-5" role="status">
            <div className="spinner-border text-primary" aria-hidden="true"></div>
            <span className="text-secondary">{message}</span>
        </div>
    )
}