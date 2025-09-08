using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AISlop
{
    public class AgentHandler
    {
        private readonly Tools _tools;
        private readonly AIWrapper _agent;
        private bool _agentRunning = true;

        public AgentHandler(string modelName)
        {
            _tools = new Tools();
            _agent = new AIWrapper(modelName);
        }

        public async Task RunAsync(string initialTask)
        {
            if (string.IsNullOrWhiteSpace(initialTask))
                throw new ArgumentNullException("Task was an empty string!");

            Logging.DisplayAgentThought(ConsoleColor.Green);
            var response = await _agent.AskAi(initialTask);

            while (_agentRunning)
            {
                response = await HandleAgentResponse(response);
            }
        }

        private async Task<string> HandleAgentResponse(string response)
        {
            var toolcall = Parser.Parse(response);
            if (toolcall == null)
                return await HandleInvalidToolcall(response);

            string toolOutput = ExecuteTool(toolcall);

            if (!string.IsNullOrEmpty(toolOutput))
                Logging.DisplayToolCallUsage(toolOutput);

            if (!_agentRunning)
                return await HandleTaskCompletion(response);
            else
                return await ContinueAgent(toolOutput);
        }

        private async Task<string> HandleInvalidToolcall(string response)
        {
            int count = Regex.Matches(response, "json", RegexOptions.IgnoreCase).Count;
            Logging.DisplayToolCallUsage($"Json parser error: {count} json detected!");

            Logging.DisplayAgentThought(ConsoleColor.Green);
            return await _agent.AskAi($"Tool result: \"Json parser error: {count} json detected! Try again..\"");
        }

        private string ExecuteTool(dynamic toolcall)
        {
            string toolOutput = "";

            switch (toolcall.Tool.ToLower())
            {
                case "createdirectory":
                    toolOutput = _tools.CreateDirectory(toolcall.Args["name"], bool.Parse(toolcall.Args["setasactive"].ToLower()));
                    break;

                case "createfile":
                    toolOutput = _tools.CreateFile(toolcall.Args["filename"], toolcall.Args["content"]);
                    break;

                case "readfile":
                    toolOutput = _tools.ReadFile(toolcall.Args["filename"]);
                    break;

                case "modifyfile":
                    toolOutput = _tools.ModifyFile(toolcall.Args["filename"], toolcall.Args["overridenfilecontent"]);
                    break;

                case "getworkspaceentries":
                    toolOutput = _tools.GetWorkspaceEntries();
                    break;

                case "openfolder":
                    toolOutput = _tools.OpenFolder(toolcall.Args["folderName"]);
                    break;

                case "taskdone":
                    Logging.DisplayAgentThought(toolcall.Args["message"], ConsoleColor.Yellow);
                    _agentRunning = false;
                    break;

                case "askuser":
                    Logging.DisplayAgentThought(toolcall.Args["message"], ConsoleColor.Cyan);
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write("Response: ");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    toolOutput = Console.ReadLine()!;
                    break;

                case "readtextfrompdf":
                    toolOutput = _tools.ReadTextFromPDF(toolcall.Args["filename"]);
                    break;

                case "executeterminal":
                    toolOutput = $"Command used: {toolcall.Args["command"]}. Output: {_tools.ExecuteTerminal(toolcall.Args["command"])}";
                    break;

                case "createpdffile":
                    toolOutput = _tools.CreatePdfFile(toolcall.Args["filename"], toolcall.Args["markdowntext"]);
                    break;

                default:
                    toolOutput = "No toolcall detected!";
                    break;
            }

            return toolOutput;
        }

        private async Task<string> HandleTaskCompletion(string response)
        {
            Console.Beep();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("New task: (type \"end\" to end the process)");
            string newTask = Console.ReadLine()!;

            if (string.IsNullOrWhiteSpace(newTask))
                throw new ArgumentNullException("Task was an empty string!");
            if (newTask.ToLower() == "end")
                return response;

            Console.WriteLine();

            Logging.DisplayAgentThought(ConsoleColor.Green);
            return await _agent.AskAi($"User followup question/task: {newTask}");
        }

        private async Task<string> ContinueAgent(string toolOutput)
        {
            Logging.DisplayAgentThought(ConsoleColor.Green);
            return await _agent.AskAi(
                $"Tool result: \"{toolOutput}\"\nCWD: \"{_tools.GetCurrentWorkDirectory()}\""
            );
        }
    }
}
