import { apiRequest } from './apiClient'
import type { QuizPlay, QuizResult, SubmittedAnswer } from '../types/play'

/* The questions of a quiz, without the correct answers */
export function getQuizForPlay(quizId: number): Promise<QuizPlay> {
    return apiRequest<QuizPlay>(`/api/quizzes/${quizId}/play`)
}

/* Sends the answers. The server grades them and gives points. */
export function submitAttempt(quizId: number, answers: SubmittedAnswer[]): Promise<QuizResult> {
    return apiRequest<QuizResult>(`/api/quizzes/${quizId}/attempts`, {
        method: 'POST',
        body: JSON.stringify({ answers }),
    })
}