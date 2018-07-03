using System;
using System.ComponentModel.Design;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace OpenAssetsInVsCode
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class OpenAssetsInVsCodeCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("78e5f1e1-b6d5-40d8-8d1a-7a4adc3083ea");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAssetsInVsCodeCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private OpenAssetsInVsCodeCommand(AsyncPackage package, OleMenuCommandService commandService)
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
        public static OpenAssetsInVsCodeCommand Instance
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
        /// Current Visual Studio object model
        /// </summary>
        public static DTE2 _dte = null;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Verify the current thread is the UI thread - the call to AddCommand in OpenAssetsInVsCodeCommand's constructor requires
            // the UI thread.
            ThreadHelper.ThrowIfNotOnUIThread();

            _dte = await package.GetServiceAsync(typeof(DTE)) as DTE2;

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new OpenAssetsInVsCodeCommand(package, commandService);
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

            string solutionPath = String.Empty;
            string assetsDir = String.Empty;
            string configFilePath = String.Empty;

            try {
                solutionPath = Path.GetDirectoryName(_dte.Solution.FullName);
            } catch {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "You should open a solution.",
                    "Open Assets In VS Code",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            configFilePath = solutionPath + "assetsconfig.json";

            if (File.Exists(configFilePath)) {
                // TODO: Eğer solution path'de json dosyası mevcutsa içinden AssetsDirectory ile path alıp assetsDir'e set edilmeli
                // bu dosya yoksa aşağıda regex ile Assets isimli klasör bulunup assetsDir'e set edilmeli
            }

            Array subDirectories = Directory.GetDirectories(solutionPath);
            
            Regex reg = new Regex(@"([a-z]|[A-Z]).*\/?(Assets)");

            foreach (string dir in subDirectories) {
                if (reg.Match(dir).Success) {
                    assetsDir = dir;
                    break;
                }
            }

            if (String.IsNullOrEmpty(assetsDir))
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "Can't find any assets folder in solution directory",
                    "Open Assets In VS Code",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            proc.StartInfo.FileName = "CMD.exe";
            proc.StartInfo.Arguments = "/C code " + assetsDir;
            proc.Start();
        }
    }
}
