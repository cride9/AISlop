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
            "tool": "CreatePdfFile",
            "args": {
                "fileName": "fileName",
                "markdownText": "markdownText",
                "cwd":"CurrentWorkingDirectory"

            }
        }
        */
        public string CreatePdfFile(string filename, string markdowntext,string cwd)
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