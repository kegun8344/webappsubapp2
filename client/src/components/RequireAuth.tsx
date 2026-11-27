import type { ReactNode } from 'react'
import { Link, Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import LoadingSpinner from './LoadingSpinner'

interface RequireAuthProps {
    children: ReactNode
    /* If set, the user needs at least one of these roles, e.g. ['Student'] */
    roles?: string[]
}

/*
 * Protects a page in the React app.
 * Not signed in: sends the user to the login page, which sends them back here afterwards.
 * Wrong role: shows a message instead of the page.
 * This is only for the user experience. The API checks the login and role on every request.
 */
export default function RequireAuth({ children, roles }: RequireAuthProps) {
    const { user, isLoading, hasRole } = useAuth()
    const location = useLocation()

    /* Wait until the app knows who is signed in, so the login page does not flash */
    if (isLoading) {
        return <LoadingSpinner />
    }

    if (!user) {
        return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />
    }

    if (roles && !roles.some((role) => hasRole(role))) {
        return (
            <div className="mx-auto text-center py-5" style={{ maxWidth: 520 }}>
                {/* Icon instead of a status code, same layout as the 404 page */}
                <div className="display-1 fw-bold text-primary">
                    <i className="bi bi-lock" aria-hidden="true"></i>
                </div>
                <h1 className="h3 fw-bold mb-2">Access denied</h1>
                <p className="text-secondary mb-4">Your account does not have permission to open this page.</p>
                <div className="d-grid gap-2">
                    <Link className="btn btn-primary btn-hero" to="/">Back to home</Link>
                    <Link className="btn btn-outline-secondary" to="/quizzes">Browse quizzes</Link>
                </div>
            </div>
        )
    }

    return <>{children}</>
}