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
            "tool": "ReadTextFromPDF",
            "args": {
                "filename": "filename",
                "cwd":"CurrentWorkingDirectory"

            }
        }
        */
        public string ReadTextFromPDF(string filename,string cwd)
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

    } 
}