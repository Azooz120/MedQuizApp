using System;
using System.Linq;
using System.Collections.Generic;

namespace MCQS
{
    public class QuizQuestion
    {
        public string Question { get; set; } = string.Empty;
        public string[] Options { get; set; } = new string[4];
        public int CorrectIndex { get; set; }
        public string Explanation { get; set; } = string.Empty;
    }
}