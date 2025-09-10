using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AISlop
{
    public class AgentHandler
    {
        private readonly Tools _tools;
        private readonly AIWrapper _agent;
        private string _cwd = "environment";
        private bool _agentRunning = true;
        private Dictionary<string, Func<Dictionary<string, string>, string>> _toolHandler = null!;

        /// <summary>
        /// Initializes the Tools, Agent, and a ToolHandler for this instance
        /// </summary>
        /// <param name="modelName">Ollama model name</param>
        public AgentHandler(string modelName)
        {
            _tools = new();
            _agent = new(modelName);
            _toolHandler = new()
            {
                { "createdirectory", args => _tools.CreateDirectory(args["path"], false) },
                { "createfile", args => _tools.CreateFile(args["filename"], args["content"], _cwd) },
                { "readfile", args => _tools.ReadFile(args["path"]) },
                { "writefile", args => _tools.OverwriteFile(args["path"], args["content"], _cwd) },
                { "listdirectory", args => _tools.GetWorkspaceEntries() },
                { "changedirectory", args => _tools.OpenFolder(args["path"], ref _cwd) },
                { "taskdone", args => {_agentRunning = false; return _tools.TaskDone(args["message"]); } },
                { "askuser", args => _tools.AskUser(args["question"]) },
                { "readtextfrompdf", args => _tools.ReadTextFromPDF(args["path"]) },
                { "executeterminal", args => $"Command used: {args["command"]}. Output: {_tools.ExecuteTerminal(args["command"], _cwd)}" },
                { "createpdffile", args => _tools.CreatePdfFile(args["path"], args["markdown_content"], _cwd) }
            };
        }
        /// <summary>
        /// Main function of the agent. Handles the recursion
        /// </summary>
        /// <param name="initialTask">Task string, what the agent should do</param>
        /// <exception cref="ArgumentNullException">Invalid task was given</exception>
        public async Task RunAsync(string initialTask)
        {
            if (string.IsNullOrWhiteSpace(initialTask))
                throw new ArgumentNullException("Task was an empty string!");

            Logging.DisplayAgentThought(ConsoleColor.Green);
            var agentResponse = await _agent.AskAi(initialTask);

            while (_agentRunning)
                agentResponse = await HandleAgentResponse(agentResponse);
        }
        /// <summary>
        /// Handles the agents response and task phases
        /// </summary>
        /// <param name="rawResponse">Agents response (raw response)</param>
        /// <returns>API Responses</returns>
        private async Task<string> HandleAgentResponse(string rawResponse)
        {
            var parsedToolCalls = Parser.Parse(rawResponse);
            if (parsedToolCalls.Count() == 1 && !string.IsNullOrWhiteSpace(parsedToolCalls.First().Error))
                return await HandleInvalidToolcall(parsedToolCalls.First().Error);

            string toolCallOutputs = ExecuteTool(parsedToolCalls);

            if (!string.IsNullOrEmpty(toolCallOutputs))
                Logging.DisplayToolCallUsage(toolCallOutputs);

            if (!_agentRunning)
                return await HandleTaskCompletion(toolCallOutputs);
            else
                return await ContinueAgent(toolCallOutputs);
        }
        /// <summary>
        /// Gives back the toolcall exception message to the Agent
        /// </summary>
        /// <param name="toolException">Toolcall output</param>
        /// <returns>agents response</returns>
        private async Task<string> HandleInvalidToolcall(string toolException)
        {
            Logging.DisplayToolCallUsage(toolException);
            Logging.DisplayAgentThought(ConsoleColor.Green);
            return await _agent.AskAi($"Tool result: {toolException}");
        }
        /// <summary>
        /// Executes tools in order
        /// </summary>
        /// <param name="toolcalls">Tools to execute</param>
        /// <returns>Tool outputs</returns>
        private string ExecuteTool(IEnumerable<Parser.Command> toolcalls)
        {
            StringBuilder sb = new();
            foreach (var singleCall in toolcalls)
            {
                if (_toolHandler.TryGetValue(singleCall.Tool.ToLower(), out var func))
                    sb.AppendLine($"{singleCall.Tool} output: {func(singleCall.Args)}");
            }

            return sb.ToString();
        }
        /// <summary>
        /// Task ended handle. `end` ends the current chat
        /// New prompt will launch a follow up to the task it was doing before.
        /// </summary>
        /// <param name="completeMessage">Agent completition message</param>
        /// <returns>Agent response</returns>
        /// <exception cref="ArgumentNullException">No task was given</exception>
        private async Task<string> HandleTaskCompletion(string completeMessage)
        {
            Console.Beep();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("New task: (type \"end\" to end the process)");
            string newTask = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(newTask))
                throw new ArgumentNullException("Task was an empty string!");
            if (newTask.ToLower() == "end")
                return completeMessage;

            Console.WriteLine();

            Logging.DisplayAgentThought(ConsoleColor.Green);
            return await _agent.AskAi($"User followup question/task: {newTask}");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="toolOutput"></param>
        /// <returns></returns>
        private async Task<string> ContinueAgent(string toolOutput)
        {
            Logging.DisplayAgentThought(ConsoleColor.Green);
            return await _agent.AskAi(
                $"Tool result: \"{toolOutput}\"\nCWD: \"{_tools.GetCurrentWorkDirectory()}\""
            );
        }
    }
}
