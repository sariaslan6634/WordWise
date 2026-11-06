import { getLearnerClips } from "@/lib/server-api";
import { getSession } from "@/lib/session";
import ClipPractice from "./clip-practice";

export const instant = false;

export default async function LearnClipsPage() {
  const { token } = await getSession();
  const clips = await getLearnerClips(token, 20);

  return <section className="learn-panel clip-practice-page">
    <div className="learn-panel-heading"><div><span className="panel-kicker">DİNLE VE OKU</span><h1>Klipler</h1><p>Videoyu izle, söylenen cümleyi kelime kelime takip et.</p></div></div>
    {!clips && <div className="learn-alert" role="status">Klipler API’den alınamadı. Oturumunu ve API bağlantısını kontrol et.</div>}
    {clips && <ClipPractice initialClips={clips}/>}
  </section>;
}
