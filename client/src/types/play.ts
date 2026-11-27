/* What the play endpoints send and receive (PlayDtos.cs in the API) */

export interface PlayOption {
    id: number
    text: string
}

/* A question while playing. There is no isCorrect: the answers are graded on the server. */
export interface PlayQuestion {
    questionId: number
    text: string
    /* Optional scenario or explanation shown above the question */
    context: string | null
    options: PlayOption[]
}

export interface QuizPlay {
    quizId: number
    title: string
    questions: PlayQuestion[]
}

export interface SubmittedAnswer {
    questionId: number
    /* null if the question was skipped */
    selectedAnswerId: number | null
}

/* Why points were given, the same names as PointReason in the API */
export type AwardReason = 'QuizScore' | 'FirstCompletion' | 'DailyGoal' | 'Streak'

export interface Award {
    reason: AwardReason
    points: number
}

export type BadgeTier = 'None' | 'Bronze' | 'Silver' | 'Gold' | 'Diamond'

/* A badge unlocked or upgraded by the attempt */
export interface NewBadge {
    type: string
    name: string
    description: string
    tier: BadgeTier
}

/* The result after submitting (QuizResultDto) */
export interface QuizResult {
    quizId: number
    quizTitle: string
    correctAnswers: number
    totalQuestions: number
    scorePercentage: number
    awardsPoints: boolean
    resultSaved: boolean
    awards: Award[]
    totalPoints: number
    previousBest: number | null
    bestScore: number
    streakDay: number
    dailyGoalStarsEarned: number
    dailyGoalStarsToday: number
    level: number
    pointsIntoLevel: number
    pointsForLevel: number
    leveledUp: boolean
    newBadges: NewBadge[]
}