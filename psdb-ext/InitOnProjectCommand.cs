using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace psdb_ext
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class InitOnProjectCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4133;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("c589c84a-06cb-406f-a31b-a8ebf4d137c8");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitOnProjectCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private InitOnProjectCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static InitOnProjectCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in InitOnProjectCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new InitOnProjectCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = this.ServiceProvider.GetServiceAsync(typeof(DTE)).Result as EnvDTE80.DTE2;
            if (dte != null)
            {
                UIHierarchy uih = dte.ToolWindows.SolutionExplorer;
                Array selectedItems = (Array)uih.SelectedItems;
                if (null != selectedItems)
                {
                    string directory = null;
                    string projectName = null;
                    foreach (UIHierarchyItem selItem in selectedItems)
                    {
                        var project = selItem.Object as Project;
                        projectName = project.Name;
                        directory = project.Properties.Item("FullPath").Value.ToString();
                        break;
                    }

                    if (directory != null)
                    {
                        // Get the instance number 0 of this tool window. This window is single instance so this instance
                        // is actually the only one.
                        // The last flag is set to true so that if the tool window does not exists it will be created.
                        InitWindow window = this.package.FindToolWindow(typeof(InitWindow), 0, true) as InitWindow;
                        window.CurrentDirectory = directory;
                        window.ProjectName = projectName;
                        window.ResolveNamespace();
                        if ((null == window) || (null == window.Frame))
                        {
                            throw new NotSupportedException("Cannot create tool window");
                        }

                        IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
                        Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
                    }
                }
            }

        }
    }
}
