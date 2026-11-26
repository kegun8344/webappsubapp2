import { useContext } from 'react'
import { AuthContext } from '../context/authContext'

/* The signed-in user and the login functions, from anywhere in the app */
export function useAuth() {
    const context = useContext(AuthContext)

    if (!context) {
        throw new Error('useAuth must be used inside <AuthProvider>.')
    }

    return context
}