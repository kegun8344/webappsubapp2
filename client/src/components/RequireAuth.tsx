import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
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
                <h1 className="h4 fw-bold mb-2">Access denied</h1>
                <p className="text-secondary mb-0">You do not have access to this page.</p>
            </div>
        )
    }

    return <>{children}</>
}