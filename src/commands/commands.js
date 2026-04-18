/* global Office */
import { paraphraseText } from "../api/GeminiService";

Office.onReady(() => {
  // If needed, Office.js is ready to be called.
});

/**
 * Parafrase teks yang di-select melalui klik kanan.
 * @param event {Office.AddinCommands.Event}
 */
async function paraphraseAction(event) {
  try {
    await Word.run(async (context) => {
      const range = context.document.getSelection();
      range.load("text");
      await context.sync();

      const originalText = range.text;
      if (!originalText || originalText.trim() === "") {
        console.error("Tidak ada teks yang dipilih.");
        event.completed();
        return;
      }

      // Tampilkan status loading di status bar jika memungkinkan, 
      // atau gunakan notification message.
      const result = await paraphraseText(originalText);

      range.insertText(result, "Replace");
      await context.sync();
    });
  } catch (error) {
    console.error(error);
    // Tampilkan error ke user
    // Office.context.mailbox.item?.notificationMessages.addAsync("error", { ... })
  } finally {
    event.completed();
  }
}

// Register internal function
Office.actions.associate("paraphraseAction", paraphraseAction);
