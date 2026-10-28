"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const menu = [
  { href: "/admin", label: "Genel bakış", icon: "◫" },
  { href: "/admin/words", label: "Kelime kütüphanesi", icon: "Aa" },
  { href: "/admin/word-candidates", label: "Kelime adayları", icon: "＋" },
  { href: "/admin/videos", label: "Video adayları", icon: "▷" },
  { href: "/admin/users", label: "Kullanıcılar", icon: "♧" },
];

export default function AdminNav() {
  const pathname = usePathname();
  return <nav className="admin-nav" aria-label="Yönetim menüsü">{menu.map((item) => {
    const targetPath = item.href.split("#")[0];
    const active = item.label === "Genel bakış" ? pathname === "/admin" : pathname === targetPath;
    return <Link key={item.href} href={item.href} className={`admin-nav-link ${active ? "active" : ""}`}><span className="nav-icon">{item.icon}</span><span>{item.label}</span></Link>;
  })}</nav>;
}
