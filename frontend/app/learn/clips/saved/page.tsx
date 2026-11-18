import { getSavedClips } from "@/lib/server-api";
import { getSession } from "@/lib/session";
import SavedClipsView from "./saved-clips-view";

export const instant = false;

export default async function SavedClipsPage() {
  const { token } = await getSession();
  const clips = await getSavedClips(token);

  return <section className="learn-panel saved-clips-page">
    {!clips && <div className="learn-alert" role="status">Kayıtlı klipler API’den alınamadı. Oturumunu ve API bağlantısını kontrol et.</div>}
    {clips && <SavedClipsView initialClips={clips}/>}
  </section>;
}
