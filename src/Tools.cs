using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace AISlop
{
    public class Tools
    {
        string _workspace = "workspace";
        string _workspaceRoot = "workspace";
        /*
        {
            "tool": "CreateDirectory",
            "args": {
                "name": "directoryName"
            }
        }
        */
        public string CreateDirectory(string name)
        {
            string folder = Path.Combine(_workspace, name);
            if (Directory.Exists(folder))
                return $"Directory already exists with name: \"{name}\"";

            var output = Directory.CreateDirectory(folder);
            _workspace = folder;
            return $"Directory created at: \"{folder}\". Current active directory: \"{folder}\"";
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
                "lineNumber": "number",
                "charIndex": "number",
                "insertText": "Text to be inserted to place"
            }
        }
        */
        public string ModifyFile(string filename, int lineNumber, int charIndex, string insertText)
        {
            string filePath = Path.Combine(_workspace, filename);
            if (!File.Exists(filePath))
                return $"The file does not exists: \"{filePath}\"";

            string[] lines = File.ReadAllLines(filePath);
            if (lineNumber < 0 || lineNumber >= lines.Length)
                return "Invalid line number!";

            string line = lines[lineNumber];
            if (charIndex < 0 || charIndex > line.Length)
                return "Invalid character index";

            lines[lineNumber] = line.Insert(charIndex, insertText);
            File.WriteAllLines(filePath, lines);

            return $"File modified! Text \"{insertText}\" inserted to {lineNumber}:{charIndex}";
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
            StringBuilder sb = new();
            var entries = Directory.EnumerateFileSystemEntries(_workspace);
            int count = 1;
            foreach (var entry in entries)
            {
                sb.AppendLine($"\t{count++}. {Path.GetFileName(entry)}");
            }
            if (string.IsNullOrWhiteSpace(sb.ToString()))
                return $"The folder \"{_workspace}\" is empty.";

            return $"Entries in folder \"{_workspace}\":\n{sb}";
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
            if (_workspace.Contains(folderName))
                return $"Already in a folder named \"{folderName}\"";

            if (folderName.Contains(".."))
            {
                string parent = Directory.GetParent(_workspace)?.FullName!;
                if (parent == null)
                    return "Already at the root directory, cannot go up.";

                if (!parent.Contains("workspace"))
                    return "Invalid path. You can't go further than that";

                _workspace = parent;
                return $"Successfully changed to folder \"{_workspace}\"";
            }

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

            return output + error;
            
        }
    }
}
