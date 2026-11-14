"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import SyncedCaptionPlayer from "@/components/synced-caption-player";
import type { LearnerClip } from "@/lib/contracts";
import { isSentenceCorrect, isWordCorrect, joinTyped, normalizeInput, parseSentence, revealHint, splitTyped, wordSlots } from "@/lib/dictation";

type Envelope<T> = { success?: boolean; message?: string; data?: T };

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
  const [custom, setCustom] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [url, setUrl] = useState("");
  const [repeat, setRepeat] = useState(false);
  const [pick, setPick] = useState<"auto" | "all">("auto");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [modalError, setModalError] = useState("");
  const [value, setValue] = useState("");
  const [hinted, setHinted] = useState<Set<string>>(() => new Set());
  const [done, setDone] = useState(false);
  const [solved, setSolved] = useState(0);
  const [hasInteracted, setHasInteracted] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const urlRef = useRef<HTMLInputElement>(null);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const clip = clips[index];
  const words = useMemo(() => (clip ? parseSentence(clip.text) : []), [clip]);
  const typed = useMemo(() => splitTyped(value, words), [value, words]);
  const correct = words.length > 0 && isSentenceCorrect(words, typed);

  // Baştan itibaren doğru yazılmış kelimeler kutudan silinir (altyazıda kalır); kutuda sadece sıradaki kelime(ler) görünür.
  const completed = useMemo(() => {
    let count = 0;
    for (let i = 0; i < words.length; i += 1) {
      if (words[i].auto) continue;
      if (!isWordCorrect(words[i], typed[i] ?? "")) break;
      count += 1;
    }
    return count;
  }, [words, typed]);
  const shown = useMemo(() => {
    const parts = value.split(/\s+/);
    if (parts.length <= completed) return "";
    return parts.slice(completed).join(" ");
  }, [value, completed]);

  const clearTimer = useCallback(() => {
    if (timer.current) { clearTimeout(timer.current); timer.current = null; }
  }, []);
  useEffect(() => clearTimer, [clearTimer]);

  const goTo = useCallback((next: number) => {
    clearTimer();
    setIndex(next);
    setValue("");
    setHinted(new Set());
    setDone(false);
    setError("");
  }, [clearTimer]);

  // "3 kere oynat" tercihi tarayıcıda hatırlanır (hafif kişisel tercih; erişilemezse sessizce yok sayılır).
  useEffect(() => { try { if (localStorage.getItem("writing-repeat") === "1") setRepeat(true); } catch { /* yok say */ } }, []);
  function toggleRepeat(next: boolean) {
    setRepeat(next);
    try { localStorage.setItem("writing-repeat", next ? "1" : "0"); } catch { /* yok say */ }
  }

  // Yeni klipte yazı alanına odaklan.
  useEffect(() => { if (clip && !modalOpen) inputRef.current?.focus(); }, [clip?.id, modalOpen]); // eslint-disable-line react-hooks/exhaustive-deps
  useEffect(() => { if (modalOpen) urlRef.current?.focus(); }, [modalOpen]);
  useEffect(() => {
    if (!modalOpen) return;
    const onKey = (event: KeyboardEvent) => { if (event.key === "Escape") setModalOpen(false); };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [modalOpen]);

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
    const prefix = value.split(/\s+/).slice(0, completed).join(" ");
    const tail = next.replace(/^\s+/, "");
    setValue(normalizeInput(prefix ? `${prefix} ${tail}` : tail, words));
  }

  // İpucu sınırsız (ileride bir şeye bağlanacak).
  function takeHint() {
    if (done) return;
    const result = revealHint(words, typed);
    if (!result) return;
    setHasInteracted(true);
    setValue(joinTyped(words, result.typed));
    if (result.key) setHinted((current) => new Set(current).add(result.key!));
    inputRef.current?.focus();
  }

  async function loadAuto() {
    clearTimer();
    setLoading(true); setError("");
    try {
      const fresh = usable(await request<LearnerClip[]>("Clips?count=30"));
      setClips(fresh); setCustom(false); goTo(0);
      if (fresh.length === 0) setError("Şu an alıştırma klibi yok. Biraz sonra tekrar dene.");
    } catch (failure) {
      setError(failure instanceof Error ? failure.message : "Klipler alınamadı.");
    } finally { setLoading(false); }
  }

  async function loadCustom() {
    if (!url.trim()) { setModalError("Bir YouTube bağlantısı yapıştır."); return; }
    clearTimer();
    setLoading(true); setModalError("");
    try {
      const fresh = usable(await request<LearnerClip[]>("Clips/from-video", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ url: url.trim(), mode: pick }),
      }));
      if (fresh.length === 0) throw new Error("Bu videodan uygun bir alıştırma cümlesi çıkarılamadı. Başka bir video dene.");
      setClips(fresh);
      setCustom(true); goTo(0); setHasInteracted(true);
      setModalOpen(false); setUrl("");
    } catch (failure) {
      setModalError(failure instanceof Error ? failure.message : "Video işlenemedi.");
    } finally { setLoading(false); }
  }

  /** Cümle videonun üstünde altyazı gibi çizilir: yazılan harfler renkli, yazılmamışlar silik çizgi, otomatik harfler beyaz. */
  const subtitle = clip && <p className={`write-sub${done ? " is-done" : ""}`} aria-hidden="true">
    {words.map((word, i) => {
      const slots = wordSlots(word, typed[i] ?? "", hinted, i);
      return <span className="write-sub-word" key={i}>
        {word.lead && <span className="write-sub-punct">{word.lead}</span>}
        {slots.map((slot, j) => <span key={j} className={`write-sub-letter is-${slot.status}`}>{slot.char || "\u00a0"}</span>)}
        {word.trail && <span className="write-sub-punct">{word.trail}</span>}
      </span>;
    })}
  </p>;

  const finished = !clip;
  return <div className="cinema-stage write-stage">
    <div className="cinema-bar write-bar">
      <button type="button" className="cinema-button primary" onClick={() => { setModalError(""); setModalOpen(true); }}>Video Ekle</button>
      {custom && <button type="button" className="cinema-button" onClick={() => void loadAuto()} disabled={loading}>Bizim kliplere dön</button>}
      <div className="cinema-bar-end"><span className="write-score">Çözülen: {solved}</span></div>
    </div>

    {error && <p className="cinema-error" role="alert">{error}</p>}

    {loading && !clip ? <p className="cinema-empty">Hazırlanıyor…</p> : finished ? <div className="cinema-done">
      <h2>{clips.length === 0 ? "Alıştırma yok" : "Bu seri bitti 🎉"}</h2>
      <p>{custom ? "Videodaki cümleler bitti. Yeni bir video ekle ya da bizim kliplere dön." : "Tüm cümleleri çözdün. Yeni bir seri için devam edebilirsin."}</p>
      <button type="button" className="cinema-button primary" onClick={() => void loadAuto()} disabled={loading}>{loading ? "Yükleniyor…" : "Yeni klipler getir"}</button>
    </div> : <>
      <SyncedCaptionPlayer
        key={clip.id}
        variant="cinema"
        videoId={clip.youtubeId}
        startSec={clip.startSec}
        endSec={clip.endSec}
        text={clip.text}
        loops={repeat ? 3 : 1}
        autoPlay={hasInteracted || index > 0}
        showCaption={false}
        overlay={subtitle}
      />

      <div className={`write-panel${done ? " is-done" : ""}`}>
        <label className="write-repeat"><input type="checkbox" checked={repeat} onChange={(event) => toggleRepeat(event.target.checked)}/> Videoyu 3 kere oynat</label>
        <input
          ref={inputRef}
          className="write-input"
          type="text"
          value={shown}
          onChange={(event) => onChange(event.target.value)}
          onKeyDown={(event) => { if (event.key === "Enter" && done) { clearTimer(); goTo(index + 1); } }}
          placeholder="Duyduğunu yaz…"
          autoCapitalize="off" autoCorrect="off" autoComplete="off" spellCheck={false}
          disabled={done}
          aria-label="Duyduğun cümle"
        />
        <p className="write-note">Özel isimler ve ' - işaretleri otomatik gelir; sadece harfleri yaz.</p>

        <div className="write-actions">
          <button type="button" className="cinema-button write-hint" onClick={takeHint} disabled={done}>💡 İpucu</button>
          <span className="write-legend"><i className="is-correct"/>doğru <i className="is-hint"/>ipucu <i className="is-wrong"/>yanlış</span>
          <span className="write-progress">{index + 1} / {clips.length}{done ? " · Doğru! 🎉" : ""}</span>
          <button type="button" className="cinema-button" onClick={() => goTo(index + 1)}>{done ? "Sonraki →" : "Atla →"}</button>
        </div>
      </div>
    </>}

    {modalOpen && <div className="cinema-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) setModalOpen(false); }}>
      <form className="cinema-modal" role="dialog" aria-modal="true" aria-label="Video ekle" onSubmit={(event) => { event.preventDefault(); void loadCustom(); }}>
        <h2>Video Ekle</h2>
        <p>Alıştırma yapmak istediğin YouTube videosunun bağlantısını yapıştır. Videodaki cümleler sana yazdırılır.</p>
        <input ref={urlRef} className="write-url-input" type="url" inputMode="url" placeholder="https://www.youtube.com/watch?v=…" value={url} onChange={(event) => setUrl(event.target.value)} aria-label="YouTube bağlantısı"/>
        <div className="write-pick" role="radiogroup" aria-label="Kaç klip alınsın">
          <label><input type="radio" name="write-pick" checked={pick === "auto"} onChange={() => setPick("auto")}/> <span>Otomatik klip al <small>(videodan 5 klip)</small></span></label>
          <label><input type="radio" name="write-pick" checked={pick === "all"} onChange={() => setPick("all")}/> <span>Videonun tamamını getir <small>(en fazla 60 dakikalık videolar)</small></span></label>
        </div>
        {modalError && <p className="cinema-error" role="alert">{modalError}</p>}
        <div className="cinema-modal-actions">
          <button type="button" className="cinema-button" onClick={() => setModalOpen(false)}>Vazgeç</button>
          <button type="submit" className="cinema-button primary" disabled={loading}>{loading ? (pick === "all" ? "Tüm video hazırlanıyor…" : "Hazırlanıyor…") : "Videoyu getir"}</button>
        </div>
      </form>
    </div>}
  </div>;
}
