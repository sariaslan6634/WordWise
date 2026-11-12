const LABELS = ["A1", "A2", "B1", "B2", "C1", "C2"];

/** API seviyeyi sayı olarak ("1".."6") verir; ekranda A1..C2 olarak göster. Zaten etiketse olduğu gibi döner. */
export function cefrLabel(value: string | null | undefined): string | null {
  if (!value) return null;
  return /^[1-6]$/.test(value) ? LABELS[Number(value) - 1] : value;
}
