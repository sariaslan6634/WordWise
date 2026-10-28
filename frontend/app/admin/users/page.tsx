import { getAdminUsers } from "@/lib/server-api";
import { connection } from "next/server";
import { getSession } from "@/lib/session";

const date = new Intl.DateTimeFormat("tr-TR", { dateStyle: "medium" });

export const instant = false;

export default async function AdminUsersPage() {
  await connection();
  const { token } = await getSession();
  const users = await getAdminUsers(token);

  return <div className="admin-content">
    <div className="admin-page-heading"><div><div className="eyebrow"><span className="eyebrow-dot"/> HESAP YÖNETİMİ</div><h1>Kullanıcılar <span>✳</span></h1><p>Üyelikleri ve öğrenme ilerlemesini görüntüle.</p></div><span className={`data-pill ${users ? "live" : "preview"}`}><i/>{users ? "Canlı API verisi" : "API bağlantısı yok"}</span></div>
    {!users && <div className="preview-banner warning"><span>ⓘ</span><div><b>Kullanıcı listesi alınamadı.</b><small>API’nin çalıştığını ve admin oturumunun geçerli olduğunu kontrol et.</small></div></div>}
    <div className="word-table panel admin-user-table"><div className="word-table-head"><span>KULLANICI</span><span>E-POSTA</span><span>ROL</span><span>TOPLAM XP</span><span>ÜYELİK</span><span>SON AKTİVİTE</span></div>
      {users?.items.map((user, index) => <div className="word-table-row" key={user.id}><div className="word-title"><span className={`word-initial hue-${index % 4}`}>{user.name.slice(0, 1).toUpperCase()}</span><span><b>{user.name}</b><small>{user.role === "Admin" ? "Yönetici" : "Üye"}</small></span></div><p>{user.email}</p><span className="category-tag">{user.role === "Admin" ? "Admin" : "Üye"}</span><span className="cefr-tag">{user.totalXp.toLocaleString("tr-TR")} XP</span><span className="category-tag">{date.format(new Date(user.memberSince))}</span><span className="cefr-tag">{user.lastActiveAt ? date.format(new Date(user.lastActiveAt)) : "Henüz aktif değil"}</span></div>)}
      {users?.items.length === 0 && <p className="learn-empty">Kullanıcı bulunamadı.</p>}
    </div>
    {users && <div className="table-pagination"><span>Toplam <b>{users.totalCount}</b> kullanıcı · Sayfa {users.page} / {users.totalPages}</span></div>}
  </div>;
}
