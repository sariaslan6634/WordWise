import Link from "next/link";
import type { ReactNode } from "react";
import { redirect } from "next/navigation";
import { getSession } from "@/lib/session";
import LogoutButton from "../admin/logout-button";

export const instant = false;

export default async function LearnLayout({ children }: { children: ReactNode }) {
  const { user } = await getSession();
  if (!user) redirect("/login");
  if (user.role === "Admin") redirect("/admin");

  return <div className="learn-shell">
    <header className="learn-header wrap">
      <Link className="brand" href="/"><span className="brand-mark">w.</span><span>wordwise</span></Link>
      <nav aria-label="Öğrenme menüsü"><Link href="/learn">Öğrenme alanım</Link><Link href="/learn/words">Kelime defterim</Link><Link href="/">Vitrin</Link></nav>
      <div className="learn-account"><span>{user.name}</span><LogoutButton signedIn/></div>
    </header>
    <main className="learn-content wrap">{children}</main>
  </div>;
}
