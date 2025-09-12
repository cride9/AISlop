using AISlop;
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
Config.LoadConfig();

Console.Write("Task: ");
string taskString = Console.ReadLine()!; Console.WriteLine();

StreamWriter sw = null!;
if (Config.Settings.generate_log)
{
    var fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm}-log.txt";
    sw = new StreamWriter(fileName, append: true) { AutoFlush = true };
    Console.SetOut(new MultiTextWriter(Console.Out, sw));
}

int flags = 0;
if (Config.Settings.display_thought)
    flags |= (int)ProcessingState.StreamingThought;
if (Config.Settings.display_toolcall)
    flags |= (int)ProcessingState.StreamingToolCalls;

var agentHandler = new AgentHandler(Config.Settings.model_name, flags);
await agentHandler.RunAsync(taskString);
