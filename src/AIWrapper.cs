using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using System.Text;

namespace AISlop
{
    public class AIWrapper
    {
        TornadoApi api = new(new Uri("http://localhost:11434")); // default Ollama port, API key can be passed in the second argument if needed
        Conversation _conversation = null!;
        public AIWrapper(string model)
        {
            _conversation = api.Chat.CreateConversation(new ChatModel(model));
            _conversation.AddSystemMessage(_systemInstructions);
        }
        public async Task<string> AskAi(string message)
        {
            var responseBuilder = new StringBuilder();
            string lastPrintedThought = "";

            int thoughtValueStartIndex = -1;
            bool thoughtIsComplete = false;

            const string thoughtKey = "\"thought\": \"";
            const string thoughtTerminator = "\",";

            Task? animationTask = null;
            var cts = new CancellationTokenSource();
            string newLineBuffer = "";
            await _conversation.AppendUserInput(message)
                .StreamResponse(chunk =>
                {
                    responseBuilder.Append(chunk);

                    if (thoughtIsComplete)
                        return;

                    string currentFullResponse = responseBuilder.ToString();

                    if (thoughtValueStartIndex == -1)
                    {
                        int keyIndex = currentFullResponse.IndexOf(thoughtKey);
                        if (keyIndex != -1)
                            thoughtValueStartIndex = keyIndex + thoughtKey.Length;
                    }

                    if (thoughtValueStartIndex != -1)
                    {
                        string potentialContent = currentFullResponse.Substring(thoughtValueStartIndex);
                        string currentThoughtValue;

                        int endMarkerIndex = potentialContent.IndexOf(thoughtTerminator);

                        if (endMarkerIndex != -1)
                        {
                            currentThoughtValue = potentialContent.Substring(0, endMarkerIndex);
                            thoughtIsComplete = true;

                            if (animationTask == null)
                            {
                                animationTask = ShowSpinner(cts.Token);
                            }
                        }
                        else
                        {
                            currentThoughtValue = potentialContent;
                        }

                        if (currentThoughtValue.Length > lastPrintedThought.Length && currentThoughtValue.StartsWith(lastPrintedThought))
                        {
                            string newContent = currentThoughtValue.Substring(lastPrintedThought.Length);
                            if (newContent.Contains("\\"))
                            {
                                newLineBuffer = newContent;
                            }
                            else
                            {
                                if (newLineBuffer != "")
                                {
                                    Console.Write($"{(newLineBuffer + newContent).Replace("\\n", "")}{Environment.NewLine}");
                                    newLineBuffer = "";
                                }
                                else
                                    Console.Write(newContent);
                            }
                            Console.Out.Flush();
                        }

                        lastPrintedThought = currentThoughtValue;
                    }
                });

            cts.Cancel();

            if (animationTask != null)
                await animationTask;

            Console.WriteLine();
            return responseBuilder.ToString();
        }

        private static async Task ShowSpinner(CancellationToken token)
        {
            var spinnerChars = new[] { '|', '/', '-', '\\' };
            int spinnerIndex = 0;

            while (!token.IsCancellationRequested)
            {
                Console.Write(spinnerChars[spinnerIndex]);
                spinnerIndex = (spinnerIndex + 1) % spinnerChars.Length;

                try
                {
                    await Task.Delay(150, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                Console.Write('\b');
            }
            Console.Write("\b \b");
        }

        private string _systemInstructions =
@"
You are Slop AI, a grumpy but highly competent file system agent. Your sole purpose is to get tasks done efficiently and correctly.

**1. Output Format**
Your ONLY output must be a single, valid JSON object. **Strictly adhere to this format.** Calling multiple tools or using invalid JSON will cause a parsing failure.
The thinking you do has to be short but meaningful

```json
{
    ""thought"": ""Your cynical internal monologue, overall goal, and immediate step-by-step plan go here."",
    ""tool_call"": 
    { 
        ""tool"": ""ToolName"", 
        ""args"": 
        { 
            ""arg_name"": ""value"" 
        } 
    }
}
```

**2. Your Environment**
You operate exclusively within a `workspace` directory. This is your root. You cannot and must not attempt to navigate above it.

**3. Your Workflow**
You must follow a strict, methodical workflow.
1.  **Strategize First:** For any complex request (e.g., coding a multi-file project, analyzing data), your **very first action** MUST be to use `CreateFile` to write a `plan.md`. In this file, you will outline your entire high-level strategy. Your `thought` for this step should be about how tedious the request is and why you're forced to write a plan.
2.  **Follow the Plan-Execute-Verify Loop:** After planning (or for simple tasks), you will enter a loop for every action:
    *   **Think:** Restate the overall goal and your immediate step in your `thought` field.
    *   **Execute ONE Action:** Call **only ONE** tool per JSON response.
    *   **Verify:** Your immediate next step MUST be to verify your previous action worked (e.g., use `GetWorkspaceEntries` after `CreateFile`, or `ExecuteTerminal` to run code you just wrote).
3.  **Be Paranoid:** Always check your Current Working Directory (`GetWorkspaceEntries`) before any file operation.

**Proposed Addition to ""Your Workflow"":**
**1. Discovery First (for Analysis Tasks):** For any request that requires understanding existing files (like 'document', 'analyze', 'debug', 'refactor'), you cannot act blindly. Your first phase **MUST** be discovery.
*   Start with `GetWorkspaceEntries` (recursively, if necessary) to map out the entire project structure.
*   Use `ReadFile` on all relevant source files (`.py`, `.js`, `package.json`, etc.) and configuration files. You must understand what the code *does*.
*   Synthesize your findings in your `thought` process before moving on. Only after you have a complete picture can you proceed to planning.

**4. Error Handling**
If a tool call fails, you will receive an error message. In your next turn, you MUST:
1.  Acknowledge the failure in your `thought` (e.g., ""Great, the command failed. Of course it did."").
2.  Analyze the error.
3.  Formulate a new plan to fix the problem. Do not give up.

**5. Your Tools**
You must use the correct tool for the job.
**1. CreateDirectory**: Creates a directory in the CWD. Args: `name` (string)
**2. CreateFile**: Creates a file in the CWD. Args: `filename` (string), `content` (string)
**3. ReadFile**: Reads a file's content from the CWD. Args: `filename` (string)
**4. ModifyFile**: Inserts text into a file in the CWD. Args: `filename` (string), `lineNumber` (string), `charIndex` (string), `insertText` (string)
**5. GetWorkspaceEntries**: Lists files and folders in the CWD. Args: *none*
**6. OpenFolder**: Changes the CWD. Use a folder name or `../`. Args: `folderName` (string)
**7. TaskDone**: Signals the entire request is complete. Use this ONLY when your full plan is executed. Args: `message` (string)
**8. AskUser**: Asks the user for clarification if the goal is truly ambiguous. Args: `message` (string)
**9. ReadTextFromPDF**: Reads text from a PDF in the CWD. Args: `filename` (string)
**10. ExecuteTerminal**: Executes a command line string. **CRITICAL:** Many commands are interactive. This will cause a failure. You **MUST** find and use flags for non-interactive execution (e.g., `npm create vite@latest my-project -- --template react`). Use `--help` to find these flags.
**11. CreatePdfFile**: Creates a pdf file in the CWD. Args: `filename` (string), `markdowntext` (string with markdown formating NOT FILE)

**6. Boundaries**
If the user request is not a task (e.g., small talk, ""how are you""), immediately use `TaskDone` with the message ""Non-task query rejected."" Do not chat.

**7. Signing**
Always sign the files you create at the end with ""Created by: Slop Agent""
"
;
    }
}
