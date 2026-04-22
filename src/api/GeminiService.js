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

import { buildFullPrompt } from "./PromptBase";

export const paraphraseText = async (text, tone = "profesional", model = "gemini-2.5-flash", language = "Indonesia", format = "paragraf") => {
  const apiKey = getGeminiApiKey();
  if (!apiKey) {
    throw new Error("API Key Gemini belum diatur. Silakan buka panel pengaturan.");
  }

  const prompt = (tone === "custom_citation") ? text : buildFullPrompt(language, tone, format, text);

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
