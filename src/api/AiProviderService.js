import { paraphraseText as geminiParafrase } from "./GeminiService";

/**
 * Mendapatkan API Key dari localStorage berdasarkan provider
 */
export const getApiKey = (provider) => {
  return localStorage.getItem(`gandi_apikey_${provider}`) || "";
};

/**
 * Menyimpan API Key ke localStorage
 */
export const saveApiKey = (provider, key) => {
  localStorage.setItem(`gandi_apikey_${provider}`, key);
};

/**
 * Fungsi utama untuk memanggil berbagai provider AI
 */
export const callAiProvider = async (provider, text, tone, model, language, format) => {
  // Jika Gemini, gunakan servis yang sudah sangat stabil
  if (provider === "gemini") {
    return await geminiParafrase(text, tone, model, language, format);
  }

  const apiKey = getApiKey(provider);
  if (!apiKey) {
    throw new Error(`API Key untuk ${provider.toUpperCase()} belum diatur.`);
  }

  // Siapkan Prompt yang sama persis dengan standar "Parafrase Gandi"
  const academicInstructions = `
  ATURAN SITASI AKADEMIK (WAJIB):
  1. Standar: APA Style 7th Edition.
  2. Aturan Mutlak: Gunakan HANYA NAMA BELAKANG penulis.
  3. Pembersihan Nama: Hapus gelar (Dr., Prof., dll.) dan nama depan.
  4. Format Baku:
     - Primer: (LastNameAuthor, Tahun)
     - Sekunder: (LastNameAuthorAsli, Tahun, dalam LastNameAuthor, Tahun)
     - Sekunder Berlapis: (LastNameAuthorAsli, Tahun, dalam LastNameAuthorAsli2, Tahun, dikutip oleh LastNameAuthor, Tahun)
  5. PENTING: Gunakan "&" (bukan "dan") antar nama penulis.
  6. INTEGRITAS: Dilarang keras menghilangkan sitasi.
  7. TANPA HALUSINASI (KRITIKAL): Jika teks asli TIDAK mengandung sitasi, maka DILARANG KERAS menambahkan sitasi buatan atau halusinasi baru.
  `;

  const humanizeInstructions = `
  ATURAN HUMANISASI:
  1. Burstiness & Perplexity: Variasikan panjang dan struktur kalimat secara alami.
  2. Kosakata Alami: Hindari kata AI (seperti "Berikutnya", "Sebagai kesimpulan").
  3. Aliran Organik: Transisi antar kalimat harus lancar.
  4. TANDA BACA (MUTLAK): DILARANG keras menggunakan em-dash (—). Gunakan tanda hubung (-) HANYA untuk kata ulang (cth: kupu-kupu). JANGAN hubungkan dua kata berbeda dengan tanda hubung.
  ${tone === "humanis" || tone === "akademik" ? "5. AGRESIF: Berikan gaya bahasa manusia yang unik untuk mengelabui deteksi AI." : ""}
  `;

  const formatInstructions = {
    "paragraf": "Tuliskan hasil dalam bentuk paragraf narasi.",
    "campuran": "Tuliskan hasil dengan kalimat pengantar diikuti daftar poin.",
    "poin": "Tuliskan hasil dalam bentuk daftar poin saja."
  };

  const systemPrompt = `Anda adalah asisten penulisan profesional "Parafrase Gandi". 
  Tugas Anda adalah memberikan 3 variasi parafrase dalam Bahasa ${language} dengan gaya ${tone}.
  FORMAT OUTPUT: ${format.toUpperCase()}. ${formatInstructions[format]}
  ${(tone === "akademik" || tone === "humanis") ? academicInstructions : ""}
  ${humanizeInstructions}
  PENTING: Kelola tag HTML ( <p>, <li>, dan <i> untuk istilah asing). 
  PENTING: Jika format PARAGRAF, jumlah paragraf harus sama dengan aslinya.
  KEMBALIKAN HANYA JSON VALID: {"options": [{"id": 1, "text": "..."}, {"id": 2, "text": "..."}, {"id": 3, "text": "..."}]}`;

  // Tentukan Endpoint & Payload
  let url = "";
  let payload = {};
  const isOpenAiCompatible = ["openai", "groq", "deepseek", "xai"].includes(provider);

  if (isOpenAiCompatible) {
    const endpoints = {
      "openai": "https://api.openai.com/v1/chat/completions",
      "groq": "https://api.groq.com/openai/v1/chat/completions",
      "deepseek": "https://api.deepseek.com/chat/completions",
      "xai": "https://api.x.ai/v1/chat/completions"
    };
    url = endpoints[provider];
    payload = {
      model: model,
      messages: [
        { role: "system", content: systemPrompt },
        { role: "user", content: `Parafrase teks ini:\n\n${text}` }
      ],
      response_format: { type: "json_object" }
    };
  } else if (provider === "claude") {
    url = "https://api.anthropic.com/v1/messages";
    payload = {
      model: model,
      max_tokens: 4000,
      system: systemPrompt,
      messages: [{ role: "user", content: `Parafrase teks ini:\n\n${text}` }]
    };
  }

  // Fetch
  try {
    const response = await fetch(url, {
      method: "POST",
      headers: {
        "Authorization": `Bearer ${apiKey}`,
        "Content-Type": "application/json",
        ...(provider === "claude" ? { "x-api-key": apiKey, "anthropic-version": "2023-06-01" } : {})
      },
      body: JSON.stringify(payload)
    });

    if (!response.ok) {
      const errData = await response.json();
      throw new Error(errData.error?.message || `API Error: ${response.statusText}`);
    }

    const result = await response.json();
    let content = "";

    if (isOpenAiCompatible) {
      content = result.choices[0].message.content;
    } else if (provider === "claude") {
      content = result.content[0].text;
    }

    // Parse JSON dari string content jika perlu
    try {
      return JSON.parse(content).options;
    } catch (e) {
      // Jika AI bandel keluarin teks biasa, coba extract JSON
      const jsonMatch = content.match(/\{[\s\S]*\}/);
      if (jsonMatch) return JSON.parse(jsonMatch[0]).options;
      throw new Error("Gagal memproses format JSON dari AI.");
    }
  } catch (err) {
    console.error(`Error ${provider}:`, err);
    throw err;
  }
};
