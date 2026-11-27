import { apiRequest } from './apiClient'
import type { Dashboard, Leaderboard } from '../types/stats'

/* "week" counts points since Monday, "all" counts all time */
export function getLeaderboard(period: 'all' | 'week'): Promise<Leaderboard> {
    return apiRequest<Leaderboard>(`/api/leaderboard?period=${period}`)
}

/* The signed-in student's own numbers. Only students have access (403 for others). */
export function getDashboard(): Promise<Dashboard> {
    return apiRequest<Dashboard>('/api/dashboard')
}