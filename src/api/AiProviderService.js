import { buildSystemInstructions, buildUserMessage } from "./PromptBase";
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
  const systemPrompt = (tone === "custom_citation") ? "" : buildSystemInstructions(language, tone, format);
  const userMessage = (tone === "custom_citation") ? text : buildUserMessage(text);

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
        { role: "user", content: userMessage }
      ],
      response_format: { type: "json_object" }
    };
  } else if (provider === "claude") {
    url = "https://api.anthropic.com/v1/messages";
    payload = {
      model: model,
      max_tokens: 4000,
      system: systemPrompt,
      messages: [{ role: "user", content: userMessage }]
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
