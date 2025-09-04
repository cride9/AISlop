using System.Text;
using UglyToad.PdfPig;

namespace AISlop
{
    public class Tools
    {
        string _workspace = "workspace";
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

            var file = File.Create(filePath);
            using StreamWriter sw = new(file, Encoding.UTF8);

            // I need a better way to handle this. TODO
            content = content.Replace(@"\\n", "\n");
            content = content.Replace(@"\n", "\n");

            content = content.Replace(@"\\t", "\t");
            content = content.Replace(@"\t", "\t");

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
                sb.AppendLine($"\t{count++}. {entry}");
            }
            if (string.IsNullOrWhiteSpace(sb.ToString()))
                return $"The folder \"{_workspace}\" is empty.";

            return $"Files in folder \"{_workspace}\":\n{sb.ToString()}";
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
                return $"Already in folder \"{folderName}\"";

            if (folderName == "../")
            {
                string parent = Directory.GetParent(_workspace)?.FullName;
                if (parent == null)
                    return "Already at the root directory, cannot go up.";

                if (!parent.Contains("workspace"))
                    return "Invalid path. You can't go further than that";

                _workspace = parent;
                return $"Successfully changed to folder \"{folderName}\"";
            }

            string path = Path.Combine(_workspace, folderName);
            if (!Directory.Exists(path))
                return $"Directory \"{folderName}\" does not exist";

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
    }
}
