import { getLearnerClips } from "@/lib/server-api";
import { getSession } from "@/lib/session";
import ClipPractice from "./clip-practice";

export const instant = false;

export default async function LearnClipsPage({ searchParams }: { searchParams: Promise<{ saved?: string }> }) {
  const [{ token }, { saved }] = await Promise.all([getSession(), searchParams]);
  const savedOnly = saved === "1";
  const clips = await getLearnerClips(token, 20, savedOnly);

  return <section className="cinema-page" aria-label="Listening">
    {!clips && <div className="cinema-alert" role="status">Listening klipleri API’den alınamadı. Oturumunu ve API bağlantısını kontrol et.</div>}
    {clips && <ClipPractice initialClips={clips} initialSavedOnly={savedOnly}/>}
  </section>;
}
