"use client";

import { useRouter } from "next/navigation";

export default function LogoutButton({ signedIn }: { signedIn: boolean }) {
  const router = useRouter();
  async function logout() {
    if (!signedIn) { router.push("/login"); return; }
    await fetch("/api/auth/logout", { method: "POST" });
    router.push("/login");
    router.refresh();
  }
  return <button className="logout-button" onClick={logout} aria-label={signedIn ? "Çıkış yap" : "Giriş yap"}>{signedIn ? "↗" : "→"}</button>;
}
