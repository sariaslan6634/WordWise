import Link from "next/link";
import type { ReactNode } from "react";
import { getSession } from "@/lib/session";
import AdminNav from "./admin-nav";
import ProfileMenu from "./profile-menu";
import { redirect } from "next/navigation";

export const instant = false;

export default async function AdminLayout({ children }: { children: ReactNode }) {
  const { user } = await getSession();
  if (!user) redirect("/admin-login");
  if (user.role !== "Admin") redirect("/learn");
  return <div className="admin-shell">
    <aside className="admin-sidebar">
      <Link className="brand admin-brand" href="/" aria-label="WordWise ana sayfa"><span className="brand-mark">w.</span><span>wordwise</span></Link>
      <AdminNav />
    </aside>
    <main className="admin-main"><header className="admin-topbar"><div className="breadcrumb"><Link href="/">WordWise</Link><span>/</span><b>Yönetim paneli</b></div><div className="topbar-right"><span className="admin-status"><i/> Sistem hazır</span><Link className="view-site" href="/">Vitrin sayfası <span>↗</span></Link><ProfileMenu name={user.name} email={user.email}/></div></header>{children}</main>
  </div>;
}
