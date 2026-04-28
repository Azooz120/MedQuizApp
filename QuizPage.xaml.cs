using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MCQS
{
    public partial class QuizPage : ContentPage
    {
        // ── State ─────────────────────────────────────────────────────────────
        List<QuizQuestion> _questions;
        int _currentIndex = 0;
        int _score = 0;
        bool _answered = false;

        // Option borders + badges for easy access
        Border[] _optionCards;
        Border[] _optionBadges;
        Label[] _optionLabels;

        double _barMaxWidth = 0;

        // ── Constructor ───────────────────────────────────────────────────────
        public QuizPage()
        {
            InitializeComponent();

            _optionCards = new[] { Option0, Option1, Option2, Option3 };
            _optionBadges = new[] { Badge0, Badge1, Badge2, Badge3 };
            _optionLabels = new[] { OptionLabel0, OptionLabel1, OptionLabel2, OptionLabel3 };
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _questions = QuizSession.Questions;
            _currentIndex = 0;
            _score = 0;

            // Reset wrong answers tracking
            QuizSession.WrongAnswers = new List<WrongAnswerRecord>();
            QuizSession.Total = _questions.Count;

            Opacity = 0;
            await this.FadeTo(1, 300, Easing.CubicOut);
            LoadQuestion();
        }

        // ── Load question ─────────────────────────────────────────────────────
        void LoadQuestion()
        {
            if (_questions == null || _questions.Count == 0) return;

            _answered = false;
            var q = _questions[_currentIndex];
            int total = _questions.Count;

            // Header
            QuestionNumberLabel.Text = $"Question {_currentIndex + 1} of {total}";
            ScoreLabel.Text = $"{_score} ✓";

            // Progress bar
            double pct = (double)_currentIndex / total;
            double targetWidth = _barMaxWidth * pct;
            ProgressFill.WidthRequest = Math.Max(targetWidth, 0);

            // Question text
            QuestionLabel.Text = q.Question;

            // Options
            string[] letters = { "A", "B", "C", "D" };
            for (int i = 0; i < 4; i++)
            {
                _optionLabels[i].Text = q.Options[i];
                ResetOptionStyle(i, letters[i]);
            }

            // Hide explanation + next
            ExplanationCard.IsVisible = false;
            NextButton.IsVisible = false;
        }

        // ── Option tapped ─────────────────────────────────────────────────────
        async void OnOptionTapped(object sender, TappedEventArgs e)
        {
            if (_answered) return;
            _answered = true;

            int selected = int.Parse(e.Parameter?.ToString() ?? "0");
            int correct = _questions[_currentIndex].CorrectIndex;

            if (sender is View v) { await v.ScaleTo(0.96, 60); await v.ScaleTo(1.00, 60); }

            string[] letters = { "A", "B", "C", "D" };
            for (int i = 0; i < 4; i++)
            {
                if (i == correct) ApplyCorrectStyle(i, letters[i]);
                else if (i == selected && selected != correct) ApplyWrongStyle(i, letters[i]);
                else ApplyDimStyle(i);
            }

            if (selected == correct)
            {
                _score++;
            }
            else
            {
                // Record wrong answer for results review
                QuizSession.WrongAnswers.Add(new WrongAnswerRecord
                {
                    Question = _questions[_currentIndex].Question,
                    SelectedIndex = selected
                });
            }

            ScoreLabel.Text = $"{_score} ✓";

            ExplanationLabel.Text = _questions[_currentIndex].Explanation;
            ExplanationCard.IsVisible = true;
            await ExplanationCard.FadeTo(1, 250);

            bool isLast = _currentIndex == _questions.Count - 1;
            NextButton.Text = isLast ? "See Results 🏁" : "Next →";
            NextButton.IsVisible = true;
            await NextButton.FadeTo(1, 200);
        }

        // ── Next tapped ───────────────────────────────────────────────────────
        async void OnNextTapped(object sender, EventArgs e)
        {
            if (sender is View v) { await v.ScaleTo(0.96, 70); await v.ScaleTo(1, 70); }

            bool isLast = _currentIndex == _questions.Count - 1;

            if (isLast)
            {
                // Store results in session
                QuizSession.Score = _score;
                QuizSession.Total = _questions.Count;
                await Shell.Current.GoToAsync("results");
            }
            else
            {
                _currentIndex++;

                // Animate transition
                await this.FadeTo(0.4, 120);
                LoadQuestion();
                await this.FadeTo(1.0, 120);
            }
        }

        // ── Exit tapped ───────────────────────────────────────────────────────
        async void OnExitTapped(object sender, TappedEventArgs e)
        {
            if (sender is View v) { await v.ScaleTo(0.85, 70); await v.ScaleTo(1, 70); }

            bool confirm = await DisplayAlert(
                "Exit Quiz",
                "Are you sure you want to exit? Your progress will be lost.",
                "Exit", "Continue"
            );
            if (!confirm) return;
            await Shell.Current.GoToAsync("//home");
        }

        // ── Style helpers ─────────────────────────────────────────────────────
        void ResetOptionStyle(int i, string letter)
        {
            _optionCards[i].BackgroundColor = (Color)Application.Current.Resources["CardBackground"];
            _optionCards[i].Stroke = new SolidColorBrush(Color.FromArgb("#F0F0F0"));
            _optionCards[i].StrokeThickness = 1.5;
            _optionCards[i].Opacity = 1;

            _optionBadges[i].BackgroundColor = (Color)Application.Current.Resources["IconBadgeBackground"];
            var badgeLabel = _optionBadges[i].Content as Label;
            if (badgeLabel != null)
            {
                badgeLabel.Text = letter;
                badgeLabel.TextColor = (Color)Application.Current.Resources["TextMuted"];
            }

            _optionLabels[i].TextColor = (Color)Application.Current.Resources["TextBody"];
        }

        void ApplyCorrectStyle(int i, string letter)
        {
            _optionCards[i].BackgroundColor = Color.FromArgb("#F0FFF4");
            _optionCards[i].Stroke = new SolidColorBrush(Color.FromArgb("#26de81"));
            _optionCards[i].StrokeThickness = 2;

            _optionBadges[i].BackgroundColor = Color.FromArgb("#26de81");
            var badgeLabel = _optionBadges[i].Content as Label;
            if (badgeLabel != null)
            {
                badgeLabel.Text = "✓";
                badgeLabel.TextColor = Colors.White;
            }

            _optionLabels[i].TextColor = Color.FromArgb("#1a7a3a");
        }

        void ApplyWrongStyle(int i, string letter)
        {
            _optionCards[i].BackgroundColor = Color.FromArgb("#FFF0F0");
            _optionCards[i].Stroke = new SolidColorBrush(Color.FromArgb("#E53935"));
            _optionCards[i].StrokeThickness = 2;

            _optionBadges[i].BackgroundColor = Color.FromArgb("#E53935");
            var badgeLabel = _optionBadges[i].Content as Label;
            if (badgeLabel != null)
            {
                badgeLabel.Text = "✕";
                badgeLabel.TextColor = Colors.White;
            }

            _optionLabels[i].TextColor = Color.FromArgb("#E53935");
        }

        void ApplyDimStyle(int i)
        {
            _optionCards[i].Opacity = 0.45;
        }
    }
}