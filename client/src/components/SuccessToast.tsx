import { useEffect } from 'react'

interface SuccessToastProps {
    message: string | null
    onClose: () => void
}

/* A green message that hides itself after a few seconds, like the toast in Subapp 1. Renders nothing without a message. */
export default function SuccessToast({ message, onClose }: SuccessToastProps) {
    useEffect(() => {
        if (!message) return

        const timer = setTimeout(onClose, 4000)
        return () => clearTimeout(timer)
    }, [message, onClose])

    if (!message) {
        return null
    }

    return (
        <div className="alert alert-success d-flex align-items-center justify-content-between toast-message shadow" role="alert">
            <span>{message}</span>
            <button type="button" className="btn-close" aria-label="Close" onClick={onClose}></button>
        </div>
    )
}