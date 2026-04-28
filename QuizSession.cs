using System;
using System.Linq;
using System.Collections.Generic;

namespace MCQS
{
    public class WrongAnswerRecord
    {
        public string Question { get; set; } = string.Empty;
        public int SelectedIndex { get; set; }
    }

    public static class QuizSession
    {
        public static List<QuizQuestion> Questions { get; set; } = new();
        public static List<WrongAnswerRecord> WrongAnswers { get; set; } = new();
        public static string CategoryName { get; set; } = string.Empty;
        public static int Score { get; set; }
        public static int Total { get; set; }
    }
}