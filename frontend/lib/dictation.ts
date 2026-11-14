/** Yazma (dikte) alıştırması için saf yardımcılar: cümleyi kelimelere ayırma, harf harf değerlendirme ve ipucu. */

export type ExpectedWord = {
  lead: string;
  /** Kelimenin yazılışı (içindeki ' ve - dahil), baştaki/sondaki noktalama hariç. */
  core: string;
  trail: string;
  /** Kullanıcının yazması gereken harf ve rakamlar (' ve - hariç). */
  letters: string[];
  /** Özel isim gibi otomatik yazılan kelime: kullanıcı yazmaz. */
  auto: boolean;
};
export type LetterStatus = "empty" | "correct" | "hint" | "wrong" | "fixed" | "auto";
export type LetterSlot = { char: string; status: LetterStatus };

const EDGE = /^([^\p{L}\p{N}]*)(.*?)([^\p{L}\p{N}]*)$/u;
const SENTENCE_END = /[.!?…]["'”’)\]]*$/;
/** Kullanıcının, kelimenin gerçek uzunluğundan fazla kaç harf yazabileceği (0: 4 harfli kelimeye en çok 4 harf yazılır). */
export const MAX_EXTRA = 0;

const ABBREVIATION = /^(mr|mrs|ms|dr|prof|st|jr|sr|vs|mt)$/i;

const isLetter = (char: string) => /[\p{L}\p{N}]/u.test(char);
const isUpper = (char: string) => char !== char.toLowerCase() && char === char.toUpperCase();

/** Türkçe klavyede yazılan ı / İ İngilizce i olarak kabul edilir (İngilizcede noktasız ı ya da noktalı İ yoktur). */
const turkishI = (value: string) => value.replace(/[ıİ]/g, "i");

/** Harf karşılaştırması: büyük/küçük harf, aksan, kıvrık tırnak ve Türkçe ı/İ farkı yok sayılır. */
export function fold(value: string): string {
  return turkishI(value).replace(/[’‘]/g, "'").normalize("NFD").replace(/\p{M}/gu, "").toLowerCase();
}

/** Kutuya yazılan metin sabit biçime çevrilir: hep küçük harf, ı/İ → i. (Büyük/küçük harf altyazıda İngilizce kurallarıyla gösterilir.) */
export const canonTyped = (value: string) => turkishI(value).toLowerCase();

const I_FORM = /^i(?:'(?:m|ll|ve|d|s))?$/i;
/** Cümle ortasında bile hep büyük harfle yazılan sık kelimeler (gün, ay, dil, milliyet). "march/may/august" fiil/sıfat olabildiği için yok. */
const ALWAYS_CAPITAL = new Set([
  "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday",
  "january", "february", "april", "june", "july", "september", "october", "november", "december",
  "english", "american", "british", "spanish", "french", "german", "italian", "chinese", "japanese", "turkish", "russian", "arabic", "korean",
]);
const capitalize = (word: string) => word.charAt(0).toUpperCase() + word.slice(1);

/**
 * İngilizce büyük/küçük harf kuralları: "I" ve I'm/I'll/I've/I'd hep büyük; cümle başı (ilk kelime, . ! ? sonrası,
 * alıntı tırnağından sonra) büyük; gün/ay/dil adları büyük. Başka harfler değiştirilmez (TED, Jack olduğu gibi kalır).
 */
export function fixCase(text: string): string {
  let previous: { core: string; trail: string } | null = null;
  return text.replace(/[’‘]/g, "'").trim().split(/\s+/).map((token) => {
    const match = EDGE.exec(token);
    if (!match || match[2].length === 0) return token;
    const [, lead, rawCore, trail] = match;
    let core = rawCore;
    const start = previous === null
      || (/[.!?]["'”)\]]*$/.test(previous.trail) && !ABBREVIATION.test(previous.core))
      || /["“]/.test(lead);
    if (I_FORM.test(core)) core = "I" + core.slice(1).toLowerCase();
    else if (ALWAYS_CAPITAL.has(core.toLowerCase())) core = capitalize(core.toLowerCase());
    else if (start) core = capitalize(core);
    previous = { core, trail };
    return lead + core + trail;
  }).join(" ");
}

/**
 * Cümleyi kelimelere böler. Baştaki/sondaki noktalama yazılması gerekmeyen süstür; kelime içindeki ' ve - otomatik gelir.
 * Cümle başında olmayıp büyük harfle başlayan kelimeler (Jack, TED, London) özel isim sayılır ve otomatik yazılır;
 * "I", "I'm" gibi zamir biçimleri hariçtir.
 */
export function parseSentence(text: string): ExpectedWord[] {
  const words: ExpectedWord[] = [];
  for (const token of fixCase(text).split(/\s+/)) {
    const match = EDGE.exec(token);
    if (!match || match[2].length === 0) continue;
    const core = match[2];
    const letters = Array.from(core).filter(isLetter);
    if (letters.length === 0) continue;
    // Alıntı açan tırnaktan sonra gelen büyük harf (said, "Don't ...) yeni bir cümle başıdır.
    const prev = words[words.length - 1];
    const startsSentence = words.length === 0 || (SENTENCE_END.test(prev.trail) && !ABBREVIATION.test(prev.core)) || /["“']/.test(match[1]);
    const capitalized = isUpper(Array.from(core)[0]);
    const pronounI = /^I('|$)/.test(core);
    words.push({ lead: match[1], core, trail: match[3], letters, auto: capitalized && !startsSentence && !pronounI });
  }
  return words;
}

/** Kullanıcının yazması gereken (otomatik olmayan) kelime sayısı. */
export const typeableCount = (words: ExpectedWord[]) => words.filter((word) => !word.auto).length;

/**
 * Giriş kutusundaki metni kelimelere dağıtır: her yazılan kelime sıradaki otomatik olmayan kelimeye gider,
 * otomatik kelimeler hazır doldurulur. Harf/rakam dışındaki karakterler (', -, noktalama) yok sayılır.
 */
export function splitTyped(value: string, words: ExpectedWord[]): string[] {
  const parts = value.replace(/^\s+/, "").split(/\s+/);
  let next = 0;
  return words.map((word) => {
    if (word.auto) return word.letters.join("");
    const raw = (parts[next++] ?? "").replace(/[^\p{L}\p{N}]/gu, "");
    return Array.from(raw).slice(0, word.letters.length + MAX_EXTRA).join("");
  });
}

/** Yazılan kelimeleri giriş kutusu metnine çevirir (otomatik kelimeler dahil edilmez). */
export function joinTyped(words: ExpectedWord[], typed: string[]): string {
  return words.map((word, i) => (word.auto ? null : typed[i] ?? "")).filter((part): part is string => part !== null).join(" ").replace(/\s+$/, "");
}

/**
 * Giriş kutusuna yazılan metni düzenler: baştaki boşluk silinir, ardışık boşluklar teke iner, kelime sayısı ve her
 * kelimenin uzunluğu sınırlanır. Son kelime tamamlanmadan önce yazılan tek boşluk (sonraki kelimeye geçiş) korunur.
 */
export function normalizeInput(rawValue: string, words: ExpectedWord[]): string {
  const value = canonTyped(rawValue);
  const parts = value.replace(/^\s+/, "").split(/\s+/);
  const trailingSpace = parts.length > 1 && parts[parts.length - 1] === "";
  if (trailingSpace) parts.pop();
  const limit = typeableCount(words);
  const typed = splitTyped(parts.slice(0, limit).join(" "), words);
  const kept = words.map((word, i) => (word.auto ? null : typed[i])).filter((part): part is string => part !== null).slice(0, Math.min(parts.length, limit));
  const text = kept.join(" ");
  return trailingSpace && parts.length < limit && kept.length > 0 && kept[kept.length - 1] !== "" ? text + " " : text;
}

export function wordSlots(word: ExpectedWord, typed: string, hinted: Set<string>, wordIndex: number): LetterSlot[] {
  const actual = Array.from(typed);
  const slots: LetterSlot[] = [];
  let letterIndex = 0;
  for (const char of Array.from(word.core)) {
    if (!isLetter(char)) { slots.push({ char, status: "fixed" }); continue; }
    const j = letterIndex++;
    if (word.auto) slots.push({ char, status: "auto" });
    else if (j >= actual.length) slots.push({ char: "", status: "empty" });
    else if (fold(actual[j]) !== fold(char)) slots.push({ char: actual[j], status: "wrong" });
    else slots.push({ char, status: hinted.has(`${wordIndex}:${j}`) ? "hint" : "correct" }); // doğru harf, kullanıcının yazdığı değil kelimenin kendi (doğru büyük/küçük) biçimiyle gösterilir
  }
  for (let j = word.letters.length; j < actual.length; j += 1) slots.push({ char: actual[j], status: "wrong" });
  return slots;
}

export function isWordCorrect(word: ExpectedWord, typed: string): boolean {
  if (word.auto) return true;
  const actual = Array.from(typed);
  return word.letters.length === actual.length && word.letters.every((letter, j) => fold(letter) === fold(actual[j]));
}

export function isSentenceCorrect(words: ExpectedWord[], typed: string[]): boolean {
  return words.length > 0 && words.every((word, i) => isWordCorrect(word, typed[i] ?? ""));
}

/**
 * İpucu: henüz doğru olmayan ilk kelimedeki ilk hatalı/eksik harfi doğrusuyla doldurur.
 * Hiç eksik yoksa null döner.
 */
export function revealHint(words: ExpectedWord[], typed: string[]): { typed: string[]; key: string | null } | null {
  const i = words.findIndex((word, index) => !isWordCorrect(word, typed[index] ?? ""));
  if (i < 0) return null;
  const expected = words[i].letters;
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
