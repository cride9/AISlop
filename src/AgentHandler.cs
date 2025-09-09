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
        private Dictionary<string, Func<Dictionary<string, string>, string>> _toolHandler = null!;

        public AgentHandler(string modelName)
        {
            _tools = new();
            _agent = new(modelName);
            _toolHandler = new()
            {
                { "createdirectory", args => _tools.CreateDirectory(args["name"], bool.Parse(args["setasactive"])) },
                { "createfile", args => _tools.CreateFile(args["filename"], args["content"]) },
                { "readfile", args => _tools.ReadFile(args["filename"]) },
                { "modifyfile", args => _tools.ModifyFile(args["filename"], args["overridenfilecontent"]) },
                { "getworkspaceentries", args => _tools.GetWorkspaceEntries() },
                { "openfolder", args => _tools.OpenFolder(args["folderName"]) },
                { "taskdone", args => {_agentRunning = false; return _tools.TaskDone(args["message"]); } },
                { "askuser", args => _tools.AskUser(args["message"]) },
                { "readtextfrompdf", args => _tools.ReadTextFromPDF(args["filename"]) },
                { "executeterminal", args => $"Command used: {args["command"]}. Output: {_tools.ExecuteTerminal(args["command"])}" },
                { "createpdffile", args => _tools.CreatePdfFile(args["filename"], args["markdowntext"]) }
            };
        }

        public async Task RunAsync(string initialTask)
        {
            if (string.IsNullOrWhiteSpace(initialTask))
                throw new ArgumentNullException("Task was an empty string!");

            Logging.DisplayAgentThought(ConsoleColor.Green);
            var response = await _agent.AskAi(initialTask);

            while (_agentRunning)
                response = await HandleAgentResponse(response);
        }

        private async Task<string> HandleAgentResponse(string response)
        {
            var toolcall = Parser.Parse(response);
            if (toolcall.Count() == 1 && !string.IsNullOrWhiteSpace(toolcall.First().Error))
                return await HandleInvalidToolcall(toolcall.First().Error);

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
            Logging.DisplayToolCallUsage(response);
            Logging.DisplayAgentThought(ConsoleColor.Green);
            return await _agent.AskAi($"Tool result: {response}");
        }

        private string ExecuteTool(IEnumerable<Parser.Command> toolcall)
        {
            foreach (var singleCall in toolcall)
            {
                if (_toolHandler.TryGetValue(singleCall.Tool, out var func))
                    return func(singleCall.Args);
            }

            return null!;
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
