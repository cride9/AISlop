using AISlop;
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

Console.Write("Task: ");
string taskString = Console.ReadLine()!; Console.WriteLine();

var agentHandler = new AgentHandler("qwen3:4b-instruct-2507-q4_K_M");
await agentHandler.RunAsync(taskString);