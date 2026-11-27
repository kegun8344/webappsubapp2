export type ColorVisionMode = 'None' | 'RedGreen' | 'BlueYellow'

export interface Settings {
    darkModeEnabled: boolean
    reduceMotion: boolean
    colorVisionMode: ColorVisionMode
}

/* Used for visitors and until the user's settings have loaded */
export const defaultSettings: Settings = { darkModeEnabled: false, reduceMotion: false, colorVisionMode: 'None' }

export interface ChangePasswordRequest {
    currentPassword: string
    newPassword: string
    confirmPassword: string
}

export interface DeleteAccountInfo {
    blockReason: string | null
    privateQuizzes: number
    publicQuizzes: number
    attempts: number
    points: number
}