using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Maui.Graphics;

namespace MCQS
{
    public class Category
    {
        static readonly Color[] Palette = {
            Color.FromArgb("#6C63FF"),
            Color.FromArgb("#FF6584"),
            Color.FromArgb("#26de81"),
            Color.FromArgb("#FF9F43"),
            Color.FromArgb("#4ECDC4"),
            Color.FromArgb("#A29BFE"),
        };

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public int ColorIndex { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public List<string> PdfPaths { get; set; } = new List<string>();

        public Dictionary<string, List<QuizQuestion>> PdfQuestions { get; set; }
            = new Dictionary<string, List<QuizQuestion>>();

        [JsonIgnore]
        public List<QuizQuestion> AllQuestions =>
            PdfQuestions.Values.SelectMany(q => q).ToList();

        [JsonIgnore]
        public int TotalQuestions => AllQuestions.Count;

        [JsonIgnore]
        public int PdfCount => PdfPaths.Count;

        [JsonIgnore]
        public Color CardColor => Palette[ColorIndex % Palette.Length];

        public bool IsExtracted(string pdfPath) =>
            PdfQuestions.ContainsKey(pdfPath) &&
            PdfQuestions[pdfPath].Count > 0;
    }
}