using System;
using System.IO;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace MCQS
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Items.Add(new ShellContent
            {
                Route = "home",
                Content = new HomePage()
            });

            Routing.RegisterRoute("category", typeof(CategoryPage));
            Routing.RegisterRoute("quizsettings", typeof(QuizSettingsPage));
            Routing.RegisterRoute("loading", typeof(LoadingPage));
            Routing.RegisterRoute("quiz", typeof(QuizPage));
            Routing.RegisterRoute("results", typeof(ResultsPage));
        }
    }
}