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
/** Vurgulamayı kelime zamanından bu kadar saniye ERKEN başlat (YouTube gecikmesi + altyazının geç görünmesi için). */
export const DEFAULT_LEAD_SEC = 0.15;

export default function SyncedCaptionPlayer({
  videoId,
  startSec,
  endSec,
  text,
  words,
  leadSec,
  loops = 1,
  autoPlay = false,
  onFinished,
}: {
  videoId: string;
  startSec: number;
  endSec: number;
  text: string;
  words?: CaptionWord[];
  /** Vurgu ne kadar erken başlasın (sn). Varsayılan 0.15. */
  leadSec?: number;
  /** Klip kaç kez üst üste oynatılsın (varsayılan 1). Örn. 3 = ilk oynatma + 2 tekrar. */
  loops?: number;
  /** Klip yüklenir yüklenmez otomatik başlasın. */
  autoPlay?: boolean;
  /** Klip (tüm tekrarlarıyla) bittiğinde çağrılır. */
  onFinished?: () => void;
}) {
  const hostRef = useRef<HTMLDivElement>(null);
  const playerRef = useRef<YtPlayer | null>(null);
  const [time, setTime] = useState(startSec);
  const [error, setError] = useState("");
  const [play, setPlay] = useState(1);
  const playsRef = useRef(0);
  const finishedRef = useRef(false);
  const armedRef = useRef(false); // oynatma bitiş saniyesinden uzaklaşınca true olur; tekrar başlarken eski bitişin yeniden sayılmasını önler
  const loopsRef = useRef(loops);
  const onFinishedRef = useRef(onFinished);
  useEffect(() => { loopsRef.current = loops; onFinishedRef.current = onFinished; });
  const lead = leadSec ?? DEFAULT_LEAD_SEC;
  const captionWords = useMemo(() => words ?? estimateWordTimes(text, startSec, endSec), [words, text, startSec, endSec]);

  useEffect(() => {
    let disposed = false;
    let frame = 0;
    let mount: HTMLDivElement | null = null;
    playsRef.current = 0;
    finishedRef.current = false;
    armedRef.current = false;
    const w = window as unknown as YtWindow;
    const previousReady = w.onYouTubeIframeAPIReady;

    // Klip bitti: tekrar kaldıysa baştan oynat, yoksa bitti haberini ver.
    const handleEnd = () => {
      if (finishedRef.current || !armedRef.current) return;
      armedRef.current = false;
      finishedRef.current = true;
      playsRef.current += 1;
      if (playsRef.current < loopsRef.current) {
        setPlay(playsRef.current + 1);
        finishedRef.current = false;
        playerRef.current?.loadVideoById({ videoId, startSeconds: startSec, endSeconds: endSec });
      } else {
        onFinishedRef.current?.();
      }
    };

    const createPlayer = () => {
      if (disposed || !hostRef.current || !w.YT?.Player || playerRef.current) return;
      // YouTube oynatıcısı verdiği elemanı iframe ile değiştirir; bu yüzden her seferinde yeni bir iç eleman veriyoruz.
      mount = document.createElement("div");
      hostRef.current.appendChild(mount);
      playerRef.current = new w.YT.Player(mount, {
        height: "100%",
        width: "100%",
        videoId,
        playerVars: {
          controls: 1,
          enablejsapi: 1,
          playsinline: 1,
          autoplay: autoPlay ? 1 : 0,
          rel: 0,
          start: Math.floor(startSec),
          end: Math.ceil(endSec),
          origin: window.location.origin,
        },
        events: {
          onReady: () => {
            const tick = () => {
              try {
                if (playerRef.current) {
                  const current = playerRef.current.getCurrentTime() || 0;
                  setTime(current);
                  if (!armedRef.current && current < endSec - 0.5) armedRef.current = true;
                  if (current >= endSec - 0.15 && current > startSec + 0.3) handleEnd();
                }
              } catch { /* oynatıcı yüklenirken geçici hata */ }
              frame = requestAnimationFrame(tick);
            };
            frame = requestAnimationFrame(tick);
          },
          onStateChange: (event) => { if (event.data === 0) handleEnd(); },
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
      cancelAnimationFrame(frame);
      playerRef.current?.destroy();
      playerRef.current = null;
      mount?.remove();
    };
  }, [videoId, startSec, endSec, autoPlay]);

  function replay() {
    playsRef.current = 0;
    finishedRef.current = false;
    armedRef.current = false;
    setPlay(1);
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
          const spoken = time >= word.start - lead;
          return (
            <span
              key={`${word.text}-${index}`}
              style={{
                color: spoken ? SPOKEN : FADED,
                transition: "color .08s linear",
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
        Cümleyi tekrar oynat{loops > 1 ? ` · tekrar ${Math.min(play, loops)}/${loops}` : ""}
      </button>
    </div>
  );
}
