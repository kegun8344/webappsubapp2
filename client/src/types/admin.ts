/* What the administrator endpoints send (AdminUserDtos.cs and MoveQuizDtos.cs in the API) */

export interface AdminUser {
    id: string
    email: string
    roles: string[]
    quizzesCreated: number
    quizzesPublished: number
    quizzesCompleted: number
    totalPoints: number
    level: number
    isLocked: boolean
    /* The server has checked UserDeletionRules */
    canDelete: boolean
}

export interface UserDeletionPreview {
    id: string
    email: string
    roles: string[]
    privateQuizzes: number
    publicQuizzes: number
    attempts: number
    points: number
}

export interface TeacherOption {
    id: string
    email: string
}

export interface MoveQuizInfo {
    quizId: number
    title: string
    /* null when the quiz has no teacher */
    currentTeacherEmail: string | null
    teachers: TeacherOption[]
}

export interface MoveAllResult {
    moved: number
    teacherEmail: string
}

/* The role filter on the Users page. null means all users. */
export type UserRoleFilter = 'Student' | 'Teacher' | null