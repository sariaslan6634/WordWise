import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "WordWise — Kelimeler, yerine otursun",
  description: "İngilizce kelimeleri gerçek hayattan kısa videolar, akıllı tekrarlar ve küçük başarılarla öğren.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="tr">
      <body>{children}</body>
    </html>
  );
}
