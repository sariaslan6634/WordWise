"use client";

import Link from "next/link";
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

type Settings = {
  autoNext: boolean;
  loop: boolean;
  captions: boolean; // bizim altyazımız (varsayılan açık)
  sync: boolean; // kelime kelime senkron vurgu (kapalıysa cümle baştan tam görünür)
};

const defaultSettings: Settings = { autoNext: false, loop: false, captions: true, sync: true };

function loadSettings(): Settings {
  try {
    const raw = window.localStorage.getItem(SETTINGS_KEY);
    if (raw) {
      const value = JSON.parse(raw) as Partial<Settings>;
      return {
        autoNext: value.autoNext === true,
        loop: value.loop === true,
        captions: value.captions !== false,
        sync: value.sync !== false,
      };
    }
  } catch { /* tarayıcı depolaması kapalı olabilir */ }
  return defaultSettings;
}

export default function ClipPractice({ initialClips, initialSavedOnly = false }: { initialClips: LearnerClip[]; initialSavedOnly?: boolean }) {
  const [clips, setClips] = useState(initialClips);
  const [index, setIndex] = useState(0);
  const [settings, setSettings] = useState<Settings>(defaultSettings);
  const { autoNext, loop, captions, sync } = settings;
  const [fetchedEmpty, setFetchedEmpty] = useState(initialClips.length === 0);
  const [waiting, setWaiting] = useState(false);
  const [hasInteracted, setHasInteracted] = useState(false);
  const [reportOpen, setReportOpen] = useState(false);
  const [reportDone, setReportDone] = useState(false);
  const [reason, setReason] = useState<ClipReportReason>("CaptionSync");
  const [note, setNote] = useState("");
  const [reporting, setReporting] = useState(false);
  const [error, setError] = useState("");
  const [loadingMore, setLoadingMore] = useState(false);
  const [savedOnly, setSavedOnly] = useState(initialSavedOnly);
  const [savingClip, setSavingClip] = useState(false);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const modalRef = useRef<HTMLDivElement>(null);

  const clip = clips[index];

  useEffect(() => {
    setSettings(loadSettings());
  }, []);

  function update(patch: Partial<Settings>) {
    const next = { ...settings, ...patch };
    setSettings(next);
    try { window.localStorage.setItem(SETTINGS_KEY, JSON.stringify(next)); } catch { /* sorun değil */ }
    return next;
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
    update({ autoNext: value });
    if (!value) clearTimer();
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

  async function loadMore(onlySaved = savedOnly) {
    clearTimer();
    setLoadingMore(true); setError("");
    try {
      const fresh = await request<LearnerClip[]>(`Clips?count=20${onlySaved ? "&savedOnly=true" : ""}`);
      setClips(fresh); setIndex(0); setFetchedEmpty(fresh.length === 0);
    } catch (value) {
      setError(value instanceof Error ? value.message : "Yeni klipler alınamadı.");
    } finally { setLoadingMore(false); }
  }

  function toggleSavedOnly(value: boolean) {
    setSavedOnly(value);
    setHasInteracted(true);
    void loadMore(value);
  }

  async function toggleSaveClip() {
    if (!clip || savingClip) return;
    const next = !clip.isSaved;
    const id = clip.id;
    const mark = (value: boolean) => setClips((current) => current.map((item) => (item.id === id ? { ...item, isSaved: value } : item)));
    setSavingClip(true); setError("");
    mark(next); // anında tepki ver; hata olursa geri al
    try {
      await request<unknown>(`Clips/${encodeURIComponent(id)}/save`, { method: next ? "POST" : "DELETE" });
    } catch (value) {
      mark(!next);
      setError(value instanceof Error ? value.message : "Klip kaydedilemedi.");
    } finally { setSavingClip(false); }
  }

  return <div className="cinema-stage">
    <div className="cinema-bar">
      <label className="cinema-switch">
        <input type="checkbox" role="switch" checked={autoNext} onChange={(event) => toggleAutoNext(event.target.checked)}/>
        <span>Otomatik geçiş<small>Klip bitince 2 sn bekle, sonrakine geç</small></span>
      </label>
      <label className="cinema-switch">
        <input type="checkbox" role="switch" checked={loop} onChange={(event) => update({ loop: event.target.checked })}/>
        <span>Döngü<small>{LOOP_COUNT} kez tekrar oynat</small></span>
      </label>
      <label className="cinema-switch">
        <input type="checkbox" role="switch" checked={captions} onChange={(event) => update({ captions: event.target.checked })}/>
        <span>Altyazı<small>Videonun üstünde göster</small></span>
      </label>
      <label className="cinema-switch">
        <input type="checkbox" role="switch" checked={sync} onChange={(event) => update({ sync: event.target.checked })}/>
        <span>Senkron konuşma<small>Kapalıysa cümle baştan tam görünür</small></span>
      </label>
      <label className="cinema-switch">
        <input type="checkbox" role="switch" checked={savedOnly} onChange={(event) => toggleSavedOnly(event.target.checked)} disabled={loadingMore}/>
        <span>Sadece kaydettiklerim<small>Kayıtlı kliplerden rastgele getir</small></span>
      </label>
      <div className="cinema-bar-end">
        <Link className="cinema-button" href="/learn/clips/saved">★ Kayıtlı klipler</Link>
        {clip && <button type="button" className="cinema-button report" onClick={openReport}>⚑ Videoyu bildir</button>}
      </div>
    </div>

    {loadingMore && !clip ? <p className="cinema-empty">Klipler yükleniyor…</p>
    : !clip ? (fetchedEmpty ? <div className="cinema-done">
      <h2>{savedOnly ? "Kayıtlı klibin yok" : "Henüz klip yok"}</h2>
      <p>{savedOnly ? "Bir klibi “Kaydet” ile kaydedince burada rastgele çıkar. Tüm klipler için “Sadece kaydettiklerim”i kapat." : "Henüz yayınlanmış klip yok. Biraz sonra tekrar bak."}</p>
      {error && <p className="cinema-error" role="alert">{error}</p>}
    </div> : <div className="cinema-done">
      <h2>Bu seri bitti 🎉</h2>
      <p>Tüm klipleri izledin. Yeni bir seri için devam edebilirsin.</p>
      <button type="button" className="cinema-button primary" onClick={() => void loadMore()} disabled={loadingMore}>{loadingMore ? "Yükleniyor…" : "Yeni klipler getir"}</button>
      {error && <p className="cinema-error" role="alert">{error}</p>}
    </div>) : <>
      <div className="clip-stage">
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
          showCaption={captions && !clip.hasBurnedInCaptions}
          sync={sync}
          onFinished={handleFinished}
        />
        <button type="button" className={`clip-save${clip.isSaved ? " is-saved" : ""}`} onClick={toggleSaveClip} disabled={savingClip} aria-pressed={clip.isSaved === true} aria-label={clip.isSaved ? "Kayıtlılardan çıkar" : "Klibi kaydet"} title={clip.isSaved ? "Kayıtlılardan çıkar" : "Klibi kaydet"}>
          <svg viewBox="0 0 24 24" width="20" height="20" fill={clip.isSaved ? "currentColor" : "none"} stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d="M6 3h12a1 1 0 0 1 1 1v17l-7-4.5L5 21V4a1 1 0 0 1 1-1z" /></svg>
        </button>
      </div>

      <div className="cinema-nav">
        <button type="button" className="cinema-button" onClick={() => { setHasInteracted(true); goTo(Math.max(0, index - 1)); }} disabled={index === 0}>← Önceki</button>
        <span>{index + 1} / {clips.length}{waiting ? " · sonraki klip geliyor…" : ""}</span>
        <button type="button" className="cinema-button primary" onClick={() => { setHasInteracted(true); goTo(index + 1); }}>Sonraki →</button>
      </div>

      <div className="clip-source">
        <div className="clip-source-meta">
          <strong>{clip.sourceTitle || "YouTube videosu"}</strong>
          {clip.sourceChannel && <span className="clip-source-channel">Kanal: {clip.sourceChannel}</span>}
        </div>
        <a className="cinema-button" href={`https://www.youtube.com/watch?v=${encodeURIComponent(clip.youtubeId)}&t=${Math.floor(clip.startSec)}s`} target="_blank" rel="noopener noreferrer">YouTube’da izle ↗</a>
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
