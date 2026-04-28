using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UglyToad.PdfPig;

namespace MCQS
{
    public static class PdfExtractor
    {
        // ── Extract all text from a PDF file ──────────────────────────────────
        public static string ExtractText(string filePath)
        {
            if (!File.Exists(filePath))
                throw new Exception($"PDF file not found: {filePath}");

            var sb = new StringBuilder();

            using (var document = PdfDocument.Open(filePath))
            {
                foreach (var page in document.GetPages())
                {
                    sb.AppendLine(page.Text);
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        // ── Split text into overlapping chunks ────────────────────────────────
        public static List<string> ChunkText(
            string text,
            int chunkSize = 3000,
            int overlap = 200)
        {
            var chunks = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return chunks;

            var words = text.Split(
                new[] { ' ', '\n', '\r', '\t' },
                StringSplitOptions.RemoveEmptyEntries);

            int total = words.Length;
            int start = 0;

            while (start < total)
            {
                int end = Math.Min(start + chunkSize, total);
                chunks.Add(string.Join(" ", words, start, end - start));
                start = end - overlap;
                if (start >= total) break;
            }

            return chunks;
        }
    }
}