import { apiRequest } from './apiClient'
import type { Question, QuestionFormRequest } from '../types/quiz'

export function createQuestion(quizId: number, data: QuestionFormRequest): Promise<Question> {
    return apiRequest<Question>(`/api/quizzes/${quizId}/questions`, {
        method: 'POST',
        body: JSON.stringify(data),
    })
}

/* Changes the text and replaces all the answer options */
export function updateQuestion(id: number, data: QuestionFormRequest): Promise<Question> {
    return apiRequest<Question>(`/api/questions/${id}`, {
        method: 'PUT',
        body: JSON.stringify(data),
    })
}

export function deleteQuestion(id: number): Promise<void> {
    return apiRequest<void>(`/api/questions/${id}`, { method: 'DELETE' })
}