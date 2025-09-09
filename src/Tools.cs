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
        public string OverwriteFile(string filename, string text,string cwd)
        {
            string filePath = Path.Combine(cwd, filename);
            

            if (!File.Exists(filePath))
                return $"The file does not exists: \"{filePath}\"";

            File.Delete(filePath);
            return CreateFile(filename, text);
        }
    }
}