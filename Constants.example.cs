namespace MCQS
{
    /// <summary>
    /// TEMPLATE FILE - Copy this file to Constants.cs and fill in your actual API keys.
    /// The actual Constants.cs file is in .gitignore to protect sensitive data.
    /// </summary>
    public static class Constants
    {
        // OpenAI API Key for question generation
        // Get from: https://platform.openai.com/api-keys
        public const string OPENAI_API_KEY = "YOUR_API_KEY_HERE";
        public const string GroqEndpoint = "https://api.groq.ai/v1/endpoint";
        public const string GroqKey = "YOUR_GROQ_API_KEY_HERE";
        
        // API Base URL (if using custom backend)
        public const string API_BASE_URL = "https://api.example.com";
        
        // Quiz configuration constants
        public const int DEFAULT_QUESTIONS_PER_QUIZ = 10;
        public const int QUESTION_TIMEOUT_SECONDS = 30;
        
        // Add more constants as needed following this pattern
    }
}
