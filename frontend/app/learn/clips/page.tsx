import { getLearnerClips } from "@/lib/server-api";
import { getSession } from "@/lib/session";
import ClipPractice from "./clip-practice";

export const instant = false;

export default async function LearnClipsPage() {
  const { token } = await getSession();
  const clips = await getLearnerClips(token, 20);

  return <section className="cinema-page" aria-label="Shadowing">
    {!clips && <div className="cinema-alert" role="status">Shadowing klipleri API’den alınamadı. Oturumunu ve API bağlantısını kontrol et.</div>}
    {clips && <ClipPractice initialClips={clips}/>}
  </section>;
}
