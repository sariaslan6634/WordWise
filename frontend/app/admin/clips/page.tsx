import { connection } from "next/server";
import { getAdminClips } from "@/lib/server-api";
import { getSession } from "@/lib/session";
import ClipManager from "./clip-manager";

export const instant = false;

export default async function AdminClipsPage() {
  await connection();
  const { token } = await getSession();
  const clips = await getAdminClips(token);

  return <div className="admin-content">
    <div className="admin-page-heading"><div><div className="eyebrow"><span className="eyebrow-dot"/> KLİP AJANI</div><h1>Video klipleri <span>✳</span></h1><p>YouTube bağlantısından tek cümlelik klipler üret, kalite kontrol sonucunu incele, uygun olanları yayınla.</p></div><div className="heading-actions"><span className={`data-pill ${clips ? "live" : "preview"}`}><i/>{clips ? "Canlı API verisi" : "API bağlantısı yok"}</span></div></div>
    {!clips && <div className="preview-banner warning"><span>ⓘ</span><div><b>Klipler alınamadı.</b><small>API bağlantısını ve admin oturumunu kontrol et.</small></div></div>}
    <ClipManager initialClips={clips ?? []}/>
  </div>;
}
