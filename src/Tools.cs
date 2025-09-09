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
            "tool": "ReadFile",
            "args": {
                "filename": "FilaName.extension",
                "cwd":"CurrentWorkingDirectory"

            }
        }
        */
        public string ReadFile(string filename,string cwd)
        {
            string filePath = Path.Combine(cwd, filename);
            

            if (!File.Exists(filePath))
                return $"The file does not exists: \"{filePath}\"";

            var file = File.OpenRead(filePath);
            using StreamReader sr = new(file);

            return "File content:\n```\n" + sr.ReadToEnd().ToString() + "\n```";
        }
    }
}