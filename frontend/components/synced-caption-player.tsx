"use client";

import { useEffect, useMemo, useRef, useState } from "react";

export type CaptionWord = { text: string; start: number; end: number };

type YtPlayer = {
  destroy(): void;
  getCurrentTime(): number;
  loadVideoById(options: { videoId: string; startSeconds: number; endSeconds: number }): void;
};
type YtApi = {
  PlayerState: { PLAYING: number };
  Player: new (
    element: HTMLElement,
    options: {
      height: string;
      width: string;
      videoId: string;
      playerVars: Record<string, string | number>;
      events: { onReady: () => void; onStateChange: (event: { data: number }) => void };
    },
  ) => YtPlayer;
};
type YtWindow = { YT?: YtApi; onYouTubeIframeAPIReady?: () => void };

/** Kelime zamanı yoksa cümle süresini harf sayısına orantılı bölerek tahmin eder. */
export function estimateWordTimes(text: string, startSec: number, endSec: number): CaptionWord[] {
  const tokens = text.trim().split(/\s+/).filter(Boolean);
  if (tokens.length === 0 || endSec <= startSec) return [];
  const weights = tokens.map((token) => Math.max(token.replace(/[^\p{L}\p{N}]/gu, "").length, 1));
  const total = weights.reduce((sum, value) => sum + value, 0);
  const span = endSec - startSec;
  let cursor = startSec;
  return tokens.map((token, index) => {
    const duration = (span * weights[index]) / total;
    const word = { text: token, start: cursor, end: cursor + duration };
    cursor += duration;
    return word;
  });
}

const FADED = "#b9bfb4";
const SPOKEN = "#e3b800";

export default function SyncedCaptionPlayer({
  videoId,
  startSec,
  endSec,
  text,
  words,
}: {
  videoId: string;
  startSec: number;
  endSec: number;
  text: string;
  words?: CaptionWord[];
}) {
  const hostRef = useRef<HTMLDivElement>(null);
  const playerRef = useRef<YtPlayer | null>(null);
  const [time, setTime] = useState(startSec);
  const [error, setError] = useState("");
  const captionWords = useMemo(() => words ?? estimateWordTimes(text, startSec, endSec), [words, text, startSec, endSec]);

  useEffect(() => {
    let disposed = false;
    let timer: ReturnType<typeof setInterval> | null = null;
    const w = window as unknown as YtWindow;
    const previousReady = w.onYouTubeIframeAPIReady;

    const createPlayer = () => {
      if (disposed || !hostRef.current || !w.YT?.Player || playerRef.current) return;
      playerRef.current = new w.YT.Player(hostRef.current, {
        height: "100%",
        width: "100%",
        videoId,
        playerVars: {
          controls: 1,
          enablejsapi: 1,
          playsinline: 1,
          rel: 0,
          start: Math.floor(startSec),
          end: Math.ceil(endSec),
          origin: window.location.origin,
        },
        events: {
          onReady: () => {
            timer = setInterval(() => {
              try {
                if (playerRef.current) setTime(playerRef.current.getCurrentTime() || 0);
              } catch { /* oynatıcı yüklenirken geçici hata */ }
            }, 80);
          },
          onStateChange: () => undefined,
        },
      });
    };

    if (w.YT?.Player) createPlayer();
    else {
      w.onYouTubeIframeAPIReady = () => {
        previousReady?.();
        createPlayer();
      };
      let script = document.querySelector<HTMLScriptElement>('script[src="https://www.youtube.com/iframe_api"]');
      if (!script) {
        script = document.createElement("script");
        script.src = "https://www.youtube.com/iframe_api";
        script.async = true;
        script.onerror = () => setError("YouTube oynatıcısı yüklenemedi.");
        document.head.appendChild(script);
      }
    }

    return () => {
      disposed = true;
      if (timer) clearInterval(timer);
      playerRef.current?.destroy();
      playerRef.current = null;
    };
  }, [videoId, startSec, endSec]);

  function replay() {
    playerRef.current?.loadVideoById({ videoId, startSeconds: startSec, endSeconds: endSec });
  }

  return (
    <div style={{ display: "grid", gap: 18, maxWidth: 760, margin: "0 auto" }}>
      <div style={{ aspectRatio: "16 / 9", background: "#17221c", borderRadius: 12, overflow: "hidden" }}>
        <div ref={hostRef} style={{ width: "100%", height: "100%" }} />
      </div>
      {error && <p role="alert" style={{ color: "#a54b43", fontSize: 12 }}>{error}</p>}
      <p
        aria-live="off"
        style={{
          fontFamily: "Georgia, 'Times New Roman', serif",
          fontSize: 30,
          lineHeight: 1.5,
          textAlign: "center",
          margin: 0,
        }}
      >
        {captionWords.map((word, index) => {
          const spoken = time >= word.start;
          return (
            <span
              key={`${word.text}-${index}`}
              style={{
                color: spoken ? SPOKEN : FADED,
                transition: "color .15s ease",
                marginRight: "0.3em",
                display: "inline-block",
              }}
            >
              {word.text}
            </span>
          );
        })}
      </p>
      <button
        type="button"
        onClick={replay}
        style={{
          justifySelf: "center",
          border: "1px solid #e7ebe2",
          borderRadius: 999,
          background: "#fff",
          padding: "8px 16px",
          fontSize: 12,
          cursor: "pointer",
        }}
      >
        Cümleyi tekrar oynat
      </button>
    </div>
  );
}
