"use client";

import { useEffect, useRef, useState } from "react";

type Status = "idle" | "loading" | "playing" | "error";

/** Kelimenin telaffuz ses dosyasını oynatan hoparlör butonu. audioUrl yoksa pasif görünür. */
export default function PronunciationButton({ audioUrl, word }: { audioUrl: string | null; word: string }) {
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const [status, setStatus] = useState<Status>("idle");

  useEffect(() => {
    // Kelime/dosya değişirse ya da sayfadan çıkılırsa sesi durdur.
    return () => {
      audioRef.current?.pause();
      audioRef.current = null;
    };
  }, [audioUrl]);

  function play() {
    if (!audioUrl) return;
    if (!audioRef.current) {
      const audio = new Audio(audioUrl);
      audio.preload = "auto";
      audio.addEventListener("playing", () => setStatus("playing"));
      audio.addEventListener("ended", () => setStatus("idle"));
      audio.addEventListener("pause", () => setStatus((current) => (current === "error" ? current : "idle")));
      audio.addEventListener("error", () => setStatus("error"));
      audioRef.current = audio;
    }
    const audio = audioRef.current;
    audio.currentTime = 0; // oynarken tekrar basılırsa baştan çal
    setStatus("loading");
    audio.play().catch(() => setStatus("error"));
  }

  const disabled = !audioUrl;
  const label = disabled
    ? "Bu kelime için ses dosyası yok"
    : status === "error"
      ? "Ses oynatılamadı, tekrar dene"
      : `${word} kelimesinin telaffuzunu dinle`;

  return (
    <button
      type="button"
      className={`pron-button${status === "playing" ? " is-playing" : ""}${status === "error" ? " is-error" : ""}`}
      onClick={play}
      disabled={disabled}
      aria-label={label}
      title={label}
    >
      <svg viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
        <path d="M11 5 6 9H3v6h3l5 4z" fill="currentColor" stroke="none" />
        <path className="pron-wave pron-wave-1" d="M15.5 9.5a3.5 3.5 0 0 1 0 5" />
        <path className="pron-wave pron-wave-2" d="M18 7a7 7 0 0 1 0 10" />
        <path className="pron-wave pron-wave-3" d="M20.5 4.5a10.5 10.5 0 0 1 0 15" />
      </svg>
    </button>
  );
}
