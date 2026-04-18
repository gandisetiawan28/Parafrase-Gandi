/* global localStorage */

export const getGeminiApiKey = () => {
  return localStorage.getItem("gemini_api_key") || "";
};

export const saveGeminiApiKey = (key) => {
  localStorage.setItem("gemini_api_key", key);
};

export const paraphraseText = async (text, tone = "profesional", model = "gemini-2.5-flash", language = "Indonesia") => {
  const apiKey = getGeminiApiKey();
  if (!apiKey) {
    throw new Error("API Key Gemini belum diatur. Silakan buka panel pengaturan.");
  }

  const prompt = `Berikan 3 variasi parafrase untuk teks berikut dengan nada ${tone}. 
  PENTING: Pastikan HASIL memilik JUMLAH PARAGRAF yang SAMA dengan teks asli.
  PENTING: Tulis HASIL AKHIR dalam Bahasa ${language}.
  PENTING: Bungkus SETIAP PARAGRAF dalam tag HTML <p>...</p>. JANGAN menggabungkan paragraf asli menjadi satu.
  ATURAN KHUSUS: Jika bahasa output adalah Indonesia, WAJIB mengidentifikasi kata/istilah bahasa asing (Inggris/lainnya) dan mencetaknya miring menggunakan tag HTML <i>...</i>.
  
  Kembalikan HASIL HANYA DALAM FORMAT JSON yang valid dengan struktur: 
  {
    "options": [
      {"id": 1, "text": "<p>Hasil parafrase paragraf 1...</p><p>Hasil parafrase paragraf 2...</p>"},
      {"id": 2, "text": "<p>Hasil parafrase paragraf 1...</p><p>Hasil parafrase paragraf 2...</p>"},
      {"id": 3, "text": "<p>Hasil parafrase paragraf 1...</p><p>Hasil parafrase paragraf 2...</p>}
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
