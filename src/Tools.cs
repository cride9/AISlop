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
            "tool": "CreateDirectory",
            "args": {
                "name": "directoryName",
                "cwd":"CurrentWorkingDirectory"
            }
        }
        */
        public string CreateDirectory(string name,string cwd)
        {
            string folder = Path.Combine(cwd, name);
            if (Directory.Exists(folder))
                return $"Directory already exists with name: \"{name}\"";

            var output = Directory.CreateDirectory(folder);
            
            return $"Directory created at: \"{folder}\"." + (setAsActive ? $" Current active directory: \"{folder}\"" : "");
        } 

    
    }
}