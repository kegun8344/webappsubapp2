import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import * as authService from '../services/authService'
import { AuthContext, type AuthContextValue } from './authContext'
import type { LoginRequest, RegisterRequest, User } from '../types/auth'

/*
 * Keeps track of who is signed in, for the whole app.
 * The token is in an HttpOnly cookie that JavaScript cannot read, so only the user info is kept here.
 */
export default function AuthProvider({ children }: { children: ReactNode }) {
    const [user, setUser] = useState<User | null>(null)
    const [isLoading, setIsLoading] = useState(true)

    /* When the app starts (or the page is refreshed): ask the API who is signed in. The cookie is sent automatically. */
    useEffect(() => {
        let active = true

        authService
            .getCurrentUser()
            .then((current) => {
                if (active) setUser(current)
            })
            .catch(() => {
                /* 401 means nobody is signed in, which is normal */
                if (active) setUser(null)
            })
            .finally(() => {
                if (active) setIsLoading(false)
            })

        return () => {
            active = false
        }
    }, [])

    const login = useCallback(async (data: LoginRequest) => {
        const response = await authService.login(data)
        setUser(response.user)
    }, [])

    const register = useCallback(async (data: RegisterRequest) => {
        const response = await authService.register(data)
        setUser(response.user)
    }, [])

    const logout = useCallback(async () => {
        try {
            await authService.logout()
        } finally {
            /* Signed out in the app even if the API could not be reached */
            setUser(null)
        }
    }, [])

    const hasRole = useCallback((role: string) => user?.roles.includes(role) ?? false, [user])

    /* The same object until something changes, so pages do not render again for nothing */
    const value = useMemo<AuthContextValue>(
        () => ({ user, isLoading, login, register, logout, hasRole }),
        [user, isLoading, login, register, logout, hasRole],
    )

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}