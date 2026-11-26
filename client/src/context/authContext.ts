import { createContext } from 'react'
import type { LoginRequest, RegisterRequest, User } from '../types/auth'

/* What every page can get from useAuth() */
export interface AuthContextValue {
    /* The signed-in user, or null when nobody is signed in */
    user: User | null

    /* True until the app knows whether someone is signed in, so pages do not show the wrong content first */
    isLoading: boolean

    login: (data: LoginRequest) => Promise<void>
    register: (data: RegisterRequest) => Promise<void>
    logout: () => Promise<void>

    /* E.g. hasRole('Teacher'). Only for showing or hiding things: the API checks the role again. */
    hasRole: (role: string) => boolean
}

export const AuthContext = createContext<AuthContextValue | null>(null)