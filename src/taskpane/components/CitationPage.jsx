import * as React from "react";
import { useState, useEffect } from "react";
import { 
  makeStyles, 
  shorthands, 
  Button, 
  Text, 
  Title3,
  Field,
  Select,
  Textarea,
  Spinner,
  Label,
  Card,
  CardHeader,
  CardFooter
} from "@fluentui/react-components";
import { 
  Search24Regular, 
  Sparkle24Regular,
  Person24Regular
} from "@fluentui/react-icons";
import { getAllDocuments } from "../../api/StorageService";
import { callAiProvider, getApiKey } from "../../api/AiProviderService";
import { buildCitationPrompt } from "../../api/PromptBase";

const useStyles = makeStyles({
  container: {
    display: "flex",
    flexDirection: "column",
    ...shorthands.gap("20px"),
    ...shorthands.padding("10px"),
  },
  chatArea: {
    display: "flex",
    flexDirection: "column",
    ...shorthands.gap("15px"),
    ...shorthands.padding("20px"),
    ...shorthands.border("1px", "solid", "rgba(225, 225, 225, 0.5)"),
    ...shorthands.borderRadius("16px"),
    backgroundColor: "#FFFFFF",
    boxShadow: "0 4px 20px rgba(0,0,0,0.04)",
  },
  results: {
    display: "flex",
    flexDirection: "column",
    ...shorthands.gap("15px"),
  },
  optionCard: {
    ...shorthands.padding("15px"),
    borderLeft: "5px solid #0078D4",
    backgroundColor: "#FFFFFF",
    ...shorthands.borderRadius("8px"),
    boxShadow: "0 2px 12px rgba(0,0,0,0.03)",
    transition: "transform 0.2s ease",
    "&:hover": {
      transform: "translateX(5px)",
    }
  }
});

const CitationPage = () => {
  const styles = useStyles();
  const [documents, setDocuments] = useState([]);
  const [categories, setCategories] = useState([]);
  const [selectedCategory, setSelectedCategory] = useState("SEMUA");
  const [selectedDocId, setSelectedDocId] = useState("");
  const [query, setQuery] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [results, setResults] = useState([]);
  const [status, setStatus] = useState("");
  const [filteredDocs, setFilteredDocs] = useState([]);

  useEffect(() => {
    const load = async () => {
      const docs = await getAllDocuments();
      setDocuments(docs);
      
      // Ekstrak kategori unik
      const uniqueCats = ["SEMUA", ...new Set(docs.map(d => d.category).filter(Boolean))];
      setCategories(uniqueCats);
      
      if (docs.length > 0) {
        setFilteredDocs(docs);
        setSelectedDocId(docs[0].id.toString());
      }
    };
    load();
  }, []);

  // Update filtered docs when category changes
  useEffect(() => {
    const filtered = selectedCategory === "SEMUA" 
      ? documents 
      : documents.filter(d => d.category === selectedCategory);
    
    setFilteredDocs(filtered);
    if (filtered.length > 0) {
      setSelectedDocId(filtered[0].id.toString());
    } else {
      setSelectedDocId("");
    }
  }, [selectedCategory, documents]);

  const handleSearch = async () => {
    if (!query || !selectedDocId) return;
    const doc = documents.find(d => d.id.toString() === selectedDocId);
    if (!doc) {
      setStatus("Error: Pilih dokumen terlebih dahulu.");
      return;
    }

    try {
      setIsLoading(true);
      setStatus("Mencari dalam dokumen...");
      
      const prompt = buildCitationPrompt(query, doc.text, doc.author, doc.year);
      
      const provider = "gemini"; 
      const model = "gemini-2.0-flash-exp"; 
      
      const options = await callAiProvider(provider, prompt, "custom_citation", model, "Indonesia", "paragraf");
      
      setResults(options);
      setStatus("Pencarian selesai!");
    } catch (error) {
      console.error(error);
      setStatus(`Error: ${error.message}`);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className={styles.container}>
      <Title3>Cari Sitasi & Definisi</Title3>
      
      <div className={styles.chatArea}>
        <div style={{ display: "flex", gap: "10px", flexDirection: "column" }}>
          <Field label="Pilih Kategori (Variabel Penelitian)">
            <Select 
              value={selectedCategory} 
              onChange={(e, data) => setSelectedCategory(data.value)}
            >
              {categories.map(cat => (
                <option key={cat} value={cat}>{cat}</option>
              ))}
            </Select>
          </Field>

          <Field label="Pilih Peneliti (Author, Year)" icon={<Person24Regular />}>
            <Select 
              value={selectedDocId} 
              onChange={(e, data) => setSelectedDocId(data.value)}
              disabled={filteredDocs.length === 0}
            >
              {filteredDocs.length === 0 && <option value="">Tidak ada dokumen</option>}
              {filteredDocs.map(doc => (
                <option key={doc.id} value={doc.id}>
                  ({doc.author}, {doc.year}) - {doc.title.substring(0, 25)}...
                </option>
              ))}
            </Select>
          </Field>
        </div>

        <Field label="Apa yang ingin Anda cari?">
          <Textarea 
            placeholder="Contoh: tolong carikan pengertian harga" 
            value={query}
            onChange={(e, data) => setQuery(data.value)}
            rows={3}
          />
        </Field>

        <Button 
          appearance="primary" 
          icon={isLoading ? <Spinner size="tiny" /> : <Search24Regular />}
          onClick={handleSearch}
          disabled={isLoading || documents.length === 0}
        >
          {isLoading ? "Memproses..." : "Cari Sekarang"}
        </Button>
      </div>

      {status && (
        <Text align="center" size={200} style={{ color: status.startsWith("Error") ? "#D13438" : "#666" }}>
          {status}
        </Text>
      )}

      <div className={styles.results}>
        {results.map((opt) => (
          <Card key={opt.id} className={styles.optionCard}>
            <CardHeader
              header={<Text weight="bold">Pilihan {opt.id}</Text>}
              description={<Text size={100}>Berdasarkan konteks dokumen pilihan</Text>}
            />
            <div 
              style={{ fontSize: "14px", marginTop: "10px", lineHeight: "1.6" }}
              dangerouslySetInnerHTML={{ __html: opt.text }}
            />
            <CardFooter>
              <Button 
                appearance="primary" 
                size="small" 
                icon={<Sparkle24Regular />}
                onClick={() => handleInsert(opt.text)}
              >
                Gunakan Pilihan Ini
              </Button>
            </CardFooter>
          </Card>
        ))}
      </div>
    </div>
  );
};

const handleInsert = async (htmlText) => {
  try {
    await Word.run(async (context) => {
      const range = context.document.getSelection();
      range.insertHtml(htmlText, "Replace");
      await context.sync();
    });
  } catch (error) {
    console.error("Gagal memasukkan teks:", error);
  }
};

export default CitationPage;
