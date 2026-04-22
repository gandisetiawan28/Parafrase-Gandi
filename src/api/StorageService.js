import Dexie from "dexie";

export const db = new Dexie("ParafraseGandiDB");

// Schema:
// documents: id, title, author, year, text, type (pdf/docx), category, createdAt
// citations: id, docId, query, quote, paraphrase, citationString, createdAt
db.version(1).stores({
  documents: "++id, title, author, year, category, createdAt",
  citations: "++id, docId, query, createdAt",
  settings: "id"
});

/**
 * Menyimpan dokumen baru ke IndexedDB
 */
export const saveDocument = async (doc) => {
  return await db.documents.add({
    ...doc,
    createdAt: new Date().toISOString()
  });
};

/**
 * Mengambil semua dokumen
 */
export const getAllDocuments = async () => {
  return await db.documents.toArray();
};

/**
 * Menghapus dokumen berdasarkan ID
 */
export const deleteDocument = async (id) => {
  await db.documents.delete(id);
  // Hapus sitasi terkait
  await db.citations.where("docId").equals(id).delete();
};

/**
 * Update kategori dokumen
 */
export const updateDocumentCategory = async (id, category) => {
  await db.documents.update(id, { category });
};

/**
 * Menyimpan sitasi/hasil chat
 */
export const saveCitation = async (citation) => {
  return await db.citations.add({
    ...citation,
    createdAt: new Date().toISOString()
  });
};

/**
 * Mengambil sitasi untuk dokumen tertentu
 */
export const getCitationsByDoc = async (docId) => {
  return await db.citations.where("docId").equals(docId).toArray();
};
