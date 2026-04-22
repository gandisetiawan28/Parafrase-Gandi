/**
 * PromptBase.js
 * Konsolidasi instruksi prompt agar 100% konsisten di semua provider AI (Gemini, Claude, GPT, dll.)
 */

export const citationSearchInstructions = `
TUGAS KHUSUS: PENCARIAN SITASI & DEFINISI
Anda akan diberikan teks dari sebuah dokumen sebagai konteks. Tugas Anda adalah mencari pengertian atau informasi yang diminta user berdasarkan teks tersebut.
Berikan 3 PILIHAN (opsi) jawaban. Setiap opsi WAJIB memiliki format:
1. **Kutipan Verbatim**: Tampilkan teks asli langsung dari dokumen (pindahkan apa adanya). Bungkus dalam tag <i>"...utama..."</i>.
2. **Hasil Parafrase**: Berikan penjelasan yang sudah diparafrase dengan gaya bahasa yang baik.
3. **Sitasi**: Berikan sitasi in-text sesuai format APA 7th Edition (Primer atau Sekunder).

Aturan Sitasi APA 7th (Primer/Sekunder):
- Primer: (Penulis, Tahun)
- Sekunder: (PenulisAsli, Tahun, dalam PenulisKonteks, Tahun)
Pilih yang paling sesuai berdasarkan informasi dalam teks konteks.

Kembalikan HASIL HANYA DALAM FORMAT JSON yang valid dengan struktur: 
{
  "options": [
    {"id": 1, "text": "<b>Kutipan Verbatim:</b><br/><i>\"...\"</i><br/><br/><b>Hasil Parafrase:</b><br/>...<br/><br/><b>Sitasi:</b> (Author, Year)"},
    {"id": 2, "text": "..."},
    {"id": 3, "text": "..."}
  ]
}
`;

export const integrityInstructions = `
ATURAN INTEGRITAS & ANTI-HALUSINASI (MUTLAK & KRITIKAL):
1. JANGAN HILANGKAN SITASI: Jika teks asli memiliki sitasi, wajib tetap ada di hasil parafrase (pindahkan ke kalimat yang sesuai).
2. JANGAN ADA SITASI FIKTIF (SANGAT PENTING): Jika teks asli TIDAK mengandung sitasi, maka DILARANG KERAS menambahkan sitasi buatan atau halusinasi baru (seperti mengarang nama orang dan tahun). Hasil harus murni hanya parafrase teks tanpa referensi tambahan apapun. Walaupun dipilih gaya 'Akademik', dilarang keras mengarang sitasi jika sumbernya bersih.
`;

export const academicInstructions = `
ATURAN SITASI AKADEMIK (WAJIB):
1. Standar: APA Style 7th Edition.
2. Aturan Mutlak: Gunakan HANYA NAMA BELAKANG (Last Name) penulis.
3. Pembersihan Nama: Hapus semua gelar akademik dan nama depan.
4. Format Baku:
   - Primer: (LastNameAuthor, Tahun)
   - Sekunder (2 tingkat): (LastNameAuthorAsli, Tahun, dalam LastNameAuthor, Tahun)
5. PENTING (MUTLAK): Gunakan simbol "&" sebagai pemisah antar nama penulis, DILARANG menggunakan kata "dan". (Contoh: "Sari & Handoko, 2023" adalah BENAR).
6. Penempatan: Sitasi boleh diletakkan di depan kalimat (naratif) atau di belakang kalimat (parenthetical).
7. CATATAN: Aturan sitasi ini HANYA berlaku jika terdapat sitasi pada teks asli. Jika tidak ada, abaikan aturan ini secara total.
`;

export const humanizeInstructions = (tone) => `
ATURAN HUMANISASI (PENTING):
1. Variasikan panjang kalimat (Burstiness): Campurkan kalimat pendek yang lugas dengan kalimat panjang yang kompleks secara alami.
2. Variasikan struktur kalimat (Perplexity): Gunakan struktur kalimat yang beragam, hindari pola yang monoton.
3. Kosakata Alami: Gunakan sinonim yang bernuansa dan hindari kata transisi kaku khas AI.
4. PENGGUNAAN TANDA BACA (KRITIKAL): DILARANG menggunakan em-dash (—). Gunakan tanda hubung (-) HANYA untuk kata ulang (seperti "rata-rata").
${tone === "humanis" || tone === "akademik" ? "5. AGRESIF: Berikan sentuhan gaya bahasa manusia yang unik agar benar-benar lolos deteksi AI." : ""}
`;

