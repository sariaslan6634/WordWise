"use client";

import { useEffect, useRef, useState } from "react";

type Accent = "us" | "gb";
type Status = "idle" | "loading" | "playing" | "error";

const accentLabels: Record<Accent, { short: string; full: string }> = {
  us: { short: "US", full: "Amerikan" },
  gb: { short: "UK", full: "İngiliz" },
};

function Speaker() {
  return (
    <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M11 5 6 9H3v6h3l5 4z" fill="currentColor" stroke="none" />
      <path className="pron-wave pron-wave-1" d="M15.5 9.5a3.5 3.5 0 0 1 0 5" />
      <path className="pron-wave pron-wave-2" d="M18 7a7 7 0 0 1 0 10" />
      <path className="pron-wave pron-wave-3" d="M20.5 4.5a10.5 10.5 0 0 1 0 15" />
    </svg>
  );
}

/**
 * Kelimenin telaffuz butonları: Amerikan (US) ve İngiliz (UK) kayıt varsa ikisi, biri varsa tek buton, hiç yoksa pasif tek buton.
 * Ses dosyaları API'den (WordAudio) gelir; hangi aksanların olduğunu `accents` söyler.
 */
export default function PronunciationButton({ word, accents }: { word: string; accents: string[] }) {
  const available = (["us", "gb"] as Accent[]).filter((accent) => accents.includes(accent));
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const [active, setActive] = useState<Accent | null>(null);
  const [status, setStatus] = useState<Status>("idle");

  useEffect(() => {
    // Kelime değişirse ya da sayfadan çıkılırsa sesi durdur.
    return () => {
      audioRef.current?.pause();
      audioRef.current = null;
    };
  }, [word]);

  function play(accent: Accent) {
    audioRef.current?.pause(); // diğer aksan çalıyorsa sustur
    const audio = new Audio(`/api/backend/api/WordAudio/${accent}/${encodeURIComponent(word)}`);
    audioRef.current = audio;
    setActive(accent);
    setStatus("loading");
    audio.addEventListener("playing", () => { if (audioRef.current === audio) setStatus("playing"); });
    audio.addEventListener("ended", () => { if (audioRef.current === audio) setStatus("idle"); });
    audio.addEventListener("error", () => { if (audioRef.current === audio) setStatus("error"); });
    audio.play().catch(() => { if (audioRef.current === audio) setStatus("error"); });
  }

  if (available.length === 0) {
    return (
      <div className="pron-group">
        <button type="button" className="pron-button" disabled aria-label="Bu kelime için ses dosyası yok" title="Bu kelime için ses dosyası yok">
          <Speaker />
        </button>
      </div>
    );
  }

  return (
    <div className="pron-group">
      {available.map((accent) => {
        const mine = active === accent;
        const label = mine && status === "error" ? "Ses oynatılamadı, tekrar dene" : `${word} — ${accentLabels[accent].full} telaffuzu`;
        return (
          <button
            key={accent}
            type="button"
            className={`pron-button${mine && status === "playing" ? " is-playing" : ""}${mine && status === "error" ? " is-error" : ""}`}
            onClick={() => play(accent)}
            aria-label={label}
            title={label}
          >
            <Speaker />
            {available.length > 1 && <span className="pron-tag">{accentLabels[accent].short}</span>}
          </button>
        );
      })}
    </div>
  );
}
