/* What the API sends and receives for login (same names as the DTOs in api/DTOs) */

export interface User {
    id: string
    email: string
    roles: string[]
}

export interface LoginRequest {
    email: string
    password: string
}

export interface RegisterRequest {
    email: string
    password: string
    confirmPassword: string
}

/* The answer after login or sign up. The token itself is in an HttpOnly cookie that JavaScript cannot read. */
export interface AuthResponse {
    expiresAt: string
    user: User
}