using System.Windows;

namespace NoSilence
{
    public partial class OverwriteDialog : Window
    {
        public OverwriteDialogResult Result { get; private set; }

        public OverwriteDialog(string fileName)
        {
            InitializeComponent();
            messageText.Text = $"The file '{fileName}' already exists. Do you want to overwrite it?";
            Result = OverwriteDialogResult.Cancel; // Default to Cancel if closed
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            Result = OverwriteDialogResult.Yes;
            this.Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            Result = OverwriteDialogResult.No;
            this.Close();
        }

        private void YesToAll_Click(object sender, RoutedEventArgs e)
        {
            Result = OverwriteDialogResult.YesToAll;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Result = OverwriteDialogResult.Cancel;
            this.Close();
        }
    }

    public enum OverwriteDialogResult
    {
        Yes,
        No,
        YesToAll,
        Cancel
    }
}
