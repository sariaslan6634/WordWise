"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import SyncedCaptionPlayer from "@/components/synced-caption-player";
import type { ClipReportReason, LearnerClip } from "@/lib/contracts";

type Envelope<T> = { success?: boolean; message?: string; data?: T };

const AUTO_NEXT_DELAY_MS = 2000;
const LOOP_COUNT = 3;
const SETTINGS_KEY = "wordwise-clip-settings";

const reasons: { value: ClipReportReason; label: string }[] = [
  { value: "Quality", label: "Video kalitesi kötü" },
  { value: "CaptionSync", label: "Altyazı videoyla uyuşmuyor" },
  { value: "Inappropriate", label: "Uygunsuz içerik" },
  { value: "Other", label: "Diğer" },
];

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api/backend/api/${path}`, init);
  const body = await response.text();
  let parsed: Envelope<T> | null = null;
  try { parsed = body ? (JSON.parse(body) as Envelope<T>) : null; } catch { parsed = null; }
  if (!response.ok || !parsed || parsed.success === false) {
    throw new Error(parsed?.message || `İstek başarısız oldu (HTTP ${response.status}).`);
  }
  return parsed.data as T;
}

function loadSettings() {
  try {
    const raw = window.localStorage.getItem(SETTINGS_KEY);
    if (raw) {
      const value = JSON.parse(raw) as { autoNext?: boolean; loop?: boolean };
      return { autoNext: value.autoNext === true, loop: value.loop === true };
    }
  } catch { /* tarayıcı depolaması kapalı olabilir */ }
  return { autoNext: false, loop: false };
}

export default function ClipPractice({ initialClips }: { initialClips: LearnerClip[] }) {
  const [clips, setClips] = useState(initialClips);
  const [index, setIndex] = useState(0);
  const [autoNext, setAutoNext] = useState(false);
  const [loop, setLoop] = useState(false);
  const [waiting, setWaiting] = useState(false);
  const [hasInteracted, setHasInteracted] = useState(false);
  const [reportOpen, setReportOpen] = useState(false);
  const [reportDone, setReportDone] = useState(false);
  const [reason, setReason] = useState<ClipReportReason>("CaptionSync");
  const [note, setNote] = useState("");
  const [reporting, setReporting] = useState(false);
  const [error, setError] = useState("");
  const [loadingMore, setLoadingMore] = useState(false);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const modalRef = useRef<HTMLDivElement>(null);
  const hadClips = useRef(initialClips.length > 0);

  const clip = clips[index];

  useEffect(() => {
    const saved = loadSettings();
    setAutoNext(saved.autoNext);
    setLoop(saved.loop);
  }, []);

  function saveSettings(next: { autoNext: boolean; loop: boolean }) {
    try { window.localStorage.setItem(SETTINGS_KEY, JSON.stringify(next)); } catch { /* sorun değil */ }
  }

  const clearTimer = useCallback(() => {
    if (timer.current) { clearTimeout(timer.current); timer.current = null; }
    setWaiting(false);
  }, []);

  useEffect(() => clearTimer, [clearTimer]);

  const goTo = useCallback((next: number) => {
    clearTimer();
    setError("");
    setIndex(next);
  }, [clearTimer]);

  const handleFinished = useCallback(() => {
    if (!autoNext) return;
    setWaiting(true);
    timer.current = setTimeout(() => {
      timer.current = null;
      setWaiting(false);
      setIndex((current) => current + 1);
    }, AUTO_NEXT_DELAY_MS);
  }, [autoNext]);

  function toggleAutoNext(value: boolean) {
    setAutoNext(value);
    saveSettings({ autoNext: value, loop });
    if (!value) clearTimer();
  }

  function toggleLoop(value: boolean) {
    setLoop(value);
    saveSettings({ autoNext, loop: value });
  }

  // ---- Bildirim penceresi ----
  function openReport() {
    clearTimer();
    setHasInteracted(true);
    setError("");
    setReportDone(false);
    setReportOpen(true);
  }

  const closeReport = useCallback(() => {
    if (reporting) return;
    if (reportDone && clip) {
      setClips((current) => current.filter((item) => item.id !== clip.id)); // bildirilen klip bir daha gösterilmez
    }
    setReportOpen(false);
    setReportDone(false);
    setNote("");
    setError("");
  }, [reporting, reportDone, clip]);

  useEffect(() => {
    if (!reportOpen) return;
    modalRef.current?.focus();
    const onKey = (event: KeyboardEvent) => { if (event.key === "Escape") closeReport(); };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [reportOpen, closeReport]);

  async function submitReport() {
    if (!clip) return;
    setReporting(true); setError("");
    try {
      await request<unknown>(`Clips/${encodeURIComponent(clip.id)}/report`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ reason, note: note.trim() || null }),
      });
      setReportDone(true);
    } catch (value) {
      setError(value instanceof Error ? value.message : "Bildirim gönderilemedi.");
    } finally { setReporting(false); }
  }

  async function loadMore() {
    setLoadingMore(true); setError("");
    try {
      const fresh = await request<LearnerClip[]>("Clips?count=20");
      setClips(fresh); setIndex(0);
      if (fresh.length > 0) hadClips.current = true;
    } catch (value) {
      setError(value instanceof Error ? value.message : "Yeni klipler alınamadı.");
    } finally { setLoadingMore(false); }
  }

  if (!clip && !hadClips.current) {
    return <p className="cinema-empty">Henüz yayınlanmış klip yok. Biraz sonra tekrar bak.</p>;
  }

  return <div className="cinema-stage">
    {!clip ? <div className="cinema-done">
      <h2>Bu seri bitti 🎉</h2>
      <p>Tüm klipleri izledin. Yeni bir seri için devam edebilirsin.</p>
      <button type="button" className="cinema-button primary" onClick={loadMore} disabled={loadingMore}>{loadingMore ? "Yükleniyor…" : "Yeni klipler getir"}</button>
      {error && <p className="cinema-error" role="alert">{error}</p>}
    </div> : <>
      <SyncedCaptionPlayer
        key={clip.id}
        variant="cinema"
        videoId={clip.youtubeId}
        startSec={clip.startSec}
        endSec={clip.endSec}
        text={clip.text}
        words={clip.words.length > 0 ? clip.words : undefined}
        loops={loop ? LOOP_COUNT : 1}
        autoPlay={hasInteracted || index > 0}
        paused={reportOpen}
        onFinished={handleFinished}
      />

      <div className="cinema-bar">
        <label className="cinema-switch">
          <input type="checkbox" role="switch" checked={autoNext} onChange={(event) => toggleAutoNext(event.target.checked)}/>
          <span>Otomatik geçiş<small>Klip bitince 2 sn bekle, sonrakine geç</small></span>
        </label>
        <label className="cinema-switch">
          <input type="checkbox" role="switch" checked={loop} onChange={(event) => toggleLoop(event.target.checked)}/>
          <span>Döngü<small>{LOOP_COUNT} kez tekrar oynat</small></span>
        </label>
        <div className="cinema-bar-end">
          <button type="button" className="cinema-button report" onClick={openReport}>⚑ Videoyu bildir</button>
        </div>
      </div>

      <div className="cinema-nav">
        <button type="button" className="cinema-button" onClick={() => { setHasInteracted(true); goTo(Math.max(0, index - 1)); }} disabled={index === 0}>← Önceki</button>
        <span>{index + 1} / {clips.length}{waiting ? " · sonraki klip geliyor…" : ""}</span>
        <button type="button" className="cinema-button primary" onClick={() => { setHasInteracted(true); goTo(index + 1); }}>Sonraki →</button>
      </div>
    </>}

    {reportOpen && clip && <div className="cinema-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) closeReport(); }}>
      <div className="cinema-modal" role="dialog" aria-modal="true" aria-labelledby="report-title" tabIndex={-1} ref={modalRef}>
        {reportDone ? <div className="cinema-success">
          <span className="cinema-success-icon" aria-hidden="true">✓</span>
          <h2 id="report-title">Video bildirildi</h2>
          <p>Teşekkürler, ekibimiz inceleyecek. Bu video artık sana gösterilmeyecek.</p>
          <button type="button" className="cinema-button primary" onClick={closeReport}>Kapat</button>
        </div> : <form onSubmit={(event) => { event.preventDefault(); void submitReport(); }} style={{ display: "grid", gap: 16 }}>
          <div><h2 id="report-title">Videoyu bildir</h2><p>Video durduruldu. Bu videoyla ilgili sorun nedir?</p></div>
          <fieldset className="cinema-reasons">
            {reasons.map((item) => <label key={item.value} className="cinema-reason"><input type="radio" name="clip-report-reason" value={item.value} checked={reason === item.value} onChange={() => setReason(item.value)}/>{item.label}</label>)}
          </fieldset>
          <textarea placeholder="İstersen kısaca açıkla (isteğe bağlı)" maxLength={500} value={note} onChange={(event) => setNote(event.target.value)} aria-label="Açıklama (isteğe bağlı)"/>
          {error && <p className="cinema-error" role="alert">{error}</p>}
          <div className="cinema-modal-actions">
            <button type="button" className="cinema-button" onClick={closeReport} disabled={reporting}>Vazgeç</button>
            <button type="submit" className="cinema-button primary" disabled={reporting}>{reporting ? "Gönderiliyor…" : "Bildir"}</button>
          </div>
        </form>}
      </div>
    </div>}
  </div>;
}
