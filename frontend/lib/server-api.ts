import "server-only";

import type { AdminUser, ApiEnvelope, AuthResponse, DashboardStats, PagedResponse, PendingContent, SavedWord, UserProfile, VideoCandidate, VideoCandidateReview, WordDetail, WordSummary } from "./contracts";

export const apiBaseUrl = (process.env.WORDWISE_API_URL || "http://localhost:5271").replace(/\/$/, "");

export async function apiRequest<T>(path: string, token?: string, init?: RequestInit): Promise<T | null> {
  try {
    const response = await fetch(`${apiBaseUrl}${path}`, {
      ...init,
      cache: "no-store",
      headers: {
        ...(init?.body ? { "Content-Type": "application/json" } : {}),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...init?.headers,
      },
    });
    if (!response.ok) return null;
    const envelope = (await response.json()) as ApiEnvelope<T>;
    return envelope.success ? envelope.data : null;
  } catch {
    return null;
  }
}

export function readAuthResponse(value: unknown): AuthResponse | null {
  if (!value || typeof value !== "object") return null;
  const envelope = value as ApiEnvelope<AuthResponse>;
  if (!envelope.success || !envelope.data?.token) return null;
  return envelope.data;
}

export async function getAdminStats(token?: string) {
  return apiRequest<DashboardStats>("/api/AdminDashboard/stats", token);
}

export async function getAdminWords(token?: string, options: { page?: number; pageSize?: number; searchText?: string; isApproved?: boolean; cefrLevel?: string; partOfSpeech?: string; status?: string } = {}) {
  const params = new URLSearchParams({ page: String(options.page ?? 1), pageSize: String(options.pageSize ?? 7) });
  if (options.searchText) params.set("searchText", options.searchText);
  if (options.isApproved !== undefined) params.set("isApproved", String(options.isApproved));
  if (options.cefrLevel) params.set("cefrLevel", options.cefrLevel);
  if (options.partOfSpeech) params.set("partOfSpeech", options.partOfSpeech);
  if (options.status) params.set("status", options.status);
  return apiRequest<PagedResponse<WordSummary>>(`/api/admin/AdminWord?${params}`, token);
}

export async function getAdminUsers(token?: string) {
  return apiRequest<PagedResponse<AdminUser>>("/api/AdminDashboard/users?page=1&pageSize=20", token);
}

export async function getAdminPendingContent(token?: string) {
  return apiRequest<PendingContent>("/api/AdminDashboard/pending-content", token);
}

export async function getAdminVideoCandidatesByWordId(wordId: string, token?: string) {
  return apiRequest<VideoCandidate[]>(`/api/AdminVideo/candidates/${encodeURIComponent(wordId)}`, token);
}

export async function getAdminVideoCandidateReview(candidateId: string, token?: string) {
  return apiRequest<VideoCandidateReview>(`/api/AdminVideo/candidates/${encodeURIComponent(candidateId)}/review`, token);
}

export async function getWordByText(text: string, token?: string) {
  return apiRequest<WordDetail>(`/api/Words/search?text=${encodeURIComponent(text)}`, token);
}

export async function getUserProfile(token?: string) {
  return apiRequest<UserProfile>("/api/Profile", token);
}

export async function getSavedWords(token?: string, pageSize = 5) {
  return apiRequest<PagedResponse<SavedWord>>(`/api/UserWord?page=1&pageSize=${pageSize}`, token);
}
