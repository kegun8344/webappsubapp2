/* What GET /api/home sends (HomeDtos.cs in the API) */

import type { TeacherOption } from './admin'

export interface HomeQuiz {
    quizId: number
    title: string
    moduleName: string
    difficulty: string
    questionCount: number
    bestScore: number
    pointsLeft: number
}

export interface StudentHome {
    streakDays: number
    streakKeptToday: boolean
    streakPointsToday: number
    dailyGoalStars: number
    dailyGoalStarsNeeded: number
    dailyGoalBonus: number
    dailyGoalReached: boolean
    improveQuizzes: HomeQuiz[]
    quizzesToTry: HomeQuiz[]
}

export interface AttentionQuiz {
    quizId: number
    title: string
    moduleName: string
    problems: string[]
}

export interface Activity {
    displayName: string
    quizId: number
    quizTitle: string
    score: number
    /* e.g. "2 h ago" */
    timeAgo: string
}

export interface QuizStats {
    quizId: number
    title: string
    moduleName: string
    questionCount: number
    attempts: number
    averageScore: number | null
}

export interface PlatformCounts {
    students: number
    teachers: number
    officialQuizzes: number
    practiceQuizzes: number
    attemptsThisWeek: number
}

export interface NewUser {
    id: string
    email: string
    isTeacher: boolean
    timeAgo: string
}

export interface LockedUser {
    id: string
    email: string
    lockedByAdmin: boolean
    lockedUntil: string
}

export interface StaffHome {
    needsAttention: AttentionQuiz[]
    recentActivity: Activity[]
    yourQuizzes: QuizStats[]
    studentsReached: number
    totalAttempts: number
    averageScore: number | null
    /* null for teachers */
    platform: PlatformCounts | null
    popularThisWeek: QuizStats[]
    newUsers: NewUser[]
    lockedUsers: LockedUser[]
    /* Administrators: the teachers in the "Move all to" dropdown */
    teacherOptions: TeacherOption[]
}

export interface Home {
    role: 'Student' | 'Teacher' | 'Admin'
    student: StudentHome | null
    staff: StaffHome | null
}