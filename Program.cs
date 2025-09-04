using AISlop;
using LlmTornado.Threads;

Action<string, ConsoleColor> displayAgentThought = (thought, color) =>
{
    Console.ForegroundColor = color;
    Console.Write("Agent: ");
    Console.ForegroundColor = ConsoleColor.White; // reset for the text
    Console.WriteLine($"{thought}\n");
};

Tools tools = new();
AIWrapper Agent = new("qwen3-coder:30b-a3b-q4_K_M");

Console.WriteLine("Task: ");
string taskString = Console.ReadLine()!;
if (string.IsNullOrWhiteSpace(taskString))
    throw new ArgumentNullException("Task was an empty string!");

var response = await Agent.AskAi(taskString);
bool agentRunning = true;
while (agentRunning)
{
    var toolcall = Parser.Parse(response);
    if (toolcall == null)
    {
        response = await Agent.AskAi("continue");
        continue;
    }

    string toolOutput = "";
    displayAgentThought(toolcall.Thought, ConsoleColor.Green);
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
    }
    if (!agentRunning)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("New task: (type \"end\" to end the process)");
        toolOutput = Console.ReadLine()!;
        if (string.IsNullOrWhiteSpace(response))
            throw new ArgumentNullException("Task was an empty string!");

        if (toolOutput.ToLower() == "end")
            break;

        response = await Agent.AskAi($"User followup question/task: {toolOutput}");
        agentRunning = true;
    }
    else
    {

        response = await Agent.AskAi(toolOutput);
    }
}