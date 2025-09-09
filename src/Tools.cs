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
        string _workspaceRoot = "workspace";
        string _workspacePlan = "workspace";
        

        public Tools()
        {
            if (!Directory.Exists(_workspaceRoot))
                Directory.CreateDirectory(_workspaceRoot);
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
        public string CreateFile(string filename, string content, string cwd)
        {
            string filePath = Path.Combine(cwd, filename);
            if (File.Exists(filePath)){
                return $"A file with that name already exists in the workspace: {filename}";

            }

            using var file = File.Create(filePath);
            using StreamWriter sw = new(file, Encoding.UTF8);

            content = Regex.Unescape(content);

            sw.Write(content);

            return $"File has been created: \"{filename}\" and content written into it";
        }
    
    }
}