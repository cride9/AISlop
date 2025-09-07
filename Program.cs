using AISlop;
using System.Text.RegularExpressions;
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

Action<string, ConsoleColor> displayAgentThought = (thought, color) =>
{
    Console.ForegroundColor = color;
    Console.Write("Slop Agent: ");
    Console.ForegroundColor = ConsoleColor.White; // reset for the text
    if (!string.IsNullOrWhiteSpace(thought))
        Console.WriteLine($"{thought}\n");
};
Action<string> displayToolCallUsage = (toolcall) =>
{
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.Write("Toolcall: ");
    Console.ForegroundColor = ConsoleColor.DarkGray; // reset for the text
    Console.WriteLine($"{toolcall}\n");
};

Tools tools = new();
AIWrapper Agent = new("gemma3:4b-it-qat");

Console.Write("Task: ");
string taskString = Console.ReadLine()!;
Console.WriteLine();
if (string.IsNullOrWhiteSpace(taskString))
    throw new ArgumentNullException("Task was an empty string!");

displayAgentThought("", ConsoleColor.Green);
var response = await Agent.AskAi(taskString);
bool agentRunning = true;
while (agentRunning)
{
    var toolcall = Parser.Parse(response);
    if (toolcall == null)
    {
        int count = Regex.Matches(response, "json", RegexOptions.IgnoreCase).Count; 
        displayToolCallUsage($"Json parser error: {count} json detected!");

        displayAgentThought("", ConsoleColor.Green);
        response = await Agent.AskAi($"Tool result: \"Json parser error: {count} json detected! Try again..\"");
        continue;
    }

    string toolOutput = "";
    switch (toolcall.Tool.ToLower())
    {
        case "createdirectory":
            toolOutput = tools.CreateDirectory(toolcall.Args["name"], bool.Parse(toolcall.Args["setasactive"].ToLower()));
            break;

        case "createfile":
            toolOutput = tools.CreateFile(toolcall.Args["filename"], toolcall.Args["content"]);
            break;

        case "readfile":
            toolOutput = tools.ReadFile(toolcall.Args["filename"]);
            break;

        case "modifyfile":
            toolOutput = tools.ModifyFile(toolcall.Args["filename"], toolcall.Args["overridenfilecontent"]);
            break;

        case "getworkspaceentries":
            toolOutput = tools.GetWorkspaceEntries();
            break;

        case "openfolder":
            toolOutput = tools.OpenFolder(toolcall.Args["folderName"]);
            break;

        case "taskdone":
            displayAgentThought(toolcall.Args["message"], ConsoleColor.Yellow);
            agentRunning = false;
            break;

        case "askuser":
            displayAgentThought(toolcall.Args["message"], ConsoleColor.Cyan);
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("Response: ");
            Console.ForegroundColor = ConsoleColor.Gray;
            toolOutput = Console.ReadLine()!;
            break;

        case "readtextfrompdf":
            toolOutput = tools.ReadTextFromPDF(toolcall.Args["filename"]);
            break;

        case "executeterminal":
            toolOutput = $"Command used: {toolcall.Args["command"]}. Output: {tools.ExecuteTerminal(toolcall.Args["command"])}";
            break;

        case "createpdffile":
            toolOutput = tools.CreatePdfFile(toolcall.Args["filename"], toolcall.Args["markdowntext"]);
            break;

        default:
            toolOutput = "No toolcall detected!";
            break;
    }
    if (toolOutput != "")
        displayToolCallUsage(toolOutput);

    if (!agentRunning)
    {
        Console.Beep();
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("New task: (type \"end\" to end the process)");
        toolOutput = Console.ReadLine()!;
        if (string.IsNullOrWhiteSpace(response))
            throw new ArgumentNullException("Task was an empty string!");
        if (toolOutput.ToLower() == "end")
            break;
        Console.WriteLine();

        displayAgentThought("", ConsoleColor.Green);
        response = await Agent.AskAi($"User followup question/task: {toolOutput}");
        agentRunning = true;
    }
    else
    {

        displayAgentThought("", ConsoleColor.Green);
        response = await Agent.AskAi($"Tool result: \"{toolOutput}\"\nCWD: \"{tools.GetCurrentWorkDirectory()}\"");
    }
}