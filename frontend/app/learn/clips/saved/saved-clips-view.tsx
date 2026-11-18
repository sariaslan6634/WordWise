"use client";

import Link from "next/link";
import { useState } from "react";
import type { LearnerClip } from "@/lib/contracts";

/** Kayıtlı klipler: videodan bir kare + söylenen metin. "Kaydedildi" butonuna basınca klip kayıttan çıkar ve listeden kalkar. */
export default function SavedClipsView({ initialClips }: { initialClips: LearnerClip[] }) {
  const [clips, setClips] = useState(initialClips);
  const [busy, setBusy] = useState("");
  const [error, setError] = useState("");

  async function unsave(clip: LearnerClip) {
    if (busy) return;
    const position = clips.findIndex((item) => item.id === clip.id);
    setBusy(clip.id); setError("");
    setClips((current) => current.filter((item) => item.id !== clip.id)); // anında listeden kalkar; hata olursa geri gelir
    try {
      const response = await fetch(`/api/backend/api/Clips/${encodeURIComponent(clip.id)}/save`, { method: "DELETE" });
      if (!response.ok) throw new Error("failed");
    } catch {
      setClips((current) => {
        const next = [...current];
        next.splice(Math.min(Math.max(position, 0), next.length), 0, clip);
        return next;
      });
      setError("İşlem yapılamadı, tekrar dene.");
    } finally { setBusy(""); }
  }

  return <>
    <div className="learn-panel-heading">
      <div><span className="panel-kicker">LISTENING</span><h1>Kayıtlı klipler</h1><p>Kaydettiğin klipler; en son kaydettiğin en üstte.</p></div>
      <div className="learn-heading-side">
        <span className="learn-count">{clips.length} klip</span>
        <Link className="button" href="/learn/clips?saved=1">▶ Rastgele oynat</Link>
      </div>
    </div>
    {error && <div className="learn-alert" role="alert">{error}</div>}
    {clips.length === 0 ? <p className="learn-empty">Henüz kayıtlı klibin yok. Listening ya da Writing sayfasında bir klipte kaydet simgesine bas.</p>
    : <div className="saved-clips-grid">{clips.map((clip) => <article key={clip.id} className="saved-clip">
        <a className="saved-clip-thumb" href={`https://www.youtube.com/watch?v=${encodeURIComponent(clip.youtubeId)}&t=${Math.floor(clip.startSec)}s`} target="_blank" rel="noopener noreferrer" aria-label="Videoyu YouTube’da aç">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={`https://i.ytimg.com/vi/${encodeURIComponent(clip.youtubeId)}/hqdefault.jpg`} alt="" loading="lazy"/>
          <span className="saved-clip-play" aria-hidden="true">▶</span>
        </a>
        <div className="saved-clip-body">
          <p className="saved-clip-text">“{clip.text}”</p>
          <small>{[clip.sourceChannel, clip.sourceTitle].filter(Boolean).join(" · ") || "YouTube videosu"}</small>
        </div>
        <button type="button" className="cinema-button save-clip is-saved" onClick={() => unsave(clip)} disabled={busy === clip.id} aria-pressed="true" title="Kayıtlılardan çıkar">
          <svg viewBox="0 0 24 24" width="16" height="16" fill="currentColor" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d="M6 3h12a1 1 0 0 1 1 1v17l-7-4.5L5 21V4a1 1 0 0 1 1-1z" /></svg>
          Kaydedildi
        </button>
      </article>)}</div>}
  </>;
}
