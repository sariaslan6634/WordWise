"use client";

import { useEffect, useRef, useState } from "react";

type PlayerEvent = { target: YouTubePlayer; data?: number };
type YouTubePlayer = {
  destroy(): void;
  getCurrentTime(): number;
  getDuration(): number;
  getPlayerState(): number;
  loadVideoById(options: { videoId: string; startSeconds: number; endSeconds: number }): void;
  pauseVideo(): void;
  playVideo(): void;
  seekTo(seconds: number, allowSeekAhead: boolean): void;
};
type YouTubeApi = {
  PlayerState: { ENDED: number; PAUSED: number; PLAYING: number };
  Player: new (element: HTMLElement, options: {
    height: string;
    width: string;
    videoId: string;
    playerVars: Record<string, string | number>;
    events: { onReady: (event: PlayerEvent) => void; onStateChange: (event: PlayerEvent) => void; onError: (event: PlayerEvent) => void };
  }) => YouTubePlayer;
};

declare global {
  interface Window {
    YT?: YouTubeApi;
    onYouTubeIframeAPIReady?: () => void;
  }
}

function timeLabel(seconds: number) {
  const safe = Number.isFinite(seconds) ? Math.max(0, Math.floor(seconds)) : 0;
  return `${Math.floor(safe / 60).toString().padStart(2, "0")}:${(safe % 60).toString().padStart(2, "0")}`;
}

export default function YouTubeSegmentPlayer({
  videoId,
  startSec,
  endSec,
  onUseStart,
  onUseEnd,
}: {
  videoId: string;
  startSec: number;
  endSec: number;
  onUseStart: (seconds: number) => void;
  onUseEnd: (seconds: number) => void;
}) {
  const hostRef = useRef<HTMLDivElement>(null);
  const playerRef = useRef<YouTubePlayer | null>(null);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const seekingRef = useRef(false);
  const [ready, setReady] = useState(false);
  const [currentTime, setCurrentTime] = useState(0);
  const [duration, setDuration] = useState(0);
  const [playing, setPlaying] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    let disposed = false;
    const previousReady = window.onYouTubeIframeAPIReady;

    const createPlayer = () => {
      if (disposed || !hostRef.current || !window.YT?.Player || playerRef.current) return;
      playerRef.current = new window.YT.Player(hostRef.current, {
        height: "100%",
        width: "100%",
        videoId,
        playerVars: { controls: 1, enablejsapi: 1, playsinline: 1, rel: 0, origin: window.location.origin },
        events: {
          onReady: (event) => {
            if (disposed) return;
            setReady(true);
            const updateTime = () => {
              if (seekingRef.current) return;
              try {
                setCurrentTime(event.target.getCurrentTime() || 0);
                setDuration(event.target.getDuration() || 0);
                setPlaying(event.target.getPlayerState() === window.YT?.PlayerState.PLAYING);
              } catch { /* Player may be changing state while the iframe loads. */ }
            };
            updateTime();
            timerRef.current = setInterval(updateTime, 300);
          },
          onStateChange: (event) => setPlaying(event.data === window.YT?.PlayerState.PLAYING),
          onError: (event) => setError(`YouTube oynatıcısı bu videoyu açamadı (hata ${event.data ?? "bilinmiyor"}). Tam videoyu YouTube'da açıp farklı bir aday seçebilirsin.`),
        },
      });
    };

    if (window.YT?.Player) createPlayer();
    else {
      window.onYouTubeIframeAPIReady = () => {
        previousReady?.();
        createPlayer();
      };
      let script = document.querySelector<HTMLScriptElement>('script[src="https://www.youtube.com/iframe_api"]');
      if (!script) {
        script = document.createElement("script");
        script.src = "https://www.youtube.com/iframe_api";
        script.async = true;
        script.onerror = () => setError("YouTube oynatıcısı yüklenemedi. İnternet bağlantısını kontrol et.");
        document.head.appendChild(script);
      }
    }

    return () => {
      disposed = true;
      if (timerRef.current) clearInterval(timerRef.current);
      playerRef.current?.destroy();
      playerRef.current = null;
      if (window.onYouTubeIframeAPIReady && !window.YT?.Player) window.onYouTubeIframeAPIReady = previousReady;
    };
  }, [videoId]);

  function seek(seconds: number, allowSeekAhead = true) {
    const next = Math.max(0, Math.min(duration || seconds, seconds));
    playerRef.current?.seekTo(next, allowSeekAhead);
    setCurrentTime(next);
  }

  function previewSelectedRange() {
    if (!Number.isFinite(startSec) || !Number.isFinite(endSec) || endSec <= startSec) return;
    playerRef.current?.loadVideoById({ videoId, startSeconds: startSec, endSeconds: endSec });
  }

  return <div className="youtube-segment-player">
    <div className="clip-player"><div className="youtube-player-host"><div ref={hostRef}/></div></div>
    {error && <p className="preview-error" role="alert">{error}</p>}
    <div className="player-time-row"><span>{timeLabel(currentTime)}</span><span>{timeLabel(duration)}</span></div>
    <input className="player-seekbar" type="range" min="0" max={Math.max(duration, 1)} step="0.1" value={Math.min(currentTime, duration || 0)} aria-label="Video zaman çizgisi" disabled={!ready || duration <= 0} onPointerDown={() => { seekingRef.current = true; }} onChange={(event) => seek(Number(event.target.value), false)} onPointerUp={(event) => { seek(Number(event.currentTarget.value), true); seekingRef.current = false; }} onKeyDown={() => { seekingRef.current = true; }} onKeyUp={(event) => { if (event.key.startsWith("Arrow")) seek(Number(event.currentTarget.value), true); seekingRef.current = false; }}/>
    <div className="player-controls"><button type="button" disabled={!ready} onClick={() => seek(currentTime - 5)}>−5 sn</button><button type="button" disabled={!ready} onClick={() => playing ? playerRef.current?.pauseVideo() : playerRef.current?.playVideo()}>{playing ? "Duraklat" : "Oynat"}</button><button type="button" disabled={!ready} onClick={() => seek(currentTime + 5)}>+5 sn</button><span className="player-control-spacer"/><button type="button" disabled={!ready} onClick={() => onUseStart(Math.floor(currentTime))}>Bu saniyeyi başlangıç yap</button><button type="button" disabled={!ready} onClick={() => onUseEnd(Math.floor(currentTime))}>Bu saniyeyi bitiş yap</button></div>
    <button className="preview-range-button" type="button" disabled={!ready || endSec <= startSec} onClick={previewSelectedRange}>Seçili aralığı oynat ({timeLabel(startSec)}–{timeLabel(endSec)})</button>
  </div>;
}
