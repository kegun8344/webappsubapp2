import { apiRequest } from './apiClient'
import type { ChangePasswordRequest, DeleteAccountInfo, Settings } from '../types/settings'

export function getSettings(): Promise<Settings> {
    return apiRequest<Settings>('/api/settings')
}

export function updateSettings(settings: Settings): Promise<Settings> {
    return apiRequest<Settings>('/api/settings', { method: 'PUT', body: JSON.stringify(settings) })
}

export function changePassword(data: ChangePasswordRequest): Promise<void> {
    return apiRequest<void>('/api/settings/change-password', { method: 'POST', body: JSON.stringify(data) })
}

export function getDeleteInfo(): Promise<DeleteAccountInfo> {
    return apiRequest<DeleteAccountInfo>('/api/settings/delete-info')
}

/* The API deletes the login cookie too */
export function deleteAccount(password: string): Promise<void> {
    return apiRequest<void>('/api/settings/delete-account', { method: 'POST', body: JSON.stringify({ password }) })
}