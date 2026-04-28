using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace MCQS
{
    [QueryProperty(nameof(CategoryId), "id")]
    public partial class QuizSettingsPage : ContentPage
    {
        // ── State ─────────────────────────────────────────────────────────────
        string _categoryId = string.Empty;
        Category _category;
        int _questionCount = 10;   // default

        static string StoragePath =>
            Path.Combine(FileSystem.AppDataDirectory, "categories.json");

        // ── Query property ────────────────────────────────────────────────────
        public string CategoryId
        {
            get => _categoryId;
            set { _categoryId = value; LoadCategory(); }
        }

        // ── Constructor ───────────────────────────────────────────────────────
        public QuizSettingsPage()
        {
            InitializeComponent();
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Opacity = 0;
            await this.FadeTo(1, 300, Easing.CubicOut);
        }

        // ── Data ──────────────────────────────────────────────────────────────
        void LoadCategory()
        {
            if (!File.Exists(StoragePath)) return;
            var json = File.ReadAllText(StoragePath);
            var all = JsonSerializer.Deserialize<List<Category>>(json);
            _category = all?.FirstOrDefault(c => c.Id == _categoryId);
            if (_category == null) return;
            CategorySubtitle.Text = _category.Name;
        }

        // ── Chip selection ────────────────────────────────────────────────────
        readonly Dictionary<int, (Border chip, Label label)> _chips = new();

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            // Map chips after XAML is fully loaded
            _chips[5] = (Chip5, (Label)Chip5.Content);
            _chips[10] = (Chip10, (Label)Chip10.Content);
            _chips[15] = (Chip15, (Label)Chip15.Content);
            _chips[20] = (Chip20, (Label)Chip20.Content);
            SelectChip(10);   // default
        }

        void SelectChip(int count)
        {
            _questionCount = count;
            QuestionCountLabel.Text = count.ToString();

            foreach (var kv in _chips)
            {
                bool selected = kv.Key == count;

                kv.Value.chip.BackgroundColor = selected
                    ? Microsoft.Maui.Graphics.Color.FromArgb("#5B5BD6")   // Primary
                    : Microsoft.Maui.Graphics.Color.FromArgb("#F1F2F8");  // IconBadgeBackground

                kv.Value.label.TextColor = selected
                    ? Microsoft.Maui.Graphics.Color.FromArgb("#FFFFFF")   // White
                    : Microsoft.Maui.Graphics.Color.FromArgb("#777777");  // TextMuted

                kv.Value.label.FontAttributes = selected
                    ? FontAttributes.Bold
                    : FontAttributes.None;
            }
        }

        async void OnChipTapped(object sender, TappedEventArgs e)
        {
            if (sender is not View v) return;
            await v.ScaleTo(0.88, 70);
            await v.ScaleTo(1.00, 70);
            if (int.TryParse(e.Parameter?.ToString(), out int count))
                SelectChip(count);
        }

        // ── Navigation ────────────────────────────────────────────────────────
        async void OnBackTapped(object sender, TappedEventArgs e)
        {
            if (sender is View v) { await v.ScaleTo(0.85, 70); await v.ScaleTo(1, 70); }
            await Shell.Current.GoToAsync("..");
        }

        async void OnGenerateTapped(object sender, EventArgs e)
        {
            if (sender is View v) { await v.ScaleTo(0.96, 80); await v.ScaleTo(1, 80); }
            if (_category == null) return;

            var allQuestions = _category.AllQuestions;
            if (allQuestions.Count == 0)
            {
                await DisplayAlert("No Questions",
                    "Add and extract at least one PDF first.", "OK");
                return;
            }

            // Shuffle and pick N
            var rng = new Random();
            var shuffled = allQuestions.OrderBy(_ => rng.Next()).ToList();
            var picked = shuffled.Take(_questionCount).ToList();

            QuizSession.Questions = picked;
            QuizSession.CategoryName = _category.Name;

            // Go straight to quiz — no loading screen needed
            await Shell.Current.GoToAsync("quiz");
        }
    }
}