import React, { useState, useEffect } from "react";
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
  Card
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
    ...shorthands.gap("15px"),
  },
  chatArea: {
    display: "flex",
    flexDirection: "column",
    ...shorthands.gap("10px"),
    ...shorthands.padding("15px"),
    ...shorthands.border("1px", "solid", "#E1E1E1"),
    ...shorthands.borderRadius("8px"),
    backgroundColor: "#F9FAFB",
  },
  results: {
    display: "flex",
    flexDirection: "column",
    ...shorthands.gap("15px"),
  },
  optionCard: {
    ...shorthands.padding("10px"),
    borderLeft: "4px solid #0078D4",
    backgroundColor: "#FFFFFF",
  }
});

const CitationPage = () => {
  const styles = useStyles();
  const [documents, setDocuments] = useState([]);
  const [selectedDocId, setSelectedDocId] = useState("");
  const [query, setQuery] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [results, setResults] = useState([]);
  const [status, setStatus] = useState("");

  useEffect(() => {
    const load = async () => {
      const docs = await getAllDocuments();
      setDocuments(docs);
      if (docs.length > 0) setSelectedDocId(docs[0].id.toString());
    };
    load();
  }, []);

  const handleSearch = async () => {
    if (!query) return;
    const doc = documents.find(d => d.id.toString() === selectedDocId);
    if (!doc) {
      setStatus("Error: Pilih dokumen terlebih dahulu.");
      return;
    }

    try {
      setIsLoading(true);
      setStatus("Mencari dalam dokumen...");
      
      const prompt = buildCitationPrompt(query, doc.text, doc.author, doc.year);
      
      // We reuse the existing AI provider logic but override the system/user message
      // Note: This requires callAiProvider to handle custom prompts or we need a new method
      // For now, I'll assume we can pass a 'type' to callAiProvider or implement a custom one here
      
      const provider = "gemini"; // Default to gemini as per existing app trend
      const model = "gemini-2.0-flash-exp"; 
      
      // Since callAiProvider expects (provider, text, tone, model, language, format)
      // and it builds its own prompt, I'll need to adapt it.
      // I'll call the API directly or update AiProviderService.
      
      // FOR THE SAKE OF THIS DEMO/V1, I'll use a slightly hacky way to pass the prompt:
      // Use 'text' as the full prompt and a special 'tone' to signal custom prompt.
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
        <Field label="Pilih Dokumen Konteks" icon={<Person24Regular />}>
          <Select 
            value={selectedDocId} 
            onChange={(e, data) => setSelectedDocId(data.value)}
          >
            {documents.map(doc => (
              <option key={doc.id} value={doc.id}>
                ({doc.author}, {doc.year}) - {doc.title.substring(0, 30)}...
              </option>
            ))}
          </Select>
        </Field>

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
            <Label weight="semibold">Pilihan {opt.id}:</Label>
            <div 
              style={{ fontSize: "14px", marginTop: "5px" }}
              dangerouslySetInnerHTML={{ __html: opt.text }}
            />
          </Card>
        ))}
      </div>
    </div>
  );
};

export default CitationPage;
