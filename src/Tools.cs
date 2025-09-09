using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Markdown;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UglyToad.PdfPig;

namespace AISlop
{
    public class Tools
    {
        string _workspace = "environment";
        string _workspaceRoot = "environment";
        string _workspacePlan = "environment";

        public Tools()
        {
            if (!Directory.Exists(_workspaceRoot))
                Directory.CreateDirectory(_workspaceRoot);
        }

        /// <summary>
        /// Creates a file
        /// </summary>
        /// <param name="filename">Filapath and name WITH extension</param>
        /// <param name="content">File content</param>
        /// <returns>Status</returns>
        public string CreateFile(string filename, string content, string cwd)
        {
            string filePath = Path.Combine(cwd, filename);
            if (File.Exists(filePath))
                return $"A file with that name already exists in the workspace: {filename}";

            using var file = File.Create(filePath);
            using StreamWriter sw = new(file, Encoding.UTF8);

            content = Regex.Unescape(content);

            sw.Write(content);

            return $"File has been created: \"{filename}\" and content written into it";
        }

        /// <summary>
        /// Trims and makes CWD string the same style
        /// </summary>
        /// <param name="path">current cwd eg.: "/workspace/project/js" or "C:\...\workspace\project\js"</param>
        /// <returns>Normalized path string</returns>
        private static string TrimToWorkspace(string path)
        {
            string workspaceRoot = Path.Combine(Directory.GetCurrentDirectory(), "workspace");

            string normalizedFullPath = Path.GetFullPath(path).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            string normalizedWorkspace = Path.GetFullPath(workspaceRoot).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            normalizedWorkspace = normalizedWorkspace.TrimEnd(Path.DirectorySeparatorChar);

            if (normalizedFullPath.StartsWith(normalizedWorkspace, StringComparison.OrdinalIgnoreCase))
            {
                string relativePath = normalizedFullPath.Substring(normalizedWorkspace.Length);
                return relativePath.TrimStart(Path.DirectorySeparatorChar);
            }

            if (normalizedFullPath.StartsWith("/" + Path.GetFileName(normalizedWorkspace)))
            {
                return normalizedFullPath.Substring(1);
            }

            return path;
        }

        public string GetCurrentWorkDirectory()
        {
            return _workspace;
        }
        /// <summary>
        /// Creates a directory
        /// </summary>
        /// <param name="name">Path to the directory</param>
        /// <param name="setAsActive">DEPRECATED HAS TO BE REMOVED</param>
        /// <returns>Status</returns>
        public string CreateDirectory(string name, bool setAsActive)
        {
            string folder = Path.Combine(_workspace, name);
            if (Directory.Exists(folder))
                return $"Directory already exists with name: \"{name}\"";

            var output = Directory.CreateDirectory(folder);
            if (setAsActive)
                _workspace = folder;
            return $"Directory created at: \"{folder}\"." + (setAsActive ? $" Current active directory: \"{folder}\"" : "");
        }

