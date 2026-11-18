"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import type { DiscoveryRunResult, DiscoveryRunStatus } from "@/lib/contracts";

type Envelope<T> = { success?: boolean; message?: string; data?: T };

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api/backend/api/AdminVideo/${path}`, init);
  const body = await response.text();
  let parsed: Envelope<T> | null = null;
  try { parsed = body ? (JSON.parse(body) as Envelope<T>) : null; } catch { parsed = null; }
  if (!response.ok || !parsed || parsed.success === false) {
    throw new Error(parsed?.message || `İstek başarısız oldu (HTTP ${response.status}).`);
  }
  return parsed.data as T;
}

function formatWait(ms: number) {
  const total = Math.max(0, Math.ceil(ms / 1000));
  const hours = Math.floor(total / 3600);
  const minutes = Math.floor((total % 3600) / 60);
  const seconds = total % 60;
  if (hours > 0) return `${hours} sa ${minutes} dk`;
  if (minutes > 0) return `${minutes} dk ${seconds} sn`;
  return `${seconds} sn`;
}

/** Admin genel bakışta "Klip getir": otomatik keşfi bir kez çalıştırır. YouTube kotası için günlük tıklama hakkı vardır; hak bitince geri sayım gösterilir. */
export default function DiscoveryRunButton({ initialStatus }: { initialStatus: DiscoveryRunStatus | null }) {
  const [status, setStatus] = useState(initialStatus);
  const [running, setRunning] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [tick, setTick] = useState(() => Date.now());
  // Sunucu saati ile tarayıcı saati arasındaki fark: geri sayım sunucuya göre doğru olsun.
  const offsetRef = useRef(initialStatus ? Date.parse(initialStatus.serverNow) - Date.now() : 0);

  const apply = useCallback((next: DiscoveryRunStatus) => {
    offsetRef.current = Date.parse(next.serverNow) - Date.now();
    setStatus(next);
    setTick(Date.now());
  }, []);

  const refresh = useCallback(async () => {
    try { apply(await request<DiscoveryRunStatus>("auto-discovery/status")); } catch { /* sessizce geç: eski durum kalır */ }
  }, [apply]);

  const exhausted = !!status && status.remaining <= 0 && !!status.nextAvailableAt;
  const waitMs = exhausted ? Date.parse(status!.nextAvailableAt!) - (tick + offsetRef.current) : 0;

  // Hak bittiyse her saniye geri say; süre dolunca durumu yeniden al.
  useEffect(() => {
    if (!exhausted) return;
    const timer = setInterval(() => setTick(Date.now()), 1000);
    return () => clearInterval(timer);
  }, [exhausted]);
  useEffect(() => { if (exhausted && waitMs <= 0) void refresh(); }, [exhausted, waitMs, refresh]);

  async function run() {
    if (running || !status || status.remaining <= 0) return;
    setRunning(true); setError(""); setMessage("");
    try {
      const result = await request<DiscoveryRunResult>("auto-discovery/run", { method: "POST" });
      setMessage(result.videosProcessed === 0
        ? "Yeni video bulunamadı (bulunanların hepsi daha önce işlenmiş)."
        : `Bu tıklamada ${result.videosProcessed} video çekildi, ${result.clipsCreated} klip çıktı (${result.clipsPublished} yayında).${result.failures.length > 0 ? ` ${result.failures.length} video işlenemedi.` : ""}`);
    } catch (value) {
      setError(value instanceof Error ? value.message : "Klipler getirilemedi.");
    } finally {
      setRunning(false);
      await refresh();
    }
  }

  const blocked = !status || status.remaining <= 0;
  return <div className="discovery-run">
    <button type="button" className="button" onClick={run} disabled={running || blocked} aria-describedby="discovery-run-info">
      {running ? "Klipler getiriliyor…" : "▶ Klip getir"}
    </button>
    <small id="discovery-run-info" aria-live="polite">
      {!status
        ? "Durum alınamadı."
        : <>
            <b>Tıklama: {status.used}/{status.allowedPerWindow}</b> ({status.windowHours} saatte, her tıklamada en çok {status.videosPerClick} video)
            {exhausted && waitMs > 0 && <><br/>Tekrar tıklamaya <b>{formatWait(waitMs)}</b> var</>}
          </>}
    </small>
    {status && status.used > 0 && <small>Son {status.windowHours} saatte: {status.videosInWindow} video çekildi, {status.clipsInWindow} klip çıktı ({status.publishedInWindow} yayında)</small>}
    {message && <small className="discovery-run-ok" role="status">{message}</small>}
    {error && <small className="discovery-run-error" role="alert">{error}</small>}
  </div>;
}
