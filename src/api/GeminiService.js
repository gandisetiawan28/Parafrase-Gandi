/* global localStorage */

export const getGeminiApiKey = () => {
  const newKey = localStorage.getItem("gandi_apikey_gemini");
  const oldKey = localStorage.getItem("gemini_api_key");
  
  if (!newKey && oldKey) {
    // Migrasi otomatis
    localStorage.setItem("gandi_apikey_gemini", oldKey);
    return oldKey;
  }
  return newKey || "";
};

export const saveGeminiApiKey = (key) => {
  localStorage.setItem("gandi_apikey_gemini", key);
};

export const paraphraseText = async (text, tone = "profesional", model = "gemini-2.5-flash", language = "Indonesia", format = "paragraf") => {
  const apiKey = getGeminiApiKey();
  if (!apiKey) {
    throw new Error("API Key Gemini belum diatur. Silakan buka panel pengaturan.");
  }

  const formatInstructions = {
    "paragraf": "Tuliskan hasil dalam bentuk paragraf narasi yang mengalir (seperti teks asli).",
    "campuran": "Tuliskan hasil dengan kalimat pengantar diikuti dengan daftar poin-poin (bullet points) untuk detailnya.",
    "poin": "Tuliskan hasil dalam bentuk daftar poin-poin (bullet points) saja, tanpa kalimat pengantar panjang."
  };

  const academicInstructions = `
  ATURAN SITASI AKADEMIK (WAJIB):
  1. Standar: APA Style 7th Edition.
  2. Aturan Mutlak: Gunakan HANYA NAMA BELAKANG (Last Name) penulis.
  3. Pembersihan Nama: Hapus semua gelar akademik (Dr., Prof., S.E., M.M., Ph.D., dll.) dan nama depan.
  4. Format Baku:
     - Primer: (LastNameAuthor, Tahun)
     - Sekunder (2 tingkat): (LastNameAuthorAsli, Tahun, dalam LastNameAuthor, Tahun)
     - Sekunder Berlapis (3 tingkat): (LastNameAuthorAsli, Tahun, dalam LastNameAuthorAsli2, Tahun, dikutip oleh LastNameAuthor, Tahun)
  5. PENTING (MUTLAK): Gunakan simbol "&" sebagai pemisah antar nama penulis, DILARANG menggunakan kata "dan". (Contoh: "Sari & Handoko, 2023" adalah BENAR. "Sari dan Handoko, 2023" adalah SALAH).
  6. Penempatan: Sitasi boleh diletakkan di depan kalimat (naratif) atau di belakang kalimat (parenthetical).
  7. INTEGRITAS: Dilarang keras menghilangkan sitasi dari teks asli. Pindahkan sitasi ke kalimat hasil parafrase yang sesuai.
  `;

  const humanizeInstructions = `
  ATURAN HUMANISASI (PENTING):
  1. Variasikan panjang kalimat (Burstiness): Campurkan kalimat pendek yang lugas dengan kalimat panjang yang kompleks secara alami.
  2. Variasikan struktur kalimat (Perplexity): Gunakan struktur kalimat yang beragam, hindari penggunaan pola subjek-predikat yang monoton.
  3. Kosakata Alami: Gunakan sinonim yang lebih bernuansa dan hindari kata-kata transisi yang terlalu formal atau 'AI-like' (seperti "Berikutnya", "Sebagai kesimpulan", "Terlebih lagi" di setiap awal paragraf).
  4. PENGGUNAAN TANDA BACA (KRITIKAL): DILARANG menggunakan tanda hubung panjang/em-dash (—). Gunakan tanda hubung (-) HANYA untuk kata ulang (seperti "kupu-kupu", "rata-rata", "hati-hati"). JANGAN gunakan tanda hubung untuk memisahkan antar kata/kalimat.
  ${tone === "humanis" || tone === "akademik" ? "5. AGRESIF: Berikan sentuhan gaya bahasa manusia yang unik, mungkin sedikit kurang formal namun tetap intelektual, untuk benar-benar mengelabui deteksi AI." : ""}
  `;

  const prompt = `Berikan 3 variasi parafrase untuk teks berikut dengan gaya ${tone === "akademik" ? "Akademik Formal (Standar Publikasi Jurnal)" : tone}. 
  PENTING: FORMAT OUTPUT harus berupa ${format.toUpperCase()}. ${formatInstructions[format]}
  ${(tone === "akademik" || tone === "humanis") ? academicInstructions : ""}
  PENTING: Jika format adalah PARAGRAF, pastikan HASIL memilik JUMLAH PARAGRAF yang SAMA dengan teks asli.
  PENTING: Tulis HASIL AKHIR dalam Bahasa ${language}.
  PENTING: Bungkus hasil (setiap paragraf atau poin) dalam tag HTML (gunakan <p> untuk paragraf dan <li> untuk daftar poin).
  ATURAN KHUSUS: Jika bahasa output adalah Indonesia, WAJIB mengidentifikasi kata/istilah bahasa asing (Inggris/lainnya) dan mencetaknya miring menggunakan tag HTML <i>...</i>.
  
  ${humanizeInstructions}

  Kembalikan HASIL HANYA DALAM FORMAT JSON yang valid dengan struktur: 
  {
    "options": [
      {"id": 1, "text": "Hasil dalam format HTML yang diminta"},
      {"id": 2, "text": "Hasil dalam format HTML yang diminta"},
      {"id": 3, "text": "Hasil dalam format HTML yang diminta"}
    ]
  }
  
  Teks yang akan diparafrase:
  "${text}"`;

  const response = await fetch(`https://generativelanguage.googleapis.com/v1beta/models/${model}:generateContent?key=${apiKey}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      contents: [{
        parts: [{ text: prompt }]
      }],
      generationConfig: {
        response_mime_type: "application/json"
      }
    })
  });

  if (!response.ok) {
    const errorData = await response.json();
    const errorMessage = errorData.error?.message || "Gagal menghubungi Gemini API";
    
    if (errorMessage.includes("quota") || errorMessage.includes("limit")) {
      throw new Error(`Kuota Habis atau Tidak Ada: ${model} mungkin belum diaktifkan di Google Cloud Console atau Billing Anda belum disetup.`);
    }
    
    throw new Error(errorMessage);
  }

  const data = await response.json();
  const rawText = data.candidates?.[0]?.content?.parts?.[0]?.text;
  
  if (!rawText) {
    throw new Error("Tidak ada hasil dari Gemini API");
  }

  try {
    const parsed = JSON.parse(rawText);
    if (!parsed.options || !Array.isArray(parsed.options)) {
      throw new Error("Format JSON dari AI tidak valid");
    }
    return parsed.options; // Mengembalikan array objek {id, text}
  } catch (e) {
    console.error("JSON Parsing error:", e, rawText);
    throw new Error("Gagal mengolah data dari AI. Coba lagi.");
  }
};
