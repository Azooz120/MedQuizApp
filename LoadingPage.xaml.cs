using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Graphics;

namespace MCQS
{
    [QueryProperty(nameof(CategoryId), "id")]
    [QueryProperty(nameof(PdfPath), "pdfpath")]
    public partial class LoadingPage : ContentPage
    {
        string _categoryId = string.Empty;
        string _pdfPath = string.Empty;

        public string CategoryId
        {
            get => _categoryId;
            set { _categoryId = value; }
        }

        public string PdfPath
        {
            get => _pdfPath;
            set { _pdfPath = Uri.UnescapeDataString(value); }
        }

        static readonly HttpClient _http = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        bool _running = false;

        static string StoragePath =>
            Path.Combine(FileSystem.AppDataDirectory, "categories.json");

        public LoadingPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Opacity = 0;
            await this.FadeTo(1, 300, Easing.CubicOut);

            if (!_running)
            {
                _running = true;
                StartDotAnimation();
                await RunPipeline();
            }
        }

        async void StartDotAnimation()
        {
            var dots = new[] { Dot1, Dot2, Dot3 };
            int i = 0;
            while (_running)
            {
                foreach (var d in dots) d.Opacity = 0.25;
                dots[i % 3].Opacity = 1;
                await Task.Delay(350);
                i++;
            }
        }

