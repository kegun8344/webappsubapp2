/* What the review endpoints send (ReviewDtos.cs in the API). The questions use the same QuizPlay type as a normal quiz. */

export interface ReviewResult {
    /* null for mistakes from all quizzes */
    quizId: number | null
    correctAnswers: number
    totalQuestions: number
    remaining: number
    resultSaved: boolean
}

export interface ClearMistakesResult {
    cleared: number
}