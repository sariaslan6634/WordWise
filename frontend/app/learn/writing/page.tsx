import { getLearnerClips } from "@/lib/server-api";
import { getSession } from "@/lib/session";
import WritingPractice from "./writing-practice";

export const instant = false;

export default async function WritingPage() {
  const { token } = await getSession();
  const clips = await getLearnerClips(token, 30);

  return <section className="cinema-page" aria-label="Writing">
    {!clips && <div className="cinema-alert" role="status">Writing klipleri API’den alınamadı. Oturumunu ve API bağlantısını kontrol et.</div>}
    {clips && <WritingPractice initialClips={clips}/>}
  </section>;
}