        double _barMaxWidth = 0;

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            if (width > 0) _barMaxWidth = width - 64;
        }

        async Task SetProgress(double pct, string emoji, string status, string sub)
        {
            StageEmoji.Text = emoji;
            StatusLabel.Text = status;
            SubStatusLabel.Text = sub;
            ProgressLabel.Text = $"{(int)(pct * 100)}%";

            double targetWidth = _barMaxWidth * pct;
            await ProgressFill.LayoutTo(
                new Rect(0, 0, Math.Max(targetWidth, 0), 8), 400, Easing.CubicOut);
        }

        void ShowError(string message)
        {
            _running = false;
            ErrorState.IsVisible = true;
            ErrorLabel.Text = message;
        }

        async Task RunPipeline()
        {
            try
            {
                // ── 1. Load category ──────────────────────────────────────────
                await SetProgress(0.05, "📂", "Loading category…", "Checking existing data");

                if (!File.Exists(StoragePath))
                    throw new Exception("No categories found.");

                var json = File.ReadAllText(StoragePath);
                var all = JsonSerializer.Deserialize<List<Category>>(json);
                var cat = all?.FirstOrDefault(c => c.Id == _categoryId);

                if (cat == null)
                    throw new Exception("Category not found.");
                if (!File.Exists(_pdfPath))
                    throw new Exception("PDF file not found.");

                // ── 2. Already extracted? ─────────────────────────────────────
                if (cat.IsExtracted(_pdfPath))
                {
                    await SetProgress(1.0, "✅", "Already extracted!",
                        $"{cat.PdfQuestions[_pdfPath].Count} questions ready");
                    await Task.Delay(800);
                    _running = false;
                    await Shell.Current.GoToAsync("..");
                    return;
                }

                // ── 3. Extract text from PDF locally ──────────────────────────
                string fileName = Path.GetFileNameWithoutExtension(_pdfPath);
                await SetProgress(0.10, "📄", "Reading PDF…", fileName);

                string rawText = await Task.Run(() => PdfExtractor.ExtractText(_pdfPath));

                if (string.IsNullOrWhiteSpace(rawText))
                    throw new Exception("Could not extract text from PDF. " +
                        "The file may be scanned or image-based.");

                // ── 4. Split into chunks ───────────────────────────────────────
                await SetProgress(0.20, "✂️", "Splitting into chunks…",
                    "Preparing for AI processing");

                var chunks = PdfExtractor.ChunkText(rawText, chunkSize: 3000, overlap: 200);

                if (chunks.Count == 0)
                    throw new Exception("No text chunks could be created.");

                // ── 5. Send each chunk to Groq ────────────────────────────────
                var allQuestions = new List<QuizQuestion>();

                for (int i = 0; i < chunks.Count; i++)
                {
                    double pct = 0.25 + (0.65 * ((double)i / chunks.Count));
                    await SetProgress(
                        pct,
                        "🧠",
                        $"Extracting chunk {i + 1} of {chunks.Count}…",
                        $"{allQuestions.Count} questions found so far"
                    );

                    var chunkQuestions = await ExtractQuestionsFromChunk(
                        chunks[i], i + 1, chunks.Count);
                    allQuestions.AddRange(chunkQuestions);

                    if (i < chunks.Count - 1)
                        await Task.Delay(1000);
                }

                // ── 6. Deduplicate ────────────────────────────────────────────
                await SetProgress(0.92, "🔍", "Removing duplicates…",
                    $"{allQuestions.Count} raw questions found");

                var unique = DeduplicateQuestions(allQuestions);

                // ── 7. Save ───────────────────────────────────────────────────
                await SetProgress(0.96, "💾", "Saving questions…",
                    $"{unique.Count} unique questions");

                cat.PdfQuestions[_pdfPath] = unique;
                cat.UpdatedAt = DateTime.Now;
                File.WriteAllText(StoragePath, JsonSerializer.Serialize(all));

                // ── 8. Done ───────────────────────────────────────────────────
                await SetProgress(1.0, "✨", "Done!",
                    $"{unique.Count} questions extracted from {fileName}");
                await Task.Delay(1000);

                _running = false;
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        async Task<List<QuizQuestion>> ExtractQuestionsFromChunk(
            string chunk, int chunkNum, int totalChunks)
        {
            var prompt = $@"You are a medical exam question extractor.

Below is a portion ({chunkNum} of {totalChunks}) of a medical question bank PDF.
The text may be messy with inconsistent formatting, numbering, and spacing.

Extract ALL multiple choice questions from this text portion.
Each question must have exactly 4 answer options.
Identify the correct answer even if marked inconsistently
(asterisk, letter prefix, 'Ans:', 'Answer:', underline notation, etc.).
Write a brief 1-2 sentence clinical explanation for the correct answer.
Fix any spelling or formatting issues.

Return ONLY a valid JSON array. No extra text, no markdown, no code fences.
If there are no complete questions in this portion, return an empty array: []

JSON schema:
[
  {{
    ""question"": ""Full question text"",
    ""options"": [""Option A text"", ""Option B text"", ""Option C text"", ""Option D text""],
    ""correctIndex"": 0,
    ""explanation"": ""Brief clinical explanation""
  }}
]

Rules:
- correctIndex is 0-based (0=first option, 1=second, 2=third, 3=fourth)
- Do NOT include letter prefixes (A, B, C, D) in the options text
- Never invent questions
- If a question is incomplete or cut off, skip it

Text to process:
{chunk}";

            var requestBody = new
            {
                model = "llama-3.1-70b-versatile",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.1,
                max_tokens = 4096,
                response_format = new { type = "json_object" }
            };

            int[] delays = { 10, 20, 40 };
            int attempt = 0;

            while (true)
            {
                var request = new HttpRequestMessage(HttpMethod.Post, Constants.GroqEndpoint);
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", Constants.GroqKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                var response = await _http.SendAsync(request);
                var respJson = await response.Content.ReadAsStringAsync();

                if ((int)response.StatusCode == 429)
                {
                    if (attempt >= delays.Length)
                        throw new Exception("Groq rate limit hit. Please wait and retry.");

                    int wait = delays[attempt++];
                    await SetProgress(0.25, "⏳",
                        $"Rate limited — waiting {wait}s…",
                        $"Attempt {attempt} of {delays.Length + 1}");
                    await Task.Delay(wait * 1000);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Groq error ({response.StatusCode}): {respJson}");

                var root = JsonNode.Parse(respJson);
                var content = root?["choices"]?[0]?["message"]?["content"]?.ToString();

                if (string.IsNullOrWhiteSpace(content))
                    return new List<QuizQuestion>();

                content = content.Trim();
                if (content.StartsWith("```"))
                    content = content.Substring(content.IndexOf('\n') + 1);
                if (content.EndsWith("```"))
                    content = content.Substring(0, content.LastIndexOf("```"));
                content = content.Trim();

                if (content.StartsWith("{"))
                {
                    var wrapper = JsonNode.Parse(content);
                    var inner = wrapper?["questions"]
                               ?? wrapper?["data"]
                               ?? wrapper?["results"];
                    if (inner != null) content = inner.ToJsonString();
                }

                if (content == "[]") return new List<QuizQuestion>();

                try
                {
                    var parsed = JsonSerializer.Deserialize<List<GroqQuestion>>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (parsed == null) return new List<QuizQuestion>();

                    return parsed
                        .Where(q =>
                            !string.IsNullOrWhiteSpace(q.Question) &&
                            q.Options != null &&
                            q.Options.Length == 4 &&
                            q.CorrectIndex >= 0 &&
                            q.CorrectIndex <= 3)
                        .Select(q => new QuizQuestion
                        {
                            Question = q.Question.Trim(),
                            Options = q.Options.Select(o => o.Trim()).ToArray(),
                            CorrectIndex = q.CorrectIndex,
                            Explanation = q.Explanation?.Trim() ?? string.Empty
                        })
                        .ToList();
                }
                catch
                {
                    return new List<QuizQuestion>();
                }
            }
        }

        List<QuizQuestion> DeduplicateQuestions(List<QuizQuestion> questions)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var unique = new List<QuizQuestion>();

            foreach (var q in questions)
            {
                var normalized = q.Question
                    .ToLowerInvariant()
                    .Replace("  ", " ")
                    .Trim();

                if (seen.Add(normalized))
                    unique.Add(q);
            }

            return unique;
        }

        async void OnRetryTapped(object sender, EventArgs e)
        {
            ErrorState.IsVisible = false;
            _running = true;
            StartDotAnimation();
            await SetProgress(0, "📄", "Reading PDF…", "Retrying…");
            await RunPipeline();
        }

        class GroqQuestion
        {
            public string Question { get; set; } = string.Empty;
            public string[] Options { get; set; } = new string[4];
            public int CorrectIndex { get; set; }
            public string Explanation { get; set; } = string.Empty;
        }
    }
}