import { apiRequest } from './apiClient'
import type { Ping } from '../types/ping'

/* Checks that the API is running */
export function getPing(): Promise<Ping> {
    return apiRequest<Ping>('/api/ping')
}