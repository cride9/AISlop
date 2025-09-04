using AISlop;
using LlmTornado.Threads;

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
AIWrapper Agent = new("qwen3:4b-instruct-2507-q8_0");

Console.WriteLine("Task: ");
string taskString = Console.ReadLine()!;
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
        displayAgentThought("", ConsoleColor.Green);
        response = await Agent.AskAi("Toolcall failed. Make sure to only use 1 toolcall in each response and format them as the instructions says");
        continue;
    }

    string toolOutput = "";
    switch (toolcall.Tool.ToLower())
    {
        case "createdirectory":
            toolOutput = tools.CreateDirectory(toolcall.Args["name"]);
            break;

        case "createfile":
            toolOutput = tools.CreateFile(toolcall.Args["filename"], toolcall.Args["content"]);
            break;

        case "readfile":
            toolOutput = tools.ReadFile(toolcall.Args["filename"]);
            break;

        case "modifyfile":
            toolOutput = tools.ModifyFile(toolcall.Args["filename"], int.Parse(toolcall.Args["lineNumber"]), int.Parse(toolcall.Args["charIndex"]), toolcall.Args["insertText"]);
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
            Console.ForegroundColor = ConsoleColor.Gray;
            toolOutput = Console.ReadLine()!;
            break;

        case "readtextfrompdf":
            toolOutput = tools.ReadTextFromPDF(toolcall.Args["filename"]);
            break;

        default:
            toolOutput = "Toolcall failed. Make sure to only use 1 toolcall in each response and format them as the instructions says";
            break;
    }
    displayToolCallUsage(toolOutput);

    if (!agentRunning)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("New task: (type \"end\" to end the process)");
        toolOutput = Console.ReadLine()!;
        if (string.IsNullOrWhiteSpace(response))
            throw new ArgumentNullException("Task was an empty string!");

        if (toolOutput.ToLower() == "end")
            break;

        displayAgentThought("", ConsoleColor.Green);
        response = await Agent.AskAi($"User followup question/task: {toolOutput}");
        agentRunning = true;
    }
    else
    {

        displayAgentThought("", ConsoleColor.Green);
        response = await Agent.AskAi(toolOutput);
    }
}