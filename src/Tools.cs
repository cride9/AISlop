﻿using QuestPDF.Fluent;
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
                "name": "directoryName",
                "cwd":"CurrentWorkingDirectory"
            }
        }
        */
        public string CreateDirectory(string name,String cwd)
        {
            string folder = Path.Combine(cwd, name);
            if (Directory.Exists(folder))
                return $"Directory already exists with name: \"{name}\"";

            var output = Directory.CreateDirectory(folder);
    
            return $"Directory created at: \"{folder}\"." + (setAsActive ? $" Current active directory: \"{folder}\"" : "");
        }
        /*
        {
            "tool": "CreateFile",
            "args": {
                "filename": "FilaName.extension",
                "content": "file content",
                "cwd":"CurrentWorkingDirectory"

            
        }
        */
        public string CreateFile(string filename, string content, String cwd)
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
        /*
        {
            "tool": "ReadFile",
            "args": {
                "filename": "FilaName.extension",
                "cwd":"CurrentWorkingDirectory"

            }
        }
        */
        public string ReadFile(string filename,String cwd)
        {
            string filePath = Path.Combine(cwd, filename);
            

            if (!File.Exists(filePath))
                return $"The file does not exists: \"{filePath}\"";

            var file = File.OpenRead(filePath);
            using StreamReader sr = new(file);

            return "File content:\n```\n" + sr.ReadToEnd().ToString() + "\n```";
        }
        /*
        {
            "tool": "OverwriteFile",
            "args": {
                "filename": "FilaName.extension",
                "insertText": "text",
                "cwd":"CurrentWorkingDirectory"

            }
        }
        */
        public string OverwriteFile(string filename, string text,String cwd)
        {
            string filePath = Path.Combine(cwd, filename);
            

            if (!File.Exists(filePath))
                return $"The file does not exists: \"{filePath}\"";

            File.Delete(filePath);
            return CreateFile(filename, text);
        }
        /*
        {
            "tool": "ListDirectory",
            "args": {
                "recursive":"IsRecursive",
                "cwd":"CurrentWorkingDirectory"

            }
        }
        */
        public string ListDirectory(bool recursive, String cwd)
        {

            //REKRÚZÍV RÉSZT NEM TELJSEN ÉRTEM HOGY ÍGY GONDOLTAD E 
            //ez passz nem tudom hogy kéne e 
            if (!Directory.Exists(cwd)) { 
                return $"The directory does not exists: \"{cwd}\"
            }

            StringBuilder resultBuilder = new StringBuilder();
            if (recursive)
            {
                string[] directoryList = Directory.GetDirectories(_workspaceRoot);
            }
            else {
                string[] directoryList = Directory.GetDirectories(cwd);
            }
            foreach (string directory in directoryList)
                {
                    resultBuilder.Append("TYPE: DIR, NAME: " + Path.GetFileName(directory) + "\n");
                    string[] fileList = Directory.GetFiles(directory);
                    foreach (string file in fileList)
                    {
                        resultBuilder.Append("TYPE: FILE, NAME: " + Path.GetFileName(file) + "\n");
                    }
                }
            return resultBuilder.ToString();
        }
        
            
       

        /*
        {
            "tool": "Changedirectory",
            "args": {
                path": "path",
               "cwd":"CurrentWorkingDirectory"

            }
        }
        */
        public String Changedirectory(String path,String cwd) {
            if (path == cwd) { 
                    return $"Alredy in \"{path}\"";
            }
            if (!Directory.Exists(path)) {
                //ha nincs még akkor nem jobb hogy ha meghívja a createt?
                return $"Not exits: \"{path}\""
            }
            if (path.StartsWith("/")) {
                cwd = Path.Combine(_workspaceRoot, path)
            }
            else {
                cwd = Path.Combine(cwd,path)
                //bizonytalan vagyok ebbe 
            return $"Successfully changed to directory \"{path}\"";
        }




        /*
        {
            "tool": "ReadTextFromPDF",
            "args": {
                "filename": "filename",
                "cwd":"CurrentWorkingDirectory"

            }
        }
        */
        public string ReadTextFromPDF(string filename,String cwd)
        {
            var filePath = Path.Combine(cwd, filename);
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
                "command": "command",
                "cwd":"CurrentWorkingDirectory"

            }
        }
        */
        public string ExecuteTerminal(string command,String cwd)
        {
            var processInfo = new ProcessStartInfo("cmd.exe", $"/c {command}")
            {
                WorkingDirectory = Path.GetFullPath(cwd),
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
                "markdownText": "markdownText",
                "cwd":"CurrentWorkingDirectory"

            }
        }
        */
        public string CreatePdfFile(string filename, string markdowntext,String cwd)
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
    }
}