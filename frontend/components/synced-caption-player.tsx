"use client";

import { useEffect, useMemo, useRef, useState } from "react";

export type CaptionWord = { text: string; start: number; end: number };

type YtPlayer = {
  destroy(): void;
  getCurrentTime(): number;
  pauseVideo(): void;
  playVideo(): void;
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
  variant = "default",
  paused = false,
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
  /** "cinema": koyu, geniş (16:9) sinema görünümü. */
  variant?: "default" | "cinema";
  /** true iken video durdurulur; false olunca (biz durdurduysak) kaldığı yerden devam eder. */
  paused?: boolean;
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
  const pausedByUsRef = useRef(false);
  useEffect(() => {
    const player = playerRef.current;
    if (!player) return;
    try {
      if (paused) { player.pauseVideo(); pausedByUsRef.current = true; }
      else if (pausedByUsRef.current) { pausedByUsRef.current = false; player.playVideo(); }
    } catch { /* oynatıcı henüz hazır değil */ }
  }, [paused]);
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
    <div className={`scp${variant === "cinema" ? " scp-cinema" : ""}`}>
      <div className="scp-video">
        <div ref={hostRef} className="scp-host" />
      </div>
      {error && <p role="alert" className="scp-error">{error}</p>}
      <p className="scp-caption" aria-live="off">
        <span className="scp-caption-inner">
          {captionWords.map((word, index) => (
            <span key={`${word.text}-${index}`} className={`scp-word${time >= word.start - lead ? " on" : ""}`}>
              {word.text}
            </span>
          ))}
        </span>
      </p>
      <button type="button" className="scp-replay" onClick={replay}>
        Cümleyi tekrar oynat{loops > 1 ? ` · tekrar ${Math.min(play, loops)}/${loops}` : ""}
      </button>
    </div>
  );
}
