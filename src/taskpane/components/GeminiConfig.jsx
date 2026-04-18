import * as React from "react";
import { useState, useEffect } from "react";
import { 
  makeStyles, 
  shorthands, 
  Button, 
  Input, 
  Label, 
  Field, 
  Select, 
  tokens,
  Title2,
  Text,
  Spinner
} from "@fluentui/react-components";
import { 
  Save24Regular, 
  Sparkle24Regular, 
  Key24Regular,
  Chat24Regular
} from "@fluentui/react-icons";
import { getGeminiApiKey, saveGeminiApiKey, paraphraseText } from "../../api/GeminiService";
import { getLicenseStatus, saveLicenseKey, verifyLicense, getBrowserId } from "../../api/LicenseService";

const useStyles = makeStyles({
  container: {
    display: "flex",
    flexDirection: "column",
    ...shorthands.gap("20px"),
    ...shorthands.padding("20px"),
    backgroundColor: "#FFFFFF",
    minHeight: "100vh",
  },
  header: {
    color: "#0078D4",
    marginBottom: "10px",
  },
  section: {
    display: "flex",
    flexDirection: "column",
    ...shorthands.gap("10px"),
    ...shorthands.border("1px", "solid", "#E1E1E1"),
    ...shorthands.padding("15px"),
    ...shorthands.borderRadius("8px"),
    backgroundColor: "#F9FAFB",
    boxShadow: "0 2px 4px rgba(0,0,0,0.05)",
  },
  footer: {
    marginTop: "auto",
    textAlign: "center",
    color: "#A1A1A1",
    fontSize: "12px",
  },
  blueButton: {
    backgroundColor: "#0078D4",
    color: "#FFFFFF",
    "&:hover": {
      backgroundColor: "#005A9E",
      color: "#FFFFFF",
    },
  },
  whatsappButton: {
    backgroundColor: "#25D366",
    color: "#FFFFFF",
    fontWeight: "600",
    ...shorthands.borderRadius("50px"),
    ...shorthands.padding("10px", "20px"),
    boxShadow: "0 4px 10px rgba(37, 211, 102, 0.3)",
    transition: "all 0.2s ease-in-out",
    "&:hover": {
      backgroundColor: "#128C7E",
      color: "#FFFFFF",
      transform: "translateY(-2px)",
      boxShadow: "0 6px 15px rgba(37, 211, 102, 0.4)",
    },
  }
});

