"use client";

import { useEffect, useMemo, useRef, useState, type CSSProperties } from "react";

export type CaptionWord = { text: string; start: number; end: number };

type YtPlayer = {
  destroy(): void;
  getCurrentTime(): number;
  pauseVideo(): void;
  playVideo(): void;
  setVolume(volume: number): void;
  mute(): void;
  unMute(): void;
  loadVideoById(options: { videoId: string; startSeconds: number; endSeconds: number }): void;
  cueVideoById(options: { videoId: string; startSeconds: number; endSeconds: number }): void;
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

const VOLUME_KEY = "wordwise-volume";

function VolumeIcon({ level }: { level: number }) {
  // level: 0 = sessiz, 1 = kısık, 2 = orta, 3 = yüksek
  return (
    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M11 5 6 9H3v6h3l5 4z" fill="currentColor" stroke="none" />
      {level === 0 && <path d="m16 9 5 6m0-6-5 6" />}
      {level >= 1 && <path d="M15.5 9.5a3.5 3.5 0 0 1 0 5" />}
      {level >= 2 && <path d="M18 7a7 7 0 0 1 0 10" />}
      {level >= 3 && <path d="M20.5 4.5a10.5 10.5 0 0 1 0 15" />}
    </svg>
  );
}

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
  showCaption = true,
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
  /** false ise bizim altyazımız gösterilmez (videonun kendi altyazısı varsa). */
  showCaption?: boolean;
}) {
  const hostRef = useRef<HTMLDivElement>(null);
  const playerRef = useRef<YtPlayer | null>(null);
  const [time, setTime] = useState(startSec);
  const [error, setError] = useState("");
  const [play, setPlay] = useState(1);
  const [volume, setVolume] = useState(100);
  const [muted, setMuted] = useState(false);
  const lastVolumeRef = useRef(70);
  const [started, setStarted] = useState(false); // video gerçekten oynamaya başlayana kadar altyazı gizli
  const [ended, setEnded] = useState(false); // klip bitti: ortada büyük tekrar butonu göster
  const playsRef = useRef(0);
  const finishedRef = useRef(false);
  const armedRef = useRef(false); // oynatma bitiş saniyesinden uzaklaşınca true olur; tekrar başlarken eski bitişin yeniden sayılmasını önler
  const loopsRef = useRef(loops);
  const onFinishedRef = useRef(onFinished);
  useEffect(() => { loopsRef.current = loops; onFinishedRef.current = onFinished; });
  const stageRef = useRef<HTMLDivElement>(null);
  const [isFs, setIsFs] = useState(false);
  useEffect(() => {
    const onChange = () => setIsFs(document.fullscreenElement === stageRef.current);
    document.addEventListener("fullscreenchange", onChange);
    return () => document.removeEventListener("fullscreenchange", onChange);
  }, []);
  function toggleFullscreen() {
    try {
      if (document.fullscreenElement) void document.exitFullscreen();
      else void stageRef.current?.requestFullscreen();
    } catch { /* tam ekran desteklenmiyor olabilir */ }
  }
  function applyVolume(next: number) {
    const value = Math.max(0, Math.min(100, Math.round(next)));
    setVolume(value);
    setMuted(value === 0);
    if (value > 0) lastVolumeRef.current = value;
    try {
      playerRef.current?.setVolume(value);
      if (value === 0) playerRef.current?.mute(); else playerRef.current?.unMute();
    } catch { /* oynatıcı henüz hazır değil */ }
    try { window.localStorage.setItem(VOLUME_KEY, String(value)); } catch { /* depolama kapalı olabilir */ }
  }
  function toggleMute() {
    applyVolume(muted || volume === 0 ? lastVolumeRef.current : 0);
  }
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
        // Klip bitti: videoyu başa sarılmış, durmuş hâlde hazırla. Oynat'a basınca baştan oynar (kaldığı yerden değil).
        try { playerRef.current?.cueVideoById({ videoId, startSeconds: startSec, endSeconds: endSec }); } catch { /* oynatıcı hazır değil */ }
        setEnded(true);
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
          fs: variant === "cinema" ? 0 : 1, // sinema modunda altyazı videonun üstünde olduğu için tam ekranı biz yönetiriz
          rel: 0,
          start: Math.floor(startSec),
          end: Math.ceil(endSec),
          origin: window.location.origin,
        },
        events: {
          onReady: () => {
            // Kullanıcının son seçtiği ses düzeyini uygula.
            try {
              const saved = Number(window.localStorage.getItem(VOLUME_KEY));
              if (window.localStorage.getItem(VOLUME_KEY) !== null && Number.isFinite(saved)) {
                const value = Math.max(0, Math.min(100, saved));
                setVolume(value);
                setMuted(value === 0);
                if (value > 0) lastVolumeRef.current = value;
                playerRef.current?.setVolume(value);
                if (value === 0) playerRef.current?.mute();
              }
            } catch { /* depolama kapalı olabilir */ }
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
          onStateChange: (event) => {
            if (event.data === 0) handleEnd();
            if (event.data === 1) { setStarted(true); setEnded(false); }
            // Klip bittikten sonra kullanıcı tekrar oynata bastı: sayaçları sıfırla, baştan oynasın.
            if (event.data === 1 && finishedRef.current) {
              playsRef.current = 0;
              finishedRef.current = false;
              armedRef.current = false;
              setPlay(1);
            }
          },
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
      setStarted(false);
      setEnded(false);
      cancelAnimationFrame(frame);
      playerRef.current?.destroy();
      playerRef.current = null;
      mount?.remove();
    };
  }, [videoId, startSec, endSec, autoPlay, variant]);

  function replay() {
    setStarted(false);
    setEnded(false);
    playsRef.current = 0;
    finishedRef.current = false;
    armedRef.current = false;
    setPlay(1);
    playerRef.current?.loadVideoById({ videoId, startSeconds: startSec, endSeconds: endSec });
  }

  const cinema = variant === "cinema";
  const caption = (
    <p className="scp-caption" aria-live="off">
      <span className="scp-caption-inner">
        {captionWords.map((word, index) => (
          <span key={`${word.text}-${index}`} className={`scp-word${time >= word.start - lead ? " on" : ""}`}>
            {word.text}
          </span>
        ))}
      </span>
    </p>
  );

  return (
    <div className={`scp${cinema ? " scp-cinema" : ""}`}>
      <div className="scp-video" ref={stageRef}>
        <div ref={hostRef} className="scp-host" />
        {cinema && showCaption && started && caption}
        {ended && (
          <div className="scp-end">
            <button type="button" onClick={replay} aria-label="Cümleyi baştan oynat" title="Cümleyi baştan oynat"><span aria-hidden="true">↻</span></button>
          </div>
        )}
        {cinema && (
          <div className="scp-tools">
            <div className="scp-volume" style={{ "--v": `${muted ? 0 : volume}%` } as CSSProperties}>
              <button type="button" onClick={toggleMute} aria-label={muted || volume === 0 ? "Sesi aç" : "Sesi kapat"} title={muted || volume === 0 ? "Sesi aç" : "Sesi kapat"}>
                <VolumeIcon level={muted || volume === 0 ? 0 : volume < 34 ? 1 : volume < 67 ? 2 : 3} />
              </button>
              <input
                type="range"
                min={0}
                max={100}
                step={1}
                value={muted ? 0 : volume}
                onChange={(event) => applyVolume(Number(event.target.value))}
                aria-label="Ses düzeyi"
              />
            </div>
            <button type="button" onClick={replay} aria-label="Cümleyi tekrar oynat" title="Cümleyi tekrar oynat">
              ↻{loops > 1 ? <span>{Math.min(play, loops)}/{loops}</span> : null}
            </button>
            <button type="button" onClick={toggleFullscreen} aria-label={isFs ? "Tam ekrandan çık" : "Tam ekran"} title={isFs ? "Tam ekrandan çık" : "Tam ekran"}>{isFs ? "🗗" : "⛶"}</button>
          </div>
        )}
      </div>
      {error && <p role="alert" className="scp-error">{error}</p>}
      {!cinema && showCaption && started && caption}
      {!cinema && (
        <button type="button" className="scp-replay" onClick={replay}>
          Cümleyi tekrar oynat{loops > 1 ? ` · tekrar ${Math.min(play, loops)}/${loops}` : ""}
        </button>
      )}
    </div>
  );
}
