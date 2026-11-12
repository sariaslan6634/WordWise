"use client";

import { useState } from "react";
import type { LearnerClip } from "@/lib/contracts";

/** Kayıtlı klipler: videodan bir kare + söylenen metin. Aynı "Kaydet" butonuyla kayıttan çıkarılır / geri alınır. */
export default function SavedClipsView({ initialClips }: { initialClips: LearnerClip[] }) {
  const [clips, setClips] = useState(initialClips);
  const [saved, setSaved] = useState<Record<string, boolean>>(() => Object.fromEntries(initialClips.map((clip) => [clip.id, true])));
  const [busy, setBusy] = useState("");
  const [error, setError] = useState("");

  async function toggle(clip: LearnerClip) {
    if (busy) return;
    const next = !saved[clip.id];
    setBusy(clip.id); setError("");
    setSaved((current) => ({ ...current, [clip.id]: next })); // anında tepki; kart yerinde kalır, tekrar basınca geri kaydedilir
    try {
      const response = await fetch(`/api/backend/api/Clips/${encodeURIComponent(clip.id)}/save`, { method: next ? "POST" : "DELETE" });
      if (!response.ok) throw new Error("failed");
    } catch {
      setSaved((current) => ({ ...current, [clip.id]: !next }));
      setError("İşlem yapılamadı, tekrar dene.");
    } finally { setBusy(""); }
  }

  if (clips.length === 0) return <p className="learn-empty">Henüz kayıtlı klibin yok. Listening sayfasında bir klipte “Kaydet”e bas.</p>;

  const kept = Object.values(saved).filter(Boolean).length;
  return <>
    {error && <div className="learn-alert" role="alert">{error}</div>}
    <div className="saved-clips-grid">{clips.map((clip) => {
      const isSaved = saved[clip.id];
      return <article key={clip.id} className={`saved-clip${isSaved ? "" : " is-removed"}`}>
        <a className="saved-clip-thumb" href={`https://www.youtube.com/watch?v=${encodeURIComponent(clip.youtubeId)}&t=${Math.floor(clip.startSec)}s`} target="_blank" rel="noopener noreferrer" aria-label="Videoyu YouTube’da aç">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={`https://i.ytimg.com/vi/${encodeURIComponent(clip.youtubeId)}/hqdefault.jpg`} alt="" loading="lazy"/>
          <span className="saved-clip-play" aria-hidden="true">▶</span>
        </a>
        <div className="saved-clip-body">
          <p className="saved-clip-text">“{clip.text}”</p>
          <small>{[clip.sourceChannel, clip.sourceTitle].filter(Boolean).join(" · ") || "YouTube videosu"}</small>
        </div>
        <button type="button" className={`cinema-button save-clip${isSaved ? " is-saved" : ""}`} onClick={() => toggle(clip)} disabled={busy === clip.id} aria-pressed={isSaved}>
          <svg viewBox="0 0 24 24" width="16" height="16" fill={isSaved ? "currentColor" : "none"} stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d="M6 3h12a1 1 0 0 1 1 1v17l-7-4.5L5 21V4a1 1 0 0 1 1-1z" /></svg>
          {isSaved ? "Kaydedildi" : "Kaydet"}
        </button>
      </article>;
    })}</div>
    {kept === 0 && <p className="learn-empty">Tüm klipleri kayıttan çıkardın. İstersen karttaki “Kaydet” ile geri alabilirsin; sayfayı yenileyince liste temizlenir.</p>}
  </>;
}
