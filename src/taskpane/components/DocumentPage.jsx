import React, { useState, useEffect } from "react";
import { 
  makeStyles, 
  shorthands, 
  Button, 
  Text, 
  Title3,
  Field,
  Input,
  Spinner,
  Label,
  Card,
  CardHeader,
  CardFooter
} from "@fluentui/react-components";
import { 
  DocumentArrowUp24Regular, 
  Delete24Regular, 
  ArrowDownload24Regular,
  Document24Regular
} from "@fluentui/react-icons";
import * as pdfjsLib from "pdfjs-dist";
import mammoth from "mammoth";
import { saveDocument, getAllDocuments, deleteDocument } from "../../api/StorageService";

// Set worker for pdfjs
pdfjsLib.GlobalWorkerOptions.workerSrc = `//cdnjs.cloudflare.com/ajax/libs/pdf.js/${pdfjsLib.version}/pdf.worker.min.js`;

const useStyles = makeStyles({
  container: {
    display: "flex",
    flexDirection: "column",
    ...shorthands.gap("20px"),
    ...shorthands.padding("10px"),
  },
  uploadArea: {
    background: "linear-gradient(135deg, #0078D4 0%, #005A9E 100%)",
    color: "#FFFFFF",
    ...shorthands.padding("30px"),
    ...shorthands.borderRadius("16px"),
    textAlign: "center",
    cursor: "pointer",
    boxShadow: "0 8px 30px rgba(0, 120, 212, 0.2)",
    transition: "transform 0.2s ease, box-shadow 0.2s ease",
    "&:hover": {
      transform: "translateY(-4px)",
      boxShadow: "0 12px 40px rgba(0, 120, 212, 0.3)",
    }
  },
  docList: {
    display: "flex",
    flexDirection: "column",
    ...shorthands.gap("15px"),
  },
  card: {
    width: "100%",
    ...shorthands.borderRadius("12px"),
    ...shorthands.border("none"),
    boxShadow: "0 4px 15px rgba(0,0,0,0.05)",
    transition: "all 0.2s ease",
    "&:hover": {
      transform: "scale(1.02)",
      boxShadow: "0 8px 25px rgba(0,0,0,0.08)",
    }
  },
  title: {
    color: "#1A1A1A",
    marginBottom: "5px",
  }
});

const DocumentPage = () => {
  const styles = useStyles();
  const [documents, setDocuments] = useState([]);
  const [isExtracting, setIsExtracting] = useState(false);
  const [status, setStatus] = useState("");

  useEffect(() => {
    loadDocuments();
  }, []);

  const loadDocuments = async () => {
    const docs = await getAllDocuments();
    setDocuments(docs);
  };

  const handleFileUpload = async (e) => {
    const file = e.target.files[0];
    if (!file) return;

    try {
      setIsExtracting(true);
      setStatus(`Mengekstrak teks dari ${file.name}...`);
      
      let text = "";
      const fileType = file.name.split('.').pop().toLowerCase();

      if (fileType === 'pdf') {
        text = await extractTextFromPdf(file);
      } else if (fileType === 'docx') {
        text = await extractTextFromDocx(file);
      } else {
        throw new Error("Format file tidak didukung. Gunakan PDF atau DOCX.");
      }

      const authorMatch = file.name.match(/\(([^)]+)\)/);
      const author = authorMatch ? authorMatch[1].split(',')[0].trim() : "Unknown";
      const yearMatch = file.name.match(/\b(19|20)\d{2}\b/);
      const year = yearMatch ? yearMatch[0] : new Date().getFullYear().toString();

      const newDoc = {
        title: file.name.replace(/\.[^/.]+$/, ""),
        author: author,
        year: year,
        text: text,
        type: fileType,
        category: `Koleksi ${documents.length + 1}`
      };

      await saveDocument(newDoc);
      await loadDocuments();
      setStatus("Dokumen berhasil disimpan!");
      setTimeout(() => setStatus(""), 3000);
    } catch (error) {
      console.error(error);
      setStatus(`Error: ${error.message}`);
    } finally {
      setIsExtracting(false);
    }
  };

  const extractTextFromPdf = async (file) => {
    const arrayBuffer = await file.arrayBuffer();
    const pdf = await pdfjsLib.getDocument({ data: arrayBuffer }).promise;
    let fullText = "";
    for (let i = 1; i <= pdf.numPages; i++) {
        const page = await pdf.getPage(i);
        const content = await page.getTextContent();
        const strings = content.items.map(item => item.str);
        fullText += strings.join(" ") + "\n";
    }
    return fullText;
  };

  const extractTextFromDocx = async (file) => {
    const arrayBuffer = await file.arrayBuffer();
    const result = await mammoth.extractRawText({ arrayBuffer });
    return result.value;
  };

  const handleDelete = async (id) => {
    await deleteDocument(id);
    await loadDocuments();
  };

  const downloadRIS = (doc) => {
    const risContent = `TY  - BOOK
AU  - ${doc.author}
PY  - ${doc.year}
TI  - ${doc.title}
ER  - `;
    
    const blob = new Blob([risContent], { type: "application/x-research-info-systems" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `${doc.title}.ris`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  return (
    <div className={styles.container}>
      <Title3>Manajemen Dokumen</Title3>
      
      <div className={styles.uploadArea} onClick={() => document.getElementById("fileInput").click()}>
        <DocumentArrowUp24Regular fontSize={48} />
        <Text block weight="semibold">Klik untuk Upload PDF atau DOCX</Text>
        <Text size={200}>Dokumen akan disimpan di penyimpanan lokal browser</Text>
        <input 
          id="fileInput" 
          type="file" 
          hidden 
          accept=".pdf,.docx" 
          onChange={handleFileUpload}
        />
      </div>

      {isExtracting && (
        <div style={{ textAlign: "center", padding: "10px" }}>
          <Spinner label={status} />
        </div>
      )}

      {status && !isExtracting && (
        <Text align="center" weight="semibold" style={{ color: status.startsWith("Error") ? "#D13438" : "#107C10" }}>
          {status}
        </Text>
      )}

      <div className={styles.docList}>
        <Text weight="bold">Daftar Dokumen ({documents.length})</Text>
        {documents.map((doc) => (
          <Card key={doc.id} className={styles.card}>
            <CardHeader
              image={<Document24Regular />}
              header={<Text weight="semibold">{doc.title}</Text>}
              description={
                <div style={{ display: "flex", gap: "10px" }}>
                  <Text size={200}>{doc.author}, {doc.year}</Text>
                  <Text size={200} italic>[{doc.category}]</Text>
                </div>
              }
            />
            <CardFooter>
              <Button 
                icon={<ArrowDownload24Regular />} 
                size="small" 
                onClick={() => downloadRIS(doc)}
              >
                RIS (Mendeley)
              </Button>
              <Button 
                icon={<Delete24Regular />} 
                appearance="subtle" 
                size="small"
                onClick={() => handleDelete(doc.id)}
              >
                Hapus
              </Button>
            </CardFooter>
          </Card>
        ))}
      </div>
    </div>
  );
};

export default DocumentPage;
