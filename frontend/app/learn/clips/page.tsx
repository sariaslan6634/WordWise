import { getLearnerClips } from "@/lib/server-api";
import { getSession } from "@/lib/session";
import ClipPractice from "./clip-practice";

export const instant = false;

export default async function LearnClipsPage() {
  const { token } = await getSession();
  const clips = await getLearnerClips(token, 20);

  return <section className="cinema-page">
    <div className="cinema-heading"><span className="cinema-kicker">DİNLE VE OKU</span><h1>Klipler</h1></div>
    {!clips && <div className="cinema-alert" role="status">Klipler API’den alınamadı. Oturumunu ve API bağlantısını kontrol et.</div>}
    {clips && <ClipPractice initialClips={clips}/>}
  </section>;
}