export const formatInstructions = {
  "paragraf": "Tuliskan hasil dalam bentuk paragraf narasi yang mengalir (seperti teks asli).",
  "campuran": "Tuliskan hasil dengan kalimat pengantar singkat diikuti dengan daftar poin-poin (bullet points) untuk detailnya. Bungkus daftar poin dalam tag <ul><li>...</li></ul>.",
  "poin": "Tuliskan hasil HANYA dalam bentuk daftar poin-poin (bullet points) yang padat dan informatif. Analisis teks asli dan pecah menjadi 3 hingga 5 poin inti. DILARANG menggunakan kalimat pengantar. Wajib masukkan hasil ke dalam struktur HTML <ul><li>...</li></ul>."
};

/**
 * Memmbuat instruksi sistem yang digunakan bersama
 */
export const buildSystemInstructions = (language, tone, format) => {
  return `Anda adalah asisten penulisan profesional "Parafrase Gandi".
Tugas Anda adalah memberikan 3 variasi parafrase dalam Bahasa ${language} dengan gaya ${tone}.
PENTING: FORMAT OUTPUT harus berupa ${format.toUpperCase()}. ${formatInstructions[format]}

${integrityInstructions}

${(tone === "akademik" || tone === "humanis") ? academicInstructions : ""}

${humanizeInstructions(tone)}

PENTING: Jika format adalah PARAGRAF, pastikan HASIL memilik JUMLAH PARAGRAF yang SAMA dengan teks asli.
PENTING: Tulis HASIL AKHIR dalam Bahasa ${language}.
PENTING: Bungkus hasil dalam tag HTML yang sesuai (<p> untuk paragraf tunggal, atau <ul><li>...</li></ul> untuk daftar poin). Pastikan setiap opsi memiliki struktur HTML yang lengkap dan valid.
ATURAN FORMATTING (ESTETIKA):
1. PENEKANAN: Gunakan tag <b>...</b> (bold) untuk penekanan kata/istilah penting. DILARANG menggunakan miring (italic) untuk penekanan.
2. EFISIENSI BOLD: Gunakan bold secara bijak dan tidak terlalu sering (hanya pada poin inti).
3. BAHASA ASING (ITALIC): Gunakan tag <i>...</i> (italic) KHUSUS untuk kata/istilah bahasa asing (contoh: kata Inggris jika bahasa output Indonesia, atau sebaliknya). JANGAN gunakan italic untuk tujuan lain selain identifikasi bahasa asing.

Kembalikan HASIL HANYA DALAM FORMAT JSON yang valid dengan struktur: 
{
  "options": [
    {"id": 1, "text": "Hasil dalam format HTML yang diminta"},
    {"id": 2, "text": "Hasil dalam format HTML yang diminta"},
    {"id": 3, "text": "Hasil dalam format HTML yang diminta"}
  ]
}`;
};

/**
 * Memmbuat pesan user yang menyertakan teks target
 */
export const buildUserMessage = (text) => {
  return `Teks yang akan diparafrase:\n"${text}"`;
};

/**
 * Helper untuk model yang hanya mendukung prompt tunggal (seperti Gemini lama)
 */
export const buildFullPrompt = (language, tone, format, text) => {
  return `${buildSystemInstructions(language, tone, format)}\n\n${buildUserMessage(text)}`;
};

/**
 * Membuat prompt untuk pencarian sitasi dalam dokumen
 */
export const buildCitationPrompt = (query, contextText, author, year) => {
  return `${citationSearchInstructions}
  
  DOKUMEN KONTEKS:
  Penulis: ${author}
  Tahun: ${year}
  Isi Teks: ${contextText.substring(0, 30000)} // Limit to reasonable length
  
  PERTANYAAN USER:
  "${query}"`;
};
