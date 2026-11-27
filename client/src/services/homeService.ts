import { apiRequest } from './apiClient'
import type { Home } from '../types/home'

/* The signed-in Home page for the user's role */
export function getHome(): Promise<Home> {
    return apiRequest<Home>('/api/home')
}