import { apiRequest } from './apiClient'
import type { QuizPlay, SubmittedAnswer } from '../types/play'
import type { ClearMistakesResult, ReviewResult } from '../types/review'

/* The user's open mistakes, from one quiz or all quizzes. No questions means nothing to review. */
export function getReview(quizId: number | null): Promise<QuizPlay> {
    return apiRequest<QuizPlay>(quizId ? `/api/review?quizId=${quizId}` : '/api/review')
}

/* Sends the review answers. The server grades them, and right answers leave the review list. No points. */
export function submitReview(quizId: number | null, answers: SubmittedAnswer[]): Promise<ReviewResult> {
    return apiRequest<ReviewResult>('/api/review', {
        method: 'POST',
        body: JSON.stringify({ quizId, answers }),
    })
}

/* Clears the open mistakes in one quiz. They come back if the user answers wrong again. */
export function clearMistakes(quizId: number): Promise<ClearMistakesResult> {
    return apiRequest<ClearMistakesResult>(`/api/review/${quizId}/clear`, { method: 'POST' })
}