        /// <summary>
        /// Reads a file content
        /// </summary>
        /// <param name="filename">File path + file name with extension</param>
        /// <returns>File content</returns>
        public string ReadFile(string filename)
        {
            string filePath = Path.Combine(_workspace, filename);
            if (filename.ToLower().Contains("plan"))
                filePath = _workspacePlan;

            if (!File.Exists(filePath))
                return $"The file does not exists: \"{filePath}\"";

            var file = File.OpenRead(filePath);
            using StreamReader sr = new(file);

            return "File content:\n```\n" + sr.ReadToEnd().ToString() + "\n```";
        }
        /// <summary>
        /// Overrides a file DEPRECATED has to be "WriteFile" for clarity
        /// </summary>
        /// <param name="filename">file path + name with extension</param>
        /// <param name="text">text to be written into the file</param>
        /// <returns>Status</returns>
        public string OverwriteFile(string filename, string text, string cwd)
        {
            string filePath = Path.Combine(cwd, filename);

            if (File.Exists(filePath))
                File.Delete(filePath);

            return CreateFile(filename, text, cwd);
        }
        /// <summary>
        /// Lists out files, DEPRECATED currently not priority, cmd return a decent string for the Agent
        /// </summary>
        /// <returns>CWD folder + file structure</returns>
        public string GetWorkspaceEntries()
        {
            var terminalOutput = ExecuteTerminal("tree /f | more +3", "environment");
            return $"Entries in folder \"{_workspace}\":\n{terminalOutput}";
        }
        /// <summary>
        /// Changes CWD
        /// </summary>
        /// <param name="folderName">path to the folder</param>
        /// <returns>Status</returns>
        public string OpenFolder(string folderName, ref string cwd)
        {
            if (folderName == "environment")
            {
                cwd = "environment";
                return $"Successfully changed to folder \"{cwd}\"";
            }

            if (cwd.Contains(folderName))
                return $"Already in a folder named \"{folderName}\"";

            string path = Path.Combine(cwd, folderName);
            if (!Directory.Exists(path))
                return $"Directory \"{folderName}\" does not exist";

            cwd = path;
            return $"Successfully changed to folder \"{folderName}\"";
        }
        /// <summary>
        /// Reads text from pdf
        /// </summary>
        /// <param name="filename">path + filename with extension</param>
        /// <returns></returns>
        public string ReadTextFromPDF(string filename)
        {
            var filePath = Path.Combine(_workspace, filename);
            if (!File.Exists(filePath))
                return $"File \"{filename}\" does not exist.";

            using var document = PdfDocument.Open(filePath);
            StringBuilder sb = new();
            foreach (var page in document.GetPages())
            {
                double? lastY = null;
                foreach (var word in page.GetWords())
                {
                    var y = word.BoundingBox.Top;
                    if (lastY != null && Math.Abs(lastY.Value - y) > 5)
                        sb.AppendLine();

                    sb.Append($"{word.Text} ");
                    lastY = y;
                }
            }

            return $"PDF file content:\n{sb.ToString()}";
        }
        /// <summary>
        /// Executes WINDOWS terminal functions
        /// </summary>
        /// <param name="command">Command to run eg.: npm init -y</param>
        /// <returns>CMD output</returns>
        public string ExecuteTerminal(string command, string cwd)
        {
            var processInfo = new ProcessStartInfo("cmd.exe", $"/c {command}")
            {
                WorkingDirectory = Path.Combine("./", cwd),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            using var process = Process.Start(processInfo);

            string output = process!.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (string.IsNullOrWhiteSpace(output.Trim()))
                output = "Command success!";

            return output + error;
        }

        /// <summary>
        /// Creates a PDF file from markdown input
        /// </summary>
        /// <param name="filename">path + filename with extension</param>
        /// <param name="markdowntext">markdown input</param>
        /// <returns>Status</returns>
        public string CreatePdfFile(string filename, string markdowntext, string cwd)
        {
            var path = Path.Combine(cwd, filename);
            if (File.Exists(path))
                return $"File already exists with name {filename} in CWD";

            markdowntext = markdowntext.Replace("\\n", "\n").Replace("\\t", "\t");
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.PageColor(Colors.White);
                    page.Margin(40);
                    page.Content().Markdown(markdowntext);
                });
            });

            document.GeneratePdf(path);
            return $"File has been created: \"{path}\" and content written into it";
        }
        /// <summary>
        /// Displays the task has been completed
        /// </summary>
        /// <param name="message">task ending message</param>
        /// <returns></returns>
        public string TaskDone(string message)
        {
            Logging.DisplayAgentThought(message, ConsoleColor.Yellow);
            return "";
        }
        /// <summary>
        /// Asks the user a question if the task is not clear
        /// </summary>
        /// <param name="message">Question</param>
        /// <returns>User response</returns>
        public string AskUser(string message)
        {
            Logging.DisplayAgentThought(message, ConsoleColor.Cyan);
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("Response: ");
            Console.ForegroundColor = ConsoleColor.Gray;
            return Console.ReadLine()!;
        }
    }
}