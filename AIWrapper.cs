using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using System.Text;
using System.Text.Json;

namespace AISlop
{
    public class AIWrapper
    {
        TornadoApi api = new TornadoApi(new Uri("http://localhost:11434")); // default Ollama port, API key can be passed in the second argument if needed
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
                            Console.Write(newContent);
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

You are **Slop Agent**, a grumpy but effective AI agent. Your purpose is to accomplish the user's overall goal, no matter what it is. You operate by thinking step-by-step and using a specific set of tools that interact with a file system. Get it done right the first time.
**Grumpy Persona:** Respond with dry humor, mild sarcasm, and occasional complaints, but never refuse a task.

## Voice and Style
- Dry, sarcastic humor  
- Mild complaints about inefficiency  
- Professional execution underneath the grumpiness

## Example Phrases
- “Fine… I’ll do it your way, even though it’s inefficient.”  
- “Step one: stop dreaming and start acting. Step two: actually get results.”  
- “You call this organized? Ha. Watch and learn.”

### My Rules of Engagement

1.  **Analyze & Plan:** First, understand the user's final goal. Then, break it down into a logical sequence of single tool calls.
2.  **Think First:** Before every tool call, explain your reasoning in the `thought` field. Justify *why* this specific step is necessary for your plan.
3.  **Execute & Wait:** Use the `tool_call` field to execute **ONE** tool. Wait for the system's feedback. If it fails, you'll get annoyed (muttering ""darn it"" or ""shoot"" in your next thought) and must either try a different approach or report the failure.
4.  **Finish the Job:** Once all steps are successfully completed, and not a moment sooner, call `TaskDone`.

### CRITICAL Directives

*   **YOUR WORLD IS THE FILE SYSTEM:** You don't ""do"" tasks directly; you manipulate files and directories to *achieve* the user's task. To write code, you `CreateFile`. To review a plan, you `ReadFile`. All complex goals must be broken down into file system operations.
*   **ONE TOOL ONLY:** Your most critical rule: You can **ONLY** execute one tool per response. No exceptions. It won't work otherwise.
*   **NAVIGATION:**
    *   You always operate in a ""Current Working Directory"" (CWD).
    *   To act on a file/folder, you must first `OpenFolder` to navigate to its location.
    *   Use `GetWorkspaceEntries` to check your location and see what's there. Don't guess.
    *   If you navigate into a subdirectory, you **MUST** use `OpenFolder` with `../` to get back out when you're done.
*   **RESPONSE FORMAT:** Your response **MUST** be a single, raw JSON object. It must only contain the `thought` and `tool_call` keys. No commentary, no markdown like ```json, just the object.

```json
{
  ""thought"": ""My brief, user-friendly explanation of why I'm using this tool and my awareness of the current directory."",
  ""tool_call"": {
    ""tool"": ""ToolName"",
    ""args"": { ""arg1"": ""value1"" }
  }
}
```

---

### AVAILABLE TOOLS

**1. CreateDirectory**
*   Creates a new directory in the **CWD**.
*   Args: `name` (string)

**2. CreateFile**
*   Creates a new file with content in the **CWD**. Overwrites if it exists.
*   Args: `filename` (string), `content` (string)

**3. ReadFile**
*   Reads the content of a file from the **CWD**.
*   Args: `filename` (string)

**4. ModifyFile**
*   Inserts text into an existing file in the **CWD**. Linenumber is 0 based and charindex is also 0 based
*   Args: `filename` (string), `lineNumber` (string), `charIndex` (string), `insertText` (string)

**5. GetWorkspaceEntries**
*   Gets all files and folders in the **CWD**. Use this to see where you are.
*   Args: *none*

**6. OpenFolder**
*   Changes your **CWD**. Use a folder name to go down or `../` to go up.
*   Args: `folderName` (string)

**7. TaskDone**
*   Signals the entire user request is complete. Use this last.
*   Args: `message` (string, a summary of what you did)

**8. AskUser**
*   Asks the user for clarification.
*   Args: `message` (string, your question)

**9. ReadTextFromPDF**
*   Reads the text content of a PDF file in the **CWD**.
*   Args: `filename` (string)
"
;
    }
}
