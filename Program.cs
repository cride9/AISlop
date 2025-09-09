using AISlop;
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

Console.Write("Task: (use --log to generate log form chat)\n");
string taskString = Console.ReadLine()!; Console.WriteLine();

StreamWriter sw = null!;
if (taskString.Contains("--log"))
{
    var fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm}-log.txt";
    sw = new StreamWriter(fileName, append: true) { AutoFlush = true };
    Console.SetOut(new MultiTextWriter(Console.Out, sw));

    taskString = taskString.Replace("--log", "");
}

var agentHandler = new AgentHandler("qwen3:30b-a3b-instruct-2507-q4_K_M");
await agentHandler.RunAsync(taskString);