const GeminiConfig = () => {
  const styles = useStyles();
  const [apiKey, setApiKey] = useState("");
  const [tone, setTone] = useState("profesional");
  const [language, setLanguage] = useState("Indonesia");
  const [format, setFormat] = useState("paragraf");
  const [model, setModel] = useState("gemini-2.5-flash");
  const [results, setResults] = useState([]); 
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  // License State
  const [licenseKey, setLicenseKey] = useState("");
  const [isLicensed, setIsLicensed] = useState(false);
  const [verifying, setVerifying] = useState(false);
  const [deviceId, setDeviceId] = useState("");
  const [showUpdatePrompt, setShowUpdatePrompt] = useState(false);
  const APP_VERSION = "2.2.0";

  useEffect(() => {
    setApiKey(getGeminiApiKey());
    const { key, isVerified } = getLicenseStatus();
    setLicenseKey(key);
    setIsLicensed(isVerified);

    // Pastikan Device ID terbuat saat pertama kali buka
    const id = getBrowserId();
    setDeviceId(id);
  }, []);

  const handleSaveKey = () => {
    saveGeminiApiKey(apiKey);
    setStatus("API Key berhasil disimpan!");
    setTimeout(() => setStatus(""), 3000);
  };

  const handleCheckUpdate = () => {
    setStatus("Mengecek versi terbaru...");
    setTimeout(() => {
      // Karena ini web app, versi di server sebenarnya selalu yang terbaru
      // Kita beri pilihan ke user untuk me-refresh jika merasa ada yang kurang
      setShowUpdatePrompt(true);
      setStatus("");
    }, 1500);
  };

  const performUpdate = () => {
    setStatus("Sedang memperbarui...");
    setTimeout(() => {
      window.location.reload(true);
    }, 1000);
  };

  const handleVerifyLicense = async () => {
    try {
      setVerifying(true);
      setStatus("Memverifikasi lisensi...");
      const result = await verifyLicense(licenseKey);
      if (result.success) {
        saveLicenseKey(licenseKey);
        setIsLicensed(true);
        setStatus("Aktivasi sukses! Selamat menggunakan Parafrase Gandi.");
      } else {
        setStatus(`Gagal: ${result.message}`);
      }
    } catch (error) {
      console.error(error);
      setStatus(`Error: ${error.message}`);
    } finally {
      setVerifying(false);
    }
  };

  const handleParaphraseManual = async () => {
    try {
      setIsLoading(true);
      setStatus("Sedang memproses variasi parafrase...");
      setResults([]);
      
      await Word.run(async (context) => {
        const range = context.document.getSelection();
        range.load("text");
        await context.sync();

        const originalText = range.text;
        if (!originalText || originalText.trim() === "") {
          throw new Error("Pilih teks di dokumen terlebih dahulu.");
        }

        const options = await paraphraseText(originalText, tone, model, language, format);
        setResults(options);
        setStatus("3 Variasi telah siap. Pilih yang terbaik!");
      });
    } catch (error) {
       console.error(error);
      setStatus(`Error: ${error.message}`);
    } finally {
      setIsLoading(false);
    }
  };

  const handleInsertResult = async (textToInsert) => {
    try {
      if (!textToInsert) return;
      
      await Word.run(async (context) => {
        const range = context.document.getSelection();
        // Menggunakan insertHtml agar tag <i> dan list diproses dengan benar di Word
        range.insertHtml(textToInsert, "Replace");
        await context.sync();
        setStatus("Teks berhasil dimasukkan ke dokumen!");
      });
    } catch (error) {
      console.error(error);
      setStatus(`Error saat memasukkan teks: ${error.message}`);
    }
  };

  // Tampilan jika belum terlisensi
  if (!isLicensed) {
    return (
      <div className={styles.container} style={{ justifyContent: "center" }}>
        <div className={styles.header} style={{ textAlign: "center", marginBottom: "30px" }}>
          <Title2>Aktivasi Parafrase Gandi</Title2>
          <Text block>Masukkan License Key Anda untuk mulai menggunakan tool ini.</Text>
        </div>
        
        <div className={styles.section}>
          <Field label="License Key">
            <Input 
              value={licenseKey} 
              onChange={(e, data) => setLicenseKey(data.value)} 
              placeholder="Contoh: GANDI-XXXX-XXXX"
              icon={<Key24Regular />}
            />
          </Field>
          <Button 
            className={styles.blueButton} 
            onClick={handleVerifyLicense}
            disabled={verifying}
          >
            {verifying ? "Memproses..." : "Aktifkan Sekarang"}
          </Button>
        </div>

        {status && (
          <div style={{ marginTop: "20px", textAlign: "center" }}>
            <Text weight="semibold" style={{ color: (status.includes("Gagal") || status.includes("Error")) ? "#D13438" : "#107C10" }}>
              {status}
            </Text>
          </div>
        )}
        
        <div className={styles.footer} style={{ marginTop: "40px" }}>
          <Text block style={{ marginBottom: "15px" }}>Belum punya kunci? Hubungi Gandi:</Text>
          <Button 
            className={styles.whatsappButton}
            icon={<Chat24Regular />} 
            onClick={() => window.open("https://wa.me/6285887107048?text=Halo%20Gandi,%20saya%20tertarik%20membeli%20lisensi%20Parafrase%20Gandi", "_blank")}
          >
            Beli Lisensi (WA)
          </Button>
          <br/><br/>
          <Text size={100} italic>ID Perangkat Anda: {deviceId || "Sedang memuat..."}</Text>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title2>Parafrase Gandi</Title2>
        <Text size={200} block>Powered by Google Gemini</Text>
      </div>

      <div className={styles.section}>
        <Field label="Gemini API Key" hint="Dapatkan di Google AI Studio">
          <Input 
            type="password" 
            value={apiKey} 
            onChange={(e, data) => setApiKey(data.value)} 
            placeholder="Masukkan API Key Anda"
          />
        </Field>
        <Button 
          icon={<Save24Regular />} 
          onClick={handleSaveKey}
          appearance="outline"
        >
          Simpan Key
        </Button>
      </div>

      <div className={styles.section}>
        <Field label="Pilih Model AI">
          <Select value={model} onChange={(e, data) => setModel(data.value)}>
            <optgroup label="Gemini Next-Gen">
              <option value="gemini-2.5-flash">Gemini 2.5 Flash</option>
              <option value="gemini-2.5-pro">Gemini 2.5 Pro</option>
              <option value="gemini-2.5-flash-lite">Gemini 2.5 Flash Lite</option>
              <option value="gemini-2-flash">Gemini 2 Flash</option>
              <option value="gemini-2-flash-lite">Gemini 2 Flash Lite</option>
            </optgroup>
            <optgroup label="Gemini Series 3">
              <option value="gemini-3-flash">Gemini 3 Flash</option>
              <option value="gemini-3.1-flash-lite">Gemini 3.1 Flash Lite</option>
              <option value="gemini-3.1-pro">Gemini 3.1 Pro</option>
            </optgroup>
            <optgroup label="Gemma 3 (Lightweight)">
              <option value="gemma-3-1b">Gemma 3 1B</option>
              <option value="gemma-3-4b">Gemma 3 4B</option>
              <option value="gemma-3-12b">Gemma 3 12B</option>
              <option value="gemma-3-27b">Gemma 3 27B</option>
            </optgroup>
            <optgroup label="Experimental & Preview">
              <option value="deep-research-pro-preview">Deep Research Pro Preview</option>
              <option value="gemini-robotics-er-1.6-preview">Gemini Robotics ER 1.6</option>
              <option value="nano-banana">Nano Banana (Preview)</option>
            </optgroup>
            <optgroup label="Legacy & Stable">
              <option value="gemini-1.5-flash">Gemini 1.5 Flash</option>
              <option value="gemini-1.5-pro">Gemini 1.5 Pro</option>
            </optgroup>
          </Select>
        </Field>

        <div style={{ display: "flex", flexWrap: "wrap", gap: "10px" }}>
          <Field label="Gaya Bahasa" style={{ flex: "1 1 120px" }}>
            <Select value={tone} onChange={(e, data) => setTone(data.value)}>
              <option value="akademik">Akademik (Sitasi Aman)</option>
              <option value="humanis">Humanis (Anti AI)</option>
              <option value="profesional">Profesional</option>
              <option value="santai">Santai</option>
              <option value="kreatif">Kreatif</option>
              <option value="ringkas">Ringkas</option>
            </Select>
          </Field>

          <Field label="Bahasa" style={{ flex: "1 1 120px" }}>
            <Select value={language} onChange={(e, data) => setLanguage(data.value)}>
              <option value="Indonesia">Indonesia</option>
              <option value="Inggris">Inggris</option>
              <option value="Arab">Arab</option>
              <option value="Jepang">Jepang</option>
            </Select>
          </Field>

          <Field label="Format Output" style={{ flex: "100%" }}>
            <Select value={format} onChange={(e, data) => setFormat(data.value)}>
              <option value="paragraf">Paragraf Penuh (Narasi)</option>
              <option value="campuran">Paragraf + Poin (Daftar)</option>
              <option value="poin">Poin-Poin Saja (List)</option>
            </Select>
          </Field>
        </div>
        
        <Button 
          className={styles.blueButton}
          icon={isLoading ? <Spinner size="tiny" /> : <Sparkle24Regular />} 
          onClick={handleParaphraseManual}
          disabled={isLoading}
        >
          {isLoading ? "Memproses..." : "Klik untuk Parafrase"}
        </Button>
      </div>

      {showUpdatePrompt && (
        <div className={styles.section} style={{ backgroundColor: "#FFF4CE", borderColor: "#FDB913" }}>
          <Text weight="semibold">Update Tersedia!</Text>
          <Text size={200} block>Versi {APP_VERSION} sudah siap. Apakah Anda ingin memperbarui tampilan sekarang?</Text>
          <div style={{ display: "flex", gap: "10px", marginTop: "10px" }}>
            <Button size="small" appearance="primary" onClick={performUpdate}>Ya, Update</Button>
            <Button size="small" onClick={() => setShowUpdatePrompt(false)}>Nanti Saja</Button>
          </div>
        </div>
      )}

      {results && results.length > 0 && (
        <div style={{ display: "flex", flexDirection: "column", gap: "15px" }}>
          {results.map((opt) => (
            <div key={opt.id} className={styles.section} style={{ borderLeft: "4px solid #0078D4", backgroundColor: "#FDFDFD" }}>
              <Label weight="semibold">Pilihan {opt.id}:</Label>
              <div 
                style={{ 
                  padding: "8px", 
                  backgroundColor: "#F3F2F1", 
                  ...shorthands.borderRadius("4px"),
                  fontSize: "13px",
                  margin: "5px 0",
                  whiteSpace: "pre-wrap"
                }}
                dangerouslySetInnerHTML={{ __html: opt.text }}
              />
              <Button 
                className={styles.blueButton}
                onClick={() => handleInsertResult(opt.text)}
                size="small"
              >
                Gunakan Pilihan {opt.id}
              </Button>
            </div>
          ))}
        </div>
      )}

      {status && (
        <div style={{ wordBreak: "break-word", padding: "10px" }}>
          <Text align="center" weight="semibold" style={{ color: (status.startsWith("Error") || status.startsWith("Gagal")) ? "#D13438" : "#107C10" }}>
            {status}
          </Text>
        </div>
      )}

      <div className={styles.footer}>
        <Text italic>© 2026 Parafrase Gandi - v{APP_VERSION}</Text>
        <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: "10px", marginTop: "15px" }}>
          <Button 
            appearance="subtle" 
            size="small" 
            icon={<Sparkle24Regular />}
            onClick={handleCheckUpdate}
          >
            Cek Update Versi
          </Button>
          <Button 
            className={styles.whatsappButton}
            icon={<Chat24Regular />} 
            size="small"
            onClick={() => window.open("https://wa.me/6285887107048?text=Halo%20Gandi,%20saya%20butuh%20bantuan%20terkait%20Parafrase%20Gandi", "_blank")}
          >
            Hubungi Gandi (WA)
          </Button>
          <Button 
            appearance="subtle" 
            size="small" 
            onClick={() => { localStorage.removeItem("gandi_license_verified"); window.location.reload(); }}
          >
            Logout Akun
          </Button>
        </div>
      </div>
    </div>
  );
};

export default GeminiConfig;
