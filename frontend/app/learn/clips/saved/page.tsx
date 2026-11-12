import Link from "next/link";
import { getSavedClips } from "@/lib/server-api";
import { getSession } from "@/lib/session";
import SavedClipsView from "./saved-clips-view";

export const instant = false;

export default async function SavedClipsPage() {
  const { token } = await getSession();
  const clips = await getSavedClips(token);

  return <section className="learn-panel saved-clips-page">
    <div className="learn-panel-heading">
      <div><span className="panel-kicker">LISTENING</span><h1>Kayıtlı klipler</h1><p>Kaydettiğin klipler; en son kaydettiğin en üstte.</p></div>
      <div className="learn-heading-side">
        {clips && <span className="learn-count">{clips.length} klip</span>}
        <Link className="button" href="/learn/clips?saved=1">▶ Rastgele oynat</Link>
      </div>
    </div>
    {!clips && <div className="learn-alert" role="status">Kayıtlı klipler API’den alınamadı. Oturumunu ve API bağlantısını kontrol et.</div>}
    {clips && <SavedClipsView initialClips={clips}/>}
  </section>;
}
