"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

type ApiResponse<T = unknown> = { success?: boolean; message?: string; data?: T };

async function readApiResponse<T>(response: Response): Promise<ApiResponse<T> | null> {
  const body = await response.text();
  if (!body.trim()) return null;
  try {
    return JSON.parse(body) as ApiResponse<T>;
  } catch {
    const detail = body.trim().slice(0, 180);
    throw new Error(`Sunucu geçerli JSON döndürmedi (HTTP ${response.status})${detail ? `: ${detail}` : ""}`);
  }
}

function responseError(response: Response, result: ApiResponse | null, fallback: string) {
  return result?.message || `HTTP ${response.status}${response.statusText ? ` ${response.statusText}` : ""}: ${fallback}`;
}

export function ImportWords() {
  const router = useRouter();
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  async function submit() {
    setBusy(true); setMessage("");
    try {
      const response = await fetch("/api/backend/api/admin/AdminWord/daily-candidates", { method: "POST" });
      const result = await readApiResponse<{ successCount: number; skippedCount: number; failedCount: number }>(response);
      if (!response.ok || !result?.success || !result.data) throw new Error(responseError(response, result, "İçe aktarma başarısız oldu."));
      const data = result.data;
      setMessage(`${data.successCount} aday eklendi, ${data.skippedCount} zaten vardı, ${data.failedCount} sözlükte bulunamadı.`);
      router.refresh();
    } catch (error) { setMessage(error instanceof Error ? error.message : "İçe aktarma başarısız oldu."); }
    finally { setBusy(false); }
  }
  return <div className="word-import-form"><p>Datamuse günün kelime adaylarını bulur. Sözlük tanımları otomatik aranır, sen onaylayana kadar kütüphaneye eklenmez.</p><button className="button" onClick={submit} disabled={busy}>{busy ? "Kelime adayları getiriliyor…" : "Bugünün kelime adaylarını getir"}</button>{message && <p role="status">{message}</p>}</div>;
}

export function ReviewWordVideosButton({ id, wordText }: { id: string; wordText: string }) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  async function approve() {
    setBusy(true); setError("");
    try {
      const response = await fetch(`/api/backend/api/AdminVideo/fetch-candidates/${encodeURIComponent(id)}`, { method: "POST" });
      const result = await readApiResponse<number>(response);
      if (!response.ok || result?.success === false) throw new Error(responseError(response, result, "YouTube adayları alınamadı."));
      const params = new URLSearchParams({ wordId: id, word: wordText });
      router.push(`/admin/videos?${params.toString()}`);
    } catch (value) { setError(value instanceof Error ? value.message : "YouTube adayları alınamadı."); }
    finally { setBusy(false); }
  }
  return <span><button className="candidate-approve" onClick={approve} disabled={busy}>{busy ? "Videolar aranıyor…" : "Videoları seç →"}</button>{error && <small role="alert">{error}</small>}</span>;
}
