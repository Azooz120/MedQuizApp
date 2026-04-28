# MedQuizApp - Medical Quiz Application

A .NET MAUI medical quiz app for Android that helps students create and take quizzes from PDF documents.

## 🚀 Quick Start (Phone Development)

### For Phone-Only Development:

1. **Edit code** on your phone using any text editor
2. **Push changes** to GitHub main branch
3. **GitHub Actions automatically builds APK** ✅ (Already configured!)
4. **Download APK** from Actions artifacts
5. **Install & test** on your phone

### Download APK:
- Go to **Actions** tab in GitHub
- Click latest workflow run
- Download **android-apk** artifact
- Install APK on your phone

## 🛠️ Development Setup

### Prerequisites:
- .NET 9 SDK
- Android SDK (for local development)
- Visual Studio Code with C# Dev Kit

### Local Build:
```bash
dotnet workload restore
dotnet build -f net9.0-android -c Debug
dotnet run -f net9.0-android
```

### Build APK:
```bash
dotnet publish -f net9.0-android -c Release -p:AndroidPackageFormat=apk
```

## 📱 Features

- 📚 **PDF Quiz Generation** - Extract questions from medical PDFs
- 🎯 **Interactive Quizzes** - Multiple choice questions with explanations
- 📊 **Results Tracking** - Score and wrong answer review
- 🎨 **Category Organization** - Color-coded quiz categories
- 📱 **Android Native** - Optimized for mobile devices

## 🔧 API Configuration

1. Copy `Constants.example.cs` to `Constants.cs`
2. Add your OpenAI API key for question generation
3. The real `Constants.cs` is gitignored for security

## 📂 Project Structure

```
MedQuizApp/
├── Models/
│   ├── Category.cs
│   ├── QuizQuestion.cs
│   └── QuizSession.cs
├── Views/
│   ├── HomePage.xaml
│   ├── QuizPage.xaml
│   ├── ResultsPage.xaml
│   └── ...
├── Services/
│   └── PdfExtractor.cs
├── Constants.cs (gitignored)
└── Constants.example.cs
```

## 🤝 Contributing

1. Fork the repository
2. Create feature branch
3. Push changes (triggers APK build)
4. Download and test APK
5. Create pull request

## ✅ Status

- ✅ Project structure set up
- ✅ GitHub Actions APK build configured
- ✅ API constants template created
- ✅ Dependencies configured
- ⚠️ Android SDK needed for local builds (GitHub Actions handles this)