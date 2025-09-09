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
            "tool": "ListDirectory",
            "args": {
                "recursive":"IsRecursive",
                "cwd":"CurrentWorkingDirectory"

            }
        }
        */
        public string ListDirectory(bool recursive, string cwd)
        {


            //REKRÚZÍV RÉSZT NEM TELJSEN ÉRTEM HOGY ÍGY GONDOLTAD E 
            if (!Directory.Exists(cwd))
            {
                return $"The directory does not exists: \"{cwd}\"";
            }

            StringBuilder resultBuilder = new StringBuilder();
            if (recursive)
            {
                string[] directoryList = Directory.GetDirectories(_workspaceRoot);
            }
            else
            {
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
    } 
}