"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";

export default function ProfileMenu({ name, email }: { name: string; email: string }) {
  const [open, setOpen] = useState(false);
  const router = useRouter();

  async function logout() {
    await fetch("/api/auth/logout", { method: "POST" }).catch(() => undefined);
    router.replace("/");
    router.refresh();
  }

  return <div className="profile-menu">
    <button className="profile-menu-trigger" type="button" aria-label="Profil menüsünü aç" aria-haspopup="menu" aria-expanded={open} onClick={() => setOpen((value) => !value)}>
      <span className="topbar-avatar">{name.slice(0, 1).toUpperCase()}</span><span className="profile-menu-chevron">⌄</span>
    </button>
    {open && <div className="profile-menu-panel" role="menu">
      <div className="profile-menu-identity"><b>{name}</b><small>{email}</small><span>Yönetici</span></div>
      <button type="button" role="menuitem" onClick={logout}><span>↗</span> Çıkış yap</button>
    </div>}
  </div>;
}
