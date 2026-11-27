import { apiRequest } from './apiClient'
import type { AdminUser, MoveAllResult, MoveQuizInfo, UserDeletionPreview, UserRoleFilter } from '../types/admin'

/* Users page: everyone except administrators, filtered by role and email */
export function getUsers(role: UserRoleFilter, search: string): Promise<AdminUser[]> {
    const params = new URLSearchParams()
    if (role) params.set('role', role)
    if (search) params.set('search', search)

    const query = params.toString()
    return apiRequest<AdminUser[]>(`/api/admin/users${query ? `?${query}` : ''}`)
}

/* What is lost if the user is deleted. 400 with the reason if the user cannot be deleted. */
export function getDeletionPreview(userId: string): Promise<UserDeletionPreview> {
    return apiRequest<UserDeletionPreview>(`/api/admin/users/${encodeURIComponent(userId)}/deletion`)
}

export function deleteUser(userId: string): Promise<void> {
    return apiRequest<void>(`/api/admin/users/${encodeURIComponent(userId)}`, { method: 'DELETE' })
}

/* The four actions on a row. The user is logged out by the server after a role change or a lock. */
type UserAction = 'make-teacher' | 'remove-teacher' | 'lock' | 'unlock'

export function changeUser(userId: string, action: UserAction): Promise<void> {
    return apiRequest<void>(`/api/admin/users/${encodeURIComponent(userId)}/${action}`, { method: 'POST' })
}

/* Change teacher page for one quiz */
export function getMoveInfo(quizId: number): Promise<MoveQuizInfo> {
    return apiRequest<MoveQuizInfo>(`/api/quizzes/${quizId}/teacher`)
}

export function moveQuiz(quizId: number, teacherId: string): Promise<void> {
    return apiRequest<void>(`/api/quizzes/${quizId}/teacher`, {
        method: 'PUT',
        body: JSON.stringify({ teacherId }),
    })
}

/* "Move all" under Needs attention on the admin Home page */
export function moveAllWithoutTeacher(teacherId: string): Promise<MoveAllResult> {
    return apiRequest<MoveAllResult>('/api/quizzes/without-teacher/move', {
        method: 'POST',
        body: JSON.stringify({ teacherId }),
    })
}