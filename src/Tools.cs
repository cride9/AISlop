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
        string _workspace = "workspace";
        string _workspaceRoot = "workspace";
        string _workspacePlan = "workspace";

        public Tools()
        {
            if (!Directory.Exists(_workspaceRoot))
                Directory.CreateDirectory(_workspaceRoot);
        }

        public string GetCurrentWorkDirectory()
        {
            return _workspace;
        }

        /*
        {
            "tool": "CreateDirectory",
            "args": {
                "name": "directoryName"
            }
        }
        */
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
        /*
        {
            "tool": "CreateFile",
            "args": {
                "filename": "FilaName.extension",
                "content": "file content"
            }
        }
        */
        public string CreateFile(string filename, string content)
        {
            string filePath = Path.Combine(_workspace, filename);
            if (File.Exists(filePath))
                return $"A file with that name already exists in the workspace: {filename}";

            if (filename.ToLower().Contains("plan"))
            {
                if (!_workspacePlan.Contains("plan"))
                    _workspacePlan = filePath;
                else
                    filePath = _workspacePlan;
            }

                using var file = File.Create(filePath);
            using StreamWriter sw = new(file, Encoding.UTF8);

            content = Regex.Unescape(content);

            sw.Write(content);

            return $"File has been created: \"{filename}\" and content written into it";
        }
        /*
        {
            "tool": "ReadFile",
            "args": {
                "filename": "FilaName.extension",
            }
        }
        */
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
        /*
        {
            "tool": "ModifyFile",
            "args": {
                "filename": "FilaName.extension",
                "insertText": "text"
            }
        }
        */
        public string ModifyFile(string filename, string text)
        {
            string filePath = Path.Combine(_workspace, filename);
            if (filename.ToLower().Contains("plan"))
                filePath = _workspacePlan;

            if (!File.Exists(filePath))
                return $"The file does not exists: \"{filePath}\"";

            File.Delete(filePath);
            return CreateFile(filename, text);
        }
        /*
        {
            "tool": "GetWorkspaceEntries",
            "args": {
            }
        }
        */
        public string GetWorkspaceEntries()
        {
            var terminalOutput = ExecuteTerminal("tree /f | more +3");
            return $"Entries in folder \"{_workspace}\":\n{terminalOutput}";
        }
        /*
        {
            "tool": "OpenFolder",
            "args": {
                "folderName": "foldername"
            }
        }
        */
        public string OpenFolder(string folderName)
        {
            if (folderName == "workspace")
            {
                _workspace = _workspaceRoot;
                return $"Successfully changed to folder \"{_workspace}\"";
            }

            if (_workspace.Contains(folderName))
                return $"Already in a folder named \"{folderName}\"";

            string path = Path.Combine(_workspace, folderName);
            string rootPath = Path.Combine(_workspaceRoot, folderName);
            if (!Directory.Exists(path) && !Directory.Exists(rootPath))
                return $"Directory \"{folderName}\" does not exist";

            // safe handle, if AI fails to navigate back
            if (Directory.Exists(rootPath))
            {
                _workspace = rootPath;
                return $"Successfully changed to folder \"{folderName}\"";
            }

            _workspace = path;
            return $"Successfully changed to folder \"{folderName}\"";
        }
        /*
        {
            "tool": "ReadTextFromPDF",
            "args": {
                "filename": "filename"
            }
        }
        */
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
        /*
        {
            "tool": "ExecuteTerminal",
            "args": {
                "command": "command"
            }
        }
        */
        public string ExecuteTerminal(string command)
        {
            var processInfo = new ProcessStartInfo("cmd.exe", $"/c {command}")
            {
                WorkingDirectory = Path.GetFullPath(_workspace),
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

        /*
        {
            "tool": "CreatePdfFile",
            "args": {
                "fileName": "fileName",
                "markdownText": "markdownText"
            }
        }
        */
        public string CreatePdfFile(string filename, string markdowntext)
        {
            var path = Path.Combine(_workspace, filename);
            if (File.Exists(path))
                return $"File already exists with name {filename} in CWD";

            //markdowntext = Regex.Unescape(markdowntext);
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

        public string TaskDone(string message)
        {
            Logging.DisplayAgentThought(message, ConsoleColor.Yellow);
            return ""; // nothing to return
        }

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
