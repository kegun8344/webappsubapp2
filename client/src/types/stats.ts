/* What the leaderboard and dashboard endpoints send (LeaderboardDtos.cs and DashboardDtos.cs in the API) */
import type { BadgeTier } from './play'

/* A small badge on the leaderboard */
export interface MiniBadge {
    type: string
    name: string
    description: string
    tier: BadgeTier
}

export interface LeaderboardEntry {
    /* Students with the same points share a rank (1, 1, 3) */
    rank: number
    displayName: string
    initial: string
    /* 0-7, used for the avatar colour class lb-avatar-N */
    avatarColor: number
    points: number
    /* Length of the points bar compared to first place, 0-100 */
    barPercent: number
    level: number
    currentStreak: number
    badgeCount: number
    topBadges: MiniBadge[]
    isCurrentUser: boolean
}

export interface LeaderboardStanding {
    rank: number
    studentCount: number
    points: number
    level: number
    levelProgressPercent: number
    pointsToNextLevel: number
    /* The student right above, null if the user is in the lead */
    nextPlaceName: string | null
    pointsBehindNextPlace: number
}

export interface LeaderboardHighlight {
    title: string
    /* A Bootstrap Icons class, e.g. "bi-fire" */
    icon: string
    name: string
    value: number
    unit: string
}

export interface Leaderboard {
    isWeekly: boolean
    /* The Monday the weekly list starts on, e.g. "2026-11-23" */
    weekStart: string
    entries: LeaderboardEntry[]
    /* null for teachers and administrators */
    standing: LeaderboardStanding | null
    highlights: LeaderboardHighlight[]
}

export interface RecentAttempt {
    quizId: number
    quizTitle: string
    score: number
    /* UTC time */
    completedAt: string
    awardsPoints: boolean
}

/* One badge with its progress (BadgeProgressDto) */
export interface BadgeProgress {
    type: string
    name: string
    description: string
    unit: string
    tier: BadgeTier
    value: number
    /* null at the highest tier */
    nextThreshold: number | null
    nextTier: BadgeTier | null
    progressPercent: number
    isLocked: boolean
}

export interface Dashboard {
    displayName: string
    totalPoints: number
    level: number
    pointsIntoLevel: number
    pointsForLevel: number
    streakDays: number
    quizzesCompleted: number
    /* null if no quizzes completed */
    averageScore: number | null
    leaderboardRank: number
    studentCount: number
    recentAttempts: RecentAttempt[]
    badges: BadgeProgress[]
}