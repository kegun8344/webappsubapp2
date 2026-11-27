import { apiRequest } from './apiClient'
import type { QuizAnalysis } from '../types/analysis'

/* How students answered each question. Only the quiz owner or an administrator gets an answer (403 otherwise). */
export function getQuizAnalysis(quizId: number): Promise<QuizAnalysis> {
    return apiRequest<QuizAnalysis>(`/api/quizzes/${quizId}/analysis`)
}