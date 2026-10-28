export interface ApiEnvelope<T> {
  success: boolean;
  message: string;
  data: T | null;
  errors: string[];
  statusCode: number;
}

export interface AuthResponse {
  userId: string;
  email: string;
  name: string;
  role: "User" | "Admin";
  token: string;
  expiresAt: string;
}

export interface DashboardStats {
  totalWords: number;
  totalVideos: number;
  totalQuizQuestions: number;
  totalUsers: number;
  newUsersToday: number;
  activeUsersLast7Days: number;
  activeUsersLast30Days: number;
  quizAnswersLast7Days: number;
  wordsAddedLast24Hours: number;
  videosAddedLast24Hours: number;
  sessionsOpenedLast24Hours: number;
  usersRegisteredLast24Hours: number;
  pendingWordCount: number;
  dailyUserActivity: { date: string; activeUsers: number; sessions: number }[];
  pendingVideoCandidates: number;
  todayCorrectAnswers: number;
  todayWrongAnswers: number;
  todayTotalAnswers: number;
  totalXpEarned: number;
  topSearchedWords: { text: string; saveCount: number }[];
  hardestQuestions: { quizQuestionId: string; questionText: string; totalAnswers: number; wrongAnswers: number; wrongRate: number }[];
}

export interface PendingContent {
  words: { id: string; text: string; definition: string; createdAt: string }[];
  videoCandidates: VideoCandidate[];
}

export interface VideoCandidate {
  id: string;
  wordId: string;
  wordText: string;
  youtubeId: string;
  title: string;
  channelTitle: string | null;
  thumbnailUrl: string | null;
  description: string | null;
  durationSeconds: number | null;
  isApproved?: boolean;
  isRejected?: boolean;
  fetchedAt: string;
}

export interface WordDetail {
  id: string;
  text: string;
  definition: string;
  ipa: string | null;
  partOfSpeech: string | null;
  cefrLevel: string | null;
  exampleSentences: string[];
  synonyms: string[];
  antonyms: string[];
  category: string | null;
  videos: { id: string; youtubeId: string; title: string; channelTitle: string | null; thumbnailUrl: string | null; startSec: number; endSec: number; displayOrder: number }[];
}

export interface VideoCandidateReview {
  candidate: VideoCandidate;
  word: WordDetail;
}

export interface WordSummary {
  id: string;
  text: string;
  definition: string;
  partOfSpeech: string | null;
  cefrLevel: string | null;
  category: string | null;
  isPublished: boolean;
  isApproved: boolean;
  createdAt: string;
}

export interface UserProfile {
  id: string;
  name: string;
  email: string;
  memberSince: string;
  totalXp: number;
  savedWordCount: number;
  masteredWordCount: number;
  pendingReviewCount: number;
  totalQuizAnswers: number;
  correctAnswers: number;
  quizAccuracy: number;
}

export interface SavedWord {
  id: string;
  wordId: string;
  text: string;
  definition: string;
  partOfSpeech: string | null;
  cefrLevel: string | null;
  knownLevel: number;
  nextReviewAt: string | null;
  reviewCount: number;
  personalNote: string | null;
  savedAt: string;
}

export interface AdminUser {
  id: string;
  name: string;
  email: string;
  role: "User" | "Admin";
  totalXp: number;
  memberSince: string;
  lastActiveAt: string | null;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export const demoStats: DashboardStats = {
  totalWords: 248,
  totalVideos: 186,
  totalQuizQuestions: 124,
  totalUsers: 392,
  newUsersToday: 18,
  activeUsersLast7Days: 143,
  activeUsersLast30Days: 0,
  quizAnswersLast7Days: 1284,
  wordsAddedLast24Hours: 0,
  videosAddedLast24Hours: 0,
  sessionsOpenedLast24Hours: 0,
  usersRegisteredLast24Hours: 0,
  pendingWordCount: 0,
  dailyUserActivity: [],
  pendingVideoCandidates: 12,
  todayCorrectAnswers: 84,
  todayWrongAnswers: 29,
  todayTotalAnswers: 113,
  totalXpEarned: 28450,
  topSearchedWords: [
    { text: "serendipity", saveCount: 82 },
    { text: "resilient", saveCount: 64 },
    { text: "eloquent", saveCount: 51 },
    { text: "wanderlust", saveCount: 43 },
  ],
  hardestQuestions: [
    { quizQuestionId: "q1", questionText: "Which word means a sudden fortunate discovery?", totalAnswers: 42, wrongAnswers: 27, wrongRate: 64.3 },
    { quizQuestionId: "q2", questionText: "Choose the closest meaning of resilient.", totalAnswers: 35, wrongAnswers: 18, wrongRate: 51.4 },
  ],
};

export const demoWords: WordSummary[] = [
  { id: "w1", text: "serendipity", definition: "The occurrence of events by chance in a happy or beneficial way.", partOfSpeech: "noun", cefrLevel: "B2", category: "Discovery", isPublished: true, isApproved: true, createdAt: "2026-10-26T09:15:00Z" },
  { id: "w2", text: "resilient", definition: "Able to recover quickly from difficulties.", partOfSpeech: "adjective", cefrLevel: "B1", category: "Character", isPublished: true, isApproved: true, createdAt: "2026-10-25T12:30:00Z" },
  { id: "w3", text: "eloquent", definition: "Fluent or persuasive in speaking or writing.", partOfSpeech: "adjective", cefrLevel: "C1", category: "Communication", isPublished: false, isApproved: false, createdAt: "2026-10-23T16:45:00Z" },
  { id: "w4", text: "wanderlust", definition: "A strong desire to travel and explore the world.", partOfSpeech: "noun", cefrLevel: "B2", category: "Travel", isPublished: true, isApproved: true, createdAt: "2026-10-21T11:00:00Z" },
  { id: "w5", text: "ephemeral", definition: "Lasting for a very short time.", partOfSpeech: "adjective", cefrLevel: "C1", category: "Time", isPublished: false, isApproved: false, createdAt: "2026-10-20T08:20:00Z" },
];
