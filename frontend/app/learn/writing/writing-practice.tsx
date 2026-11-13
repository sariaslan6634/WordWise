"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import SyncedCaptionPlayer from "@/components/synced-caption-player";
import type { LearnerClip } from "@/lib/contracts";
import { isSentenceCorrect, joinTyped, normalizeInput, parseSentence, revealHint, splitTyped, wordSlots } from "@/lib/dictation";

type Envelope<T> = { success?: boolean; message?: string; data?: T };

const MAX_HINTS = 3;
const NEXT_DELAY_MS = 1600;

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

/** Videonun kendi altyazısı varsa cevap ekranda görünür; bu klipler yazma alıştırmasında kullanılmaz. */
const usable = (items: LearnerClip[]) => items.filter((clip) => !clip.hasBurnedInCaptions && parseSentence(clip.text).length > 0);

export default function WritingPractice({ initialClips }: { initialClips: LearnerClip[] }) {
  const [clips, setClips] = useState(() => usable(initialClips));
  const [index, setIndex] = useState(0);
  const [source, setSource] = useState<"auto" | "custom">("auto");
  const [url, setUrl] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [value, setValue] = useState("");
  const [hinted, setHinted] = useState<Set<string>>(() => new Set());
  const [hintsLeft, setHintsLeft] = useState(MAX_HINTS);
  const [done, setDone] = useState(false);
  const [solved, setSolved] = useState(0);
  const [hasInteracted, setHasInteracted] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const clip = clips[index];
  const words = useMemo(() => (clip ? parseSentence(clip.text) : []), [clip]);
  const cores = useMemo(() => words.map((word) => word.core), [words]);
  const typed = useMemo(() => splitTyped(value, words.length, cores), [value, words.length, cores]);
  const correct = words.length > 0 && isSentenceCorrect(words, typed);

  const clearTimer = useCallback(() => {
    if (timer.current) { clearTimeout(timer.current); timer.current = null; }
  }, []);
  useEffect(() => clearTimer, [clearTimer]);

  const goTo = useCallback((next: number) => {
    clearTimer();
    setIndex(next);
    setValue("");
    setHinted(new Set());
    setHintsLeft(MAX_HINTS);
    setDone(false);
    setError("");
  }, [clearTimer]);

  // Yeni klipte yazı alanına odaklan.
  useEffect(() => { if (clip) inputRef.current?.focus(); }, [clip?.id]); // eslint-disable-line react-hooks/exhaustive-deps

  // Cümle (ipuçları dahil) doğru yazıldı: kısa bir tebrikten sonra yeni videoya geç.
  useEffect(() => {
    if (!correct || done) return;
    setDone(true);
    setSolved((count) => count + 1);
    timer.current = setTimeout(() => { timer.current = null; goTo(index + 1); }, NEXT_DELAY_MS);
  }, [correct, done, goTo, index]);

  function onChange(next: string) {
    if (done) return;
    setHasInteracted(true);
    setValue(normalizeInput(next, cores));
  }

  function takeHint() {
    if (done || hintsLeft <= 0) return;
    const result = revealHint(words, typed);
    if (!result) return;
    setHasInteracted(true);
    setValue(joinTyped(result.typed));
    if (result.key) setHinted((current) => new Set(current).add(result.key!));
    setHintsLeft((count) => count - 1);
    inputRef.current?.focus();
  }

  async function loadAuto() {
    clearTimer();
    setLoading(true); setError("");
    try {
      const fresh = usable(await request<LearnerClip[]>("Clips?count=30"));
      setClips(fresh); goTo(0);
      if (fresh.length === 0) setError("Şu an alıştırma klibi yok. Biraz sonra tekrar dene.");
    } catch (value2) {
      setError(value2 instanceof Error ? value2.message : "Klipler alınamadı.");
    } finally { setLoading(false); }
  }

  async function loadCustom() {
    if (!url.trim()) { setError("Bir YouTube bağlantısı yapıştır."); return; }
    clearTimer();
    setLoading(true); setError("");
    try {
      const fresh = usable(await request<LearnerClip[]>("Clips/from-video", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ url: url.trim() }),
      }));
      if (fresh.length === 0) throw new Error("Bu videodan uygun bir alıştırma cümlesi çıkarılamadı. Başka bir video dene.");
      setClips(fresh); goTo(0); setHasInteracted(true);
    } catch (value2) {
      setError(value2 instanceof Error ? value2.message : "Video işlenemedi.");
    } finally { setLoading(false); }
  }

  function switchSource(next: "auto" | "custom") {
    if (next === source) return;
    setSource(next);
    setError("");
    if (next === "auto") void loadAuto();
  }

  const finished = !clip;
  return <div className="cinema-stage write-stage">
    <div className="cinema-bar write-bar">
      <div className="write-source" role="group" aria-label="Video kaynağı">
        <button type="button" className={source === "auto" ? "is-active" : ""} onClick={() => switchSource("auto")} aria-pressed={source === "auto"}>Bizim klipler</button>
        <button type="button" className={source === "custom" ? "is-active" : ""} onClick={() => switchSource("custom")} aria-pressed={source === "custom"}>Kendi YouTube videom</button>
      </div>
      {source === "custom" && <form className="write-url" onSubmit={(event) => { event.preventDefault(); void loadCustom(); }}>
        <input type="url" inputMode="url" placeholder="YouTube bağlantısını yapıştır (https://youtube.com/watch?v=…)" value={url} onChange={(event) => setUrl(event.target.value)} aria-label="YouTube bağlantısı"/>
        <button type="submit" className="cinema-button primary" disabled={loading}>{loading ? "Hazırlanıyor…" : "Videoyu getir"}</button>
      </form>}
      <div className="cinema-bar-end"><span className="write-score">Çözülen: {solved}</span></div>
    </div>

    {error && <p className="cinema-error" role="alert">{error}</p>}

    {loading && !clip ? <p className="cinema-empty">Hazırlanıyor…</p> : finished ? <div className="cinema-done">
      <h2>{clips.length === 0 ? "Alıştırma yok" : "Bu seri bitti 🎉"}</h2>
      <p>{source === "custom" ? "Videodaki cümleler bitti. Yeni bir video bağlantısı gir ya da bizim kliplere dön." : "Tüm cümleleri çözdün. Yeni bir seri için devam edebilirsin."}</p>
      <button type="button" className="cinema-button primary" onClick={() => { setSource("auto"); void loadAuto(); }} disabled={loading}>{loading ? "Yükleniyor…" : "Yeni klipler getir"}</button>
    </div> : <>
      <SyncedCaptionPlayer
        key={clip.id}
        variant="cinema"
        videoId={clip.youtubeId}
        startSec={clip.startSec}
        endSec={clip.endSec}
        text={clip.text}
        loops={1}
        autoPlay={hasInteracted || index > 0}
        showCaption={false}
      />

      <div className={`write-panel${done ? " is-done" : ""}`}>
        <div className="write-words" onClick={() => inputRef.current?.focus()} aria-label="Duyduğun cümleyi yaz">
          {words.map((word, i) => {
            const slots = wordSlots(word.core, typed[i] ?? "", hinted, i);
            return <span className="write-word" key={i}>
              {word.lead && <span className="write-punct">{word.lead}</span>}
              {slots.map((slot, j) => <span key={j} className={`write-letter is-${slot.status}`}>{slot.char || " "}</span>)}
              {word.trail && <span className="write-punct">{word.trail}</span>}
            </span>;
          })}
        </div>

        <input
          ref={inputRef}
          className="write-input"
          type="text"
          value={value}
          onChange={(event) => onChange(event.target.value)}
          onKeyDown={(event) => { if (event.key === "Enter" && done) { clearTimer(); goTo(index + 1); } }}
          placeholder="Duyduğun cümleyi buraya yaz…"
          autoCapitalize="off" autoCorrect="off" autoComplete="off" spellCheck={false}
          disabled={done}
          aria-label="Duyduğun cümle"
        />

        <div className="write-actions">
          <button type="button" className="cinema-button write-hint" onClick={takeHint} disabled={done || hintsLeft <= 0}>💡 İpucu ({hintsLeft})</button>
          <span className="write-legend"><i className="is-correct"/>doğru <i className="is-hint"/>ipucu <i className="is-wrong"/>yanlış</span>
          <span className="write-progress">{index + 1} / {clips.length}{done ? " · Doğru! 🎉" : ""}</span>
          <button type="button" className="cinema-button" onClick={() => goTo(index + 1)}>{done ? "Sonraki →" : "Atla →"}</button>
        </div>
      </div>
    </>}
  </div>;
}
