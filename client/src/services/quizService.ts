import { apiRequest } from './apiClient'
import type { CreateTagRequest, ModuleOption, QuizDetail, QuizFilters, QuizFormRequest, QuizListItem, Tag } from '../types/quiz'

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

/* One quiz with its questions. The correct answers are only included for people who may edit it. */
export function getQuiz(id: number): Promise<QuizDetail> {
    return apiRequest<QuizDetail>(`/api/quizzes/${id}`)
}

export function createQuiz(data: QuizFormRequest): Promise<QuizDetail> {
    return apiRequest<QuizDetail>('/api/quizzes', {
        method: 'POST',
        body: JSON.stringify(data),
    })
}

export function updateQuiz(id: number, data: QuizFormRequest): Promise<void> {
    return apiRequest<void>(`/api/quizzes/${id}`, {
        method: 'PUT',
        body: JSON.stringify(data),
    })
}

export function deleteQuiz(id: number): Promise<void> {
    return apiRequest<void>(`/api/quizzes/${id}`, { method: 'DELETE' })
}

/* Shared tags and the user's own tags */
export function getTags(): Promise<Tag[]> {
    return apiRequest<Tag[]>('/api/tags')
}

/* Creates a tag (or reuses one with the same name) and puts it on the quiz */
export function createTag(quizId: number, data: CreateTagRequest): Promise<Tag> {
    return apiRequest<Tag>(`/api/quizzes/${quizId}/tags`, {
        method: 'POST',
        body: JSON.stringify(data),
    })
}

/* Puts an existing tag on the quiz */
export function attachTag(quizId: number, tagId: number): Promise<Tag> {
    return apiRequest<Tag>(`/api/quizzes/${quizId}/tags/${tagId}`, { method: 'POST' })
}

/* Takes the tag off the quiz. The tag itself is kept for other quizzes. */
export function removeTag(quizId: number, tagId: number): Promise<void> {
    return apiRequest<void>(`/api/quizzes/${quizId}/tags/${tagId}`, { method: 'DELETE' })
}

/* The modules for the dropdowns. GET /api/modules sends more fields, only id and name are used here. */
export function getModuleOptions(): Promise<ModuleOption[]> {
    return apiRequest<ModuleOption[]>('/api/modules')
}