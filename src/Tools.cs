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
            "tool": "Changedirectory",
            "args": {
                path": "path",
               "cwd":"CurrentWorkingDirectory"

            }
        }
        */
        public String Changedirectory(String path, string cwd) {
            if (path == cwd) {
                return $"Alredy in \"{path}\"";
            }
            if (!Directory.Exists(path)) {
                //ha nincs még akkor nem jobb hogy ha meghívja a createt?
                return $"Not exits: \"{path}\"";
            }
            if (path.StartsWith("/")) {
                cwd = Path.Combine(_workspaceRoot, path);
            }
            else {
                cwd = Path.Combine(cwd, path);
                //bizonytalan vagyok ebbe 
                 return $"Successfully changed to directory \"{path}\"";
            }
        }

    } 
}