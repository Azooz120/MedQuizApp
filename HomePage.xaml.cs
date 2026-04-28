using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace MCQS
{
    public partial class HomePage : ContentPage
    {
        readonly ObservableCollection<Category> _categories = new();

        static string StoragePath =>
            System.IO.Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "categories.json");

        public HomePage()
        {
            InitializeComponent();
            CategoryGrid.ItemsSource = _categories;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            LoadCategories();
            RefreshEmptyState();
            Opacity = 0;
            await this.FadeTo(1, 350, Easing.CubicOut);
        }

        void LoadCategories()
        {
            _categories.Clear();
            if (!System.IO.File.Exists(StoragePath)) return;
            var json = System.IO.File.ReadAllText(StoragePath);
            var list = JsonSerializer.Deserialize<List<Category>>(json);
            if (list == null) return;
            foreach (var c in list) _categories.Add(c);
        }

        void SaveCategories()
        {
            var json = JsonSerializer.Serialize(new List<Category>(_categories));
            System.IO.File.WriteAllText(StoragePath, json);
        }

        void RefreshEmptyState()
        {
            EmptyState.IsVisible = _categories.Count == 0;
            CategoryGrid.IsVisible = _categories.Count > 0;
        }

        async void OnAddTapped(object sender, TappedEventArgs e)
        {
            if (sender is View v) { await v.ScaleTo(0.85, 70); await v.ScaleTo(1, 70); }
            string name = await DisplayPromptAsync(
                "New Category", "Enter a name for your category:",
                "Create", "Cancel", placeholder: "e.g. Biology, History..."
            );
            if (string.IsNullOrWhiteSpace(name)) return;
            _categories.Add(new Category
            {
                Name = name.Trim(),
                ColorIndex = _categories.Count % 6
            });
            SaveCategories();
            RefreshEmptyState();
        }

        async void OnCardTapped(object sender, TappedEventArgs e)
        {
            if (sender is not View card) return;
            await card.ScaleTo(0.93, 80);
            await card.ScaleTo(1, 80);
            var cat = card.BindingContext as Category;
            if (cat == null) return;
            await Shell.Current.GoToAsync($"category?id={cat.Id}");
        }

        async void OnMenuTapped(object sender, TappedEventArgs e)
        {
            if (sender is not View btn) return;
            await btn.ScaleTo(0.8, 60);
            await btn.ScaleTo(1, 60);
            var cat = btn.BindingContext as Category;
            if (cat == null) return;
            await ShowOptionsMenu(cat);
        }

        async Task ShowOptionsMenu(Category cat)
        {
            string action = await DisplayActionSheet(
                cat.Name, "Cancel", null,
                "✏️ Rename", "🎨 Change Color", "🗑️ Delete"
            );
            switch (action)
            {
                case "✏️ Rename": await RenameCategory(cat); break;
                case "🎨 Change Color": await ChangeColor(cat); break;
                case "🗑️ Delete": await DeleteCategory(cat); break;
            }
        }

        async Task RenameCategory(Category cat)
        {
            string name = await DisplayPromptAsync(
                "Rename Category", "Enter a new name:",
                "Save", "Cancel", initialValue: cat.Name
            );
            if (string.IsNullOrWhiteSpace(name)) return;
            var i = _categories.IndexOf(cat);
            cat.Name = name.Trim();
            cat.UpdatedAt = DateTime.Now;
            if (i >= 0) { _categories.RemoveAt(i); _categories.Insert(i, cat); }
            SaveCategories();
        }

        async Task ChangeColor(Category cat)
        {
            var colorNames = new[] {
                "🟣 Violet", "🔴 Coral", "🟢 Mint",
                "🟠 Orange", "🩵 Teal", "💜 Lavender"
            };
            string picked = await DisplayActionSheet("Pick a Color", "Cancel", null, colorNames);
            if (picked == null || picked == "Cancel") return;
            int index = Array.IndexOf(colorNames, picked);
            if (index < 0) return;
            var i = _categories.IndexOf(cat);
            cat.ColorIndex = index;
            cat.UpdatedAt = DateTime.Now;
            if (i >= 0) { _categories.RemoveAt(i); _categories.Insert(i, cat); }
            SaveCategories();
        }

        async Task DeleteCategory(Category cat)
        {
            bool confirmed = await DisplayAlert(
                "Delete Category",
                $"Delete \"{cat.Name}\" and all its PDFs? This cannot be undone.",
                "Delete", "Cancel"
            );
            if (!confirmed) return;
            _categories.Remove(cat);
            SaveCategories();
            RefreshEmptyState();
        }
    }
}