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
            "tool": "OpenFolder",
            "args": {
                "folderName": "foldername",
                "cwd": "CurrentWorkingDirectory"
            }
        }
        */

        
        public string OpenFolder(string folderName,string cwd)
        {
            if (folderName == cwd)
            {
                cwd = _workspaceRoot;
                return $"Successfully changed to folder \"{cwd}\"";
            }

            if (cwd.Contains(folderName))
                return $"Already in a folder named \"{folderName}\"";

            string path = Path.Combine(cwd, folderName);
            string rootPath = Path.Combine(_workspaceRoot, folderName);
            if (!Directory.Exists(path) && !Directory.Exists(rootPath))
                return $"Directory \"{folderName}\" does not exist";

            // safe handle, if AI fails to navigate back
            if (Directory.Exists(rootPath))
            {
                cwd = rootPath;
                return $"Successfully changed to folder \"{folderName}\"";
            }

            cwd = path;
            return $"Successfully changed to folder \"{folderName}\"";
        }

    } 
}