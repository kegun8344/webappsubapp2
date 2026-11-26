import { apiRequest } from './apiClient'
import type { AuthResponse, LoginRequest, RegisterRequest, User } from '../types/auth'

/* Logs in. The API answers with the user and puts the token in an HttpOnly cookie. */
export function login(data: LoginRequest): Promise<AuthResponse> {
    return apiRequest<AuthResponse>('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify(data),
    })
}

/* Signs up as a student, and logs in straight away (same as Subapp 1) */
export function register(data: RegisterRequest): Promise<AuthResponse> {
    return apiRequest<AuthResponse>('/api/auth/register', {
        method: 'POST',
        body: JSON.stringify(data),
    })
}

/* Logs out. JavaScript cannot delete an HttpOnly cookie, so the API does it. */
export function logout(): Promise<void> {
    return apiRequest<void>('/api/auth/logout', { method: 'POST' })
}

/* Who is signed in, based on the cookie. Fails with 401 when nobody is. */
export function getCurrentUser(): Promise<User> {
    return apiRequest<User>('/api/auth/me')
}