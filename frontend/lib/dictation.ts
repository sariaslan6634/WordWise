/** Yazma (dikte) alıştırması için saf yardımcılar: cümleyi kelimelere ayırma, harf harf değerlendirme ve ipucu. */

export type ExpectedWord = { lead: string; core: string; trail: string };
export type LetterStatus = "empty" | "correct" | "hint" | "wrong";
export type LetterSlot = { char: string; status: LetterStatus };

const EDGE = /^([^\p{L}\p{N}]*)(.*?)([^\p{L}\p{N}]*)$/u;
/** Kullanıcının, kelimenin gerçek uzunluğundan fazla kaç harf yazabileceği (kırmızı fazlalık olarak gösterilir). */
export const MAX_EXTRA = 2;

/** Harf karşılaştırması: büyük/küçük harf, aksan ve kıvrık tırnak farkı yok sayılır. */
export function fold(value: string): string {
  return value.replace(/[’‘]/g, "'").normalize("NFD").replace(/\p{M}/gu, "").toLowerCase();
}

/** Cümleyi kelimelere böler; baştaki/sondaki noktalama (".", ",", "?"...) yazılması gerekmeyen süs olarak ayrılır. */
export function parseSentence(text: string): ExpectedWord[] {
  const words: ExpectedWord[] = [];
  for (const token of text.replace(/[’‘]/g, "'").trim().split(/\s+/)) {
    const match = EDGE.exec(token);
    if (!match || match[2].length === 0) continue;
    words.push({ lead: match[1], core: match[2], trail: match[3] });
  }
  return words;
}

/** Giriş kutusundaki metni kelime kelime ayırır (boşluk = sonraki kelime). */
export function splitTyped(value: string, count: number, cores: string[]): string[] {
  const parts = value.replace(/^\s+/, "").split(/\s+/);
  const result: string[] = [];
  for (let i = 0; i < count; i += 1) {
    const raw = parts[i] ?? "";
    result.push(Array.from(raw).slice(0, Array.from(cores[i] ?? "").length + MAX_EXTRA).join(""));
  }
  return result;
}

/**
 * Giriş kutusuna yazılan metni düzenler: baştaki boşluk silinir, ardışık boşluklar teke iner, kelime sayısı ve her
 * kelimenin uzunluğu sınırlanır. Son kelime tamamlanmadan önce yazılan tek boşluk (sonraki kelimeye geçiş) korunur.
 */
export function normalizeInput(value: string, cores: string[]): string {
  const parts = value.replace(/^\s+/, "").split(/\s+/);
  const trailingSpace = parts.length > 1 && parts[parts.length - 1] === "";
  if (trailingSpace) parts.pop();
  const kept = splitTyped(parts.join(" "), cores.length, cores).filter((_, i) => i < parts.length);
  const text = kept.join(" ");
  return trailingSpace && parts.length < cores.length && kept[kept.length - 1] !== "" ? text + " " : text;
}

export function joinTyped(typed: string[]): string {
  return typed.join(" ").replace(/\s+$/, "");
}

export function wordSlots(core: string, typed: string, hinted: Set<string>, wordIndex: number): LetterSlot[] {
  const expected = Array.from(core);
  const actual = Array.from(typed);
  const slots: LetterSlot[] = expected.map((letter, j) => {
    if (j >= actual.length) return { char: "", status: "empty" as const };
    if (fold(actual[j]) !== fold(letter)) return { char: actual[j], status: "wrong" as const };
    return { char: actual[j], status: hinted.has(`${wordIndex}:${j}`) ? ("hint" as const) : ("correct" as const) };
  });
  for (let j = expected.length; j < actual.length; j += 1) slots.push({ char: actual[j], status: "wrong" });
  return slots;
}

export function isWordCorrect(core: string, typed: string): boolean {
  const expected = Array.from(core);
  const actual = Array.from(typed);
  return expected.length === actual.length && expected.every((letter, j) => fold(letter) === fold(actual[j]));
}

export function isSentenceCorrect(words: ExpectedWord[], typed: string[]): boolean {
  return words.length > 0 && words.every((word, i) => isWordCorrect(word.core, typed[i] ?? ""));
}

/**
 * İpucu: henüz doğru olmayan ilk kelimedeki ilk hatalı/eksik harfi doğrusuyla doldurur.
 * Hiç eksik yoksa null döner.
 */
export function revealHint(words: ExpectedWord[], typed: string[]): { typed: string[]; key: string | null } | null {
  const i = words.findIndex((word, index) => !isWordCorrect(word.core, typed[index] ?? ""));
  if (i < 0) return null;
  const expected = Array.from(words[i].core);
  const actual = Array.from(typed[i] ?? "");
  let j = 0;
  while (j < expected.length && j < actual.length && fold(actual[j]) === fold(expected[j])) j += 1;
  const next = [...typed];
  if (j >= expected.length) {
    next[i] = expected.join(""); // doğru harflerden sonra fazlalık yazılmış: fazlalığı sil
    return { typed: next, key: null };
  }
  actual[j] = expected[j];
  next[i] = actual.join("");
  return { typed: next, key: `${i}:${j}` };
}
