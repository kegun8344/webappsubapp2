import { apiRequest } from './apiClient'
import type { Module, ModuleFormRequest, ModuleOptions, ModuleSummary } from '../types/module'

/* All modules with the signed-in user's progress */
export function getModules(): Promise<Module[]> {
    return apiRequest<Module[]>('/api/modules')
}

export function getModule(id: number): Promise<Module> {
    return apiRequest<Module>(`/api/modules/${id}`)
}

/* The student's progress over all modules */
export function getModuleSummary(): Promise<ModuleSummary> {
    return apiRequest<ModuleSummary>('/api/modules/summary')
}

/* The icons and colours the server accepts */
export function getModuleOptions(): Promise<ModuleOptions> {
    return apiRequest<ModuleOptions>('/api/modules/options')
}

export function createModule(data: ModuleFormRequest): Promise<Module> {
    return apiRequest<Module>('/api/modules', {
        method: 'POST',
        body: JSON.stringify(data),
    })
}

export function updateModule(id: number, data: ModuleFormRequest): Promise<void> {
    return apiRequest<void>(`/api/modules/${id}`, {
        method: 'PUT',
        body: JSON.stringify(data),
    })
}

/* Fails with 409 if the module still contains quizzes */
export function deleteModule(id: number): Promise<void> {
    return apiRequest<void>(`/api/modules/${id}`, { method: 'DELETE' })
}