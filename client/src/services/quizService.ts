import { apiRequest } from './apiClient'
import type { ModuleOption, QuizFilters, QuizListItem, Tag } from '../types/quiz'

/* Builds "?view=all&search=ef" from the filters, leaving out the empty ones */
function toQueryString(filters: QuizFilters): string {
    const params = new URLSearchParams({ view: filters.view })

    if (filters.moduleId) params.set('moduleId', String(filters.moduleId))
    if (filters.search) params.set('search', filters.search)
    if (filters.difficulty) params.set('difficulty', filters.difficulty)
    if (filters.tagId) params.set('tagId', String(filters.tagId))

    return `?${params.toString()}`
}

/* The quizzes the signed-in user may see, filtered on the server */
export function getQuizzes(filters: QuizFilters): Promise<QuizListItem[]> {
    return apiRequest<QuizListItem[]>(`/api/quizzes${toQueryString(filters)}`)
}

export function deleteQuiz(id: number): Promise<void> {
    return apiRequest<void>(`/api/quizzes/${id}`, { method: 'DELETE' })
}

/* Shared tags and the user's own tags, for the tag filter */
export function getTags(): Promise<Tag[]> {
    return apiRequest<Tag[]>('/api/tags')
}

/* The modules for the module filter. GET /api/modules sends more fields, only id and name are used here. */
export function getModuleOptions(): Promise<ModuleOption[]> {
    return apiRequest<ModuleOption[]>('/api/modules')
}