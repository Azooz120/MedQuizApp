using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;

namespace MCQS
{
    public class PdfItem
    {
        public string FilePath { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public bool IsExtracted { get; set; }

        public string FileName =>
            Path.GetFileNameWithoutExtension(FilePath);

        public string StatusLabel => IsExtracted
            ? $"✅ {QuestionCount} questions extracted"
            : "⏳ Not yet extracted";
    }

    [QueryProperty(nameof(CategoryId), "id")]
    public partial class CategoryPage : ContentPage
    {
        string _categoryId = string.Empty;
        Category _category;

        readonly ObservableCollection<PdfItem> _pdfs = new();

        static string StoragePath =>
            Path.Combine(FileSystem.AppDataDirectory, "categories.json");

        public string CategoryId
        {
            get => _categoryId;
            set { _categoryId = value; LoadCategory(); }
        }

        public CategoryPage()
        {
            InitializeComponent();
            PdfList.ItemsSource = _pdfs;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Reload every time we come back (extraction may have finished)
            LoadCategory();
            Opacity = 0;
            await this.FadeTo(1, 300, Easing.CubicOut);
        }

        void LoadCategory()
        {
            if (!File.Exists(StoragePath)) return;
            var json = File.ReadAllText(StoragePath);
            var all = JsonSerializer.Deserialize<List<Category>>(json);
            _category = all?.FirstOrDefault(c => c.Id == _categoryId);
            if (_category == null) return;

            CategoryTitle.Text = _category.Name;

            _pdfs.Clear();
            foreach (var path in _category.PdfPaths)
            {
                bool extracted = _category.IsExtracted(path);
                int qCount = extracted
                    ? _category.PdfQuestions[path].Count : 0;

                _pdfs.Add(new PdfItem
                {
                    FilePath = path,
                    IsExtracted = extracted,
                    QuestionCount = qCount
                });
            }
            RefreshUI();
        }

        void SaveCategory()
        {
            if (_category == null || !File.Exists(StoragePath)) return;
            var json = File.ReadAllText(StoragePath);
            var all = JsonSerializer.Deserialize<List<Category>>(json) ?? new List<Category>();
            var idx = all.FindIndex(c => c.Id == _category.Id);
            if (idx >= 0) all[idx] = _category;
            File.WriteAllText(StoragePath, JsonSerializer.Serialize(all));
        }

        void RefreshUI()
        {
            int pdfCount = _pdfs.Count;
            int totalQuestions = _category?.TotalQuestions ?? 0;

            StatsLabel.Text = pdfCount == 0
                ? "No PDFs"
                : $"{pdfCount} PDF{(pdfCount == 1 ? "" : "s")} · {totalQuestions} questions";

            EmptyState.IsVisible = pdfCount == 0;
            PdfScrollView.IsVisible = pdfCount > 0;

            // Can only start quiz if there are extracted questions
            StartQuizButton.IsEnabled = totalQuestions > 0;
            StartQuizButton.Text = totalQuestions > 0
                ? $"🚀  Start Quiz  ({totalQuestions} questions)"
                : "🚀  Start Quiz";
        }

        async void OnBackTapped(object sender, TappedEventArgs e)
        {
            if (sender is View v) { await v.ScaleTo(0.85, 70); await v.ScaleTo(1, 70); }
            await Shell.Current.GoToAsync("..");
        }

        async void OnAddPdfTapped(object sender, TappedEventArgs e)
        {
            if (sender is View v) { await v.ScaleTo(0.85, 70); await v.ScaleTo(1, 70); }
            if (_category == null) return;

            try
            {
                var fileType = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.iOS,     new[] { "com.adobe.pdf" } },
                        { DevicePlatform.Android, new[] { "application/pdf" } },
                        { DevicePlatform.WinUI,   new[] { ".pdf" } },
                        { DevicePlatform.macOS,   new[] { "pdf" } },
                    });

                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select a PDF",
                    FileTypes = fileType
                });
                if (result == null) return;

                string destDir = Path.Combine(FileSystem.AppDataDirectory, "pdfs", _category.Id);
                Directory.CreateDirectory(destDir);
                string destPath = Path.Combine(destDir, result.FileName);

                // Already added and already extracted — skip everything
                if (_category.PdfPaths.Contains(destPath) && _category.IsExtracted(destPath))
                {
                    await DisplayAlert("Already added",
                        $"\"{result.FileName}\" is already in this category and has been extracted.", "OK");
                    return;
                }

                // Copy file if not already there
                if (!File.Exists(destPath))
                {
                    using var src = await result.OpenReadAsync();
                    using var dest = File.OpenWrite(destPath);
                    await src.CopyToAsync(dest);
                }

                // Add to paths if not already listed
                if (!_category.PdfPaths.Contains(destPath))
                {
                    _category.PdfPaths.Add(destPath);
                    _category.UpdatedAt = DateTime.Now;
                    SaveCategory();
                }

                // Navigate to loading page for extraction
                await Shell.Current.GoToAsync(
                    $"loading?id={_category.Id}&pdfpath={Uri.EscapeDataString(destPath)}");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        async void OnRemovePdfTapped(object sender, TappedEventArgs e)
        {
            if (sender is not View btn) return;
            var pdf = btn.BindingContext as PdfItem;
            if (pdf == null || _category == null) return;

            bool ok = await DisplayAlert("Remove PDF",
                $"Remove \"{pdf.FileName}\"? Its extracted questions will also be removed.",
                "Remove", "Cancel");
            if (!ok) return;

            // Delete file
            if (File.Exists(pdf.FilePath)) File.Delete(pdf.FilePath);

            // Remove from both collections
            _category.PdfPaths.Remove(pdf.FilePath);
            _category.PdfQuestions.Remove(pdf.FilePath);
            _category.UpdatedAt = DateTime.Now;
            SaveCategory();

            _pdfs.Remove(pdf);
            RefreshUI();
        }

        async void OnStartQuizTapped(object sender, EventArgs e)
        {
            if (sender is View v) { await v.ScaleTo(0.96, 80); await v.ScaleTo(1, 80); }
            if (_category == null) return;
            await Shell.Current.GoToAsync($"quizsettings?id={_category.Id}");
        }
    }
}