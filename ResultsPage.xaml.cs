using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace MCQS
{
    // ── Model for wrong answer review list ────────────────────────────────────
    public class WrongAnswer
    {
        public string Question { get; set; } = string.Empty;
        public string YourAnswerLabel { get; set; } = string.Empty;
        public string CorrectAnswerLabel { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
    }

    public partial class ResultsPage : ContentPage
    {
        public ResultsPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Opacity = 0;
            await this.FadeTo(1, 350, Easing.CubicOut);
            LoadResults();
        }

        void LoadResults()
        {
            int score = QuizSession.Score;
            int total = QuizSession.Total;
            int wrong = total - score;
            double pct = total > 0 ? (double)score / total * 100 : 0;
            var questions = QuizSession.Questions;

            // Header
            CategoryLabel.Text = QuizSession.CategoryName;

            // Score circle
            ScoreLabel.Text = score.ToString();
            ScoreOutOfLabel.Text = $"out of {total}";

            // Stats
            CorrectCount.Text = score.ToString();
            WrongCount.Text = wrong.ToString();
            PctLabel.Text = $"{(int)pct}%";

            // Performance message
            SetPerformanceMessage(pct);

            // Wrong answers list
            if (wrong == 0)
            {
                WrongAnswersSection.IsVisible = false;
                return;
            }

            var wrongAnswers = new List<WrongAnswer>();

            // We need to know which answers were wrong
            // QuizSession stores wrong indices
            foreach (var wa in QuizSession.WrongAnswers)
            {
                var q = questions.FirstOrDefault(x => x.Question == wa.Question);
                if (q == null) continue;

                wrongAnswers.Add(new WrongAnswer
                {
                    Question = q.Question,
                    YourAnswerLabel = $"Your answer: {q.Options[wa.SelectedIndex]}",
                    CorrectAnswerLabel = $"Correct: {q.Options[q.CorrectIndex]}",
                    Explanation = q.Explanation
                });
            }

            WrongAnswersSection.IsVisible = wrongAnswers.Count > 0;
            WrongList.ItemsSource = wrongAnswers;
        }

        void SetPerformanceMessage(double pct)
        {
            if (pct == 100)
            {
                PerformanceEmoji.Text = "🏆";
                PerformanceLabel.Text = "Perfect Score!";
                PerformanceSubLabel.Text = "Outstanding performance";
            }
            else if (pct >= 80)
            {
                PerformanceEmoji.Text = "🌟";
                PerformanceLabel.Text = "Excellent!";
                PerformanceSubLabel.Text = "You're well prepared";
            }
            else if (pct >= 60)
            {
                PerformanceEmoji.Text = "👍";
                PerformanceLabel.Text = "Good Job!";
                PerformanceSubLabel.Text = "A little more practice and you'll ace it";
            }
            else if (pct >= 40)
            {
                PerformanceEmoji.Text = "📖";
                PerformanceLabel.Text = "Keep Studying";
                PerformanceSubLabel.Text = "Review the explanations below";
            }
            else
            {
                PerformanceEmoji.Text = "💪";
                PerformanceLabel.Text = "Don't Give Up!";
                PerformanceSubLabel.Text = "Every expert was once a beginner";
            }
        }

        async void OnBackTapped(object sender, EventArgs e)
        {
            if (sender is View v) { await v.ScaleTo(0.96, 70); await v.ScaleTo(1, 70); }
            await Shell.Current.GoToAsync("//home");
        }

        async void OnRetryTapped(object sender, EventArgs e)
        {
            if (sender is View v) { await v.ScaleTo(0.96, 70); await v.ScaleTo(1, 70); }

            // Reshuffle same questions
            var rng = new Random();
            var reshuffled = QuizSession.Questions
                .OrderBy(_ => rng.Next())
                .ToList();

            QuizSession.Questions = reshuffled;
            QuizSession.WrongAnswers = new List<WrongAnswerRecord>();
            QuizSession.Score = 0;
            QuizSession.Total = reshuffled.Count;

            await Shell.Current.GoToAsync("quiz");
        }
    }
}