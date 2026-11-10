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
  /** Ses dosyası olan aksanlar: "us" (Amerikan), "gb" (İngiliz). */
  audioAccents?: string[];
  /** Giriş yapan üye bu kelimeyi kelime defterine kaydetmiş mi. */
  isSaved?: boolean;
  partOfSpeech: string | null;
  cefrLevel: string | null;
  exampleSentences: string[];
  synonyms: string[];
  antonyms: string[];
  category: string | null;
  videos: { id: string; youtubeId: string; title: string; channelTitle: string | null; thumbnailUrl: string | null; startSec: number; endSec: number; displayOrder: number }[];
}

export interface VideoClip {
  id: string;
  videoCandidateId: string | null;
  sourceTitle: string | null;
  youtubeId: string;
  startSec: number;
  endSec: number;
  text: string;
  words: { text: string; start: number; end: number }[];
  isAutoCaption: boolean;
  isPublished: boolean;
  isRejected: boolean;
  hasBurnedInCaptions: boolean;
  createdAt: string;
  qcStatus: "Passed" | "Failed" | "Pending";
  qcReasons: string[];
  qcWarnings: string[];
}

export interface LearnerClip {
  id: string;
  youtubeId: string;
  startSec: number;
  endSec: number;
  text: string;
  words: { text: string; start: number; end: number }[];
  hasBurnedInCaptions: boolean;
}

export type ClipReportReason = "Quality" | "CaptionSync" | "Inappropriate" | "Other";

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
  exampleSentence?: string | null;
  cefrLevel: string | null;
  knownLevel: number;
  nextReviewAt: string | null;
  reviewCount: number;
  personalNote: string | null;
  savedAt: string;
}

export interface RemovedWord {
  wordId: string;
  text: string;
  definition: string;
  partOfSpeech: string | null;
  removedAt: string;
  expiresAt: string;
}

export interface ReviewWord {
  userWordId: string;
  wordId: string;
  text: string;
  definition: string;
  ipa: string | null;
  partOfSpeech: string | null;
  audioAccents: string[];
  exampleSentences: string[];
  knownLevel: number;
  nextReviewAt: string | null;
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
