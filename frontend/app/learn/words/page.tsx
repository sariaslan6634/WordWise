import { getSavedWords } from "@/lib/server-api";
import { getSession } from "@/lib/session";
import SavedWordsView from "./saved-words-view";

export const instant = false;

export default async function SavedWordsPage() {
  const { token } = await getSession();
  const page = await getSavedWords(token, 100);

  return <section className="learn-panel learn-words-page">
    {!page && <><div className="learn-panel-heading"><div><span className="panel-kicker">KİŞİSEL KÜTÜPHANE</span><h1>Kelime defterim</h1></div></div>
      <div className="learn-alert" role="status">Kelime defteri API’den alınamadı. Oturumunu ve API bağlantısını kontrol et.</div></>}
    {page && <SavedWordsView words={page.items} totalCount={page.totalCount}/>}
  </section>;
}
