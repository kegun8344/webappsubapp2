/* What GET /api/quizzes/{id}/analysis sends (QuizAnalysisDtos.cs in the API) */

export interface QuestionAnalysis {
    number: number
    text: string
    answers: number
    correct: number
    /* null when no student has answered */
    percentCorrect: number | null
    flag: 'Hard' | 'Too easy' | null
    mostChosenWrongAnswer: string | null
    mostChosenWrongCount: number
}

export interface QuizAnalysis {
    quizId: number
    title: string
    minAnswersForFlag: number
    hardBelowPercent: number
    easyAbovePercent: number
    questions: QuestionAnalysis[]
}