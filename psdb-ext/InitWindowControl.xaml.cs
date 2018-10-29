namespace psdb_ext
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using EnvDTE80;

    /// <summary>
    /// Interaction logic for InitWindowControl.
    /// </summary>
    public partial class InitWindowControl : UserControl
    {
        public string CurrentDirectory { get; set; }
        public string ProjectName { get; set; }

        public void ResolveNamespace()
        {
            var start = CurrentDirectory.IndexOf(ProjectName);
            var nameSpace = CurrentDirectory.Substring(start).Replace('\\', '.');
            this.textNamespace.Text = nameSpace;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InitWindowControl"/> class.
        /// </summary>
        public InitWindowControl()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void ok_click(object sender, RoutedEventArgs e)
        {
            var version = this.cbEngine.SelectedIndex == 0 ? "core" : "6";
            var engine = this.cbEngine.SelectedIndex == 0 ? "SqlServer" : "MySQL";

            var cmdText = $"init --version {version} --engine {engine} --context-name=\"{this.textContextName.Text}\" --output-dir \"./\" --namespace {this.textNamespace.Text} --connection-string \"{this.txtConnectionString.Text}\"";

            Helpers.InvokeCommandLineAndOutput("psdb", cmdText, executeIn: CurrentDirectory);
            var window = Window.GetWindow(this);
            window.Close();
        }

        

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void cancel_click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            window.Close();
        }

        private void ComboBoxItem_Selected(object sender, RoutedEventArgs e)
        {

        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}