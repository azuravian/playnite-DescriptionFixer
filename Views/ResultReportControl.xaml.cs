using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DescriptionFixer;

namespace DescriptionFixer.Views
{
    /// <summary>
    /// Interaction logic for ResultReportControl.xaml
    /// </summary>
    public partial class ResultReportControl : UserControl
    {
        public ResultReportControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Populates the report with the processing results.
        /// </summary>
        /// <param name="changedGames">List of games that were changed</param>
        /// <param name="videoImageChanges">Dictionary with video/image change categories and counts</param>
        /// <param name="emojiRepaired">Number of emoji repaired</param>
        /// <param name="formattingChanges">Number of formatting changes</param>
        public void SetResults(
            List<string> changedGames,
            Dictionary<string, int> videoImageChanges,
            int emojiRepaired,
            int formattingChanges)
        {
            ReportStack.Children.Clear();

            // Games changed
            ReportStack.Children.Add(CreateSectionHeader($"Games Changed ({changedGames.Count})"));
            foreach (var game in changedGames)
            {
                ReportStack.Children.Add(CreateTextItem(game));
            }

            // Video/Image changes
            ReportStack.Children.Add(CreateSectionHeader("Video/Image Changes"));
            foreach (var kvp in videoImageChanges)
            {
                ReportStack.Children.Add(CreateTextItem($"{kvp.Key}: {kvp.Value}"));
            }

            // Emoji repaired
            ReportStack.Children.Add(CreateSectionHeader("Emoji Repaired"));
            ReportStack.Children.Add(CreateTextItem(emojiRepaired.ToString()));

            // Formatting changes
            ReportStack.Children.Add(CreateSectionHeader("Formatting Changes"));
            ReportStack.Children.Add(CreateTextItem(formattingChanges.ToString()));
        }

        private TextBlock CreateSectionHeader(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 16,
                FontWeight = System.Windows.FontWeights.Bold,
                Foreground = Brushes.LightBlue,
                Margin = new System.Windows.Thickness(0, 10, 0, 5)
            };
        }

        private TextBlock CreateTextItem(string text)
        {
            return new TextBlock
            {
                Text = "• " + text,
                FontSize = 14,
                Foreground = Brushes.White,
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Margin = new System.Windows.Thickness(10, 2, 0, 2)
            };
        }

        private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
    }
}
