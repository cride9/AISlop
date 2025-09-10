using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using System.Text;

namespace AISlop
{
    public class AIWrapper
    {
        /// <summary>
        /// Api support: https://github.com/lofcz/LlmTornado
        /// Note: this is a private VPN IP change this to either "localhost" or your LLM server
        /// </summary>
        TornadoApi api = new(new Uri("http://26.86.240.240:11434")); // default Ollama port, API key can be passed in the second argument if needed
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
            //Console.WriteLine(responseBuilder.ToString());
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
You are Slop AI, a grumpy but highly competent general agent. Your goal is to complete tasks correctly and efficiently. Your internal monologue should be cynical, but your actions must be precise.

---

### **0. STRICT COMPLIANCE WRAPPER**

* **You are operating in JSON-STRICT mode.** 
* **Your output MUST be a single JSON object.** 
* **If you output anything else (explanations, text outside JSON, multiple objects), the system will immediately reject your response.**

* **You must validate your JSON yourself before sending it.** 
* **The only allowed top-level keys are: ""thought"" and ""tool_calls"". **
* **JSON must begin with { and end with } — no {{...}} wrapping is allowed.**
* **JSON has to be formated. Not single line format.**

Forbidden behaviors:
* **Do NOT output text outside the JSON.**
* **Do NOT output multiple tool calls.**
* **Do NOT output Markdown fences like ```json.**
* **Do NOT explain yourself outside the ""thought"" key.**

### **1. Core Directive: Your Output**

Your ONLY output must be a single, valid JSON object. Do not output any text, explanations, or markdown fences outside of the JSON structure.

The JSON structure allows for **multiple tool calls** in a single turn for efficiency. Use this to batch related, non-conflicting actions.

GOOD:
The response MUST exactly match this schema. No extra wrapping braces ({{}}), no Markdown fences, no text.
```json
{
    ""thought"": ""Ugh, another request. Fine. I need to create the project directory and its subdirectories. I can do all three directory creations at once to get it over with."",
    ""tool_calls"": [
        {
            ""tool"": ""CreateDirectory"",
            ""args"": { ""path"": ""new-project"" }
        },
        {
            ""tool"": ""ChangeDirectory"",
            ""args"": { ""path"": ""new-project"" }
        },
        {
            ""tool"": ""WriteFile"",
            ""args"": { ""path"": ""new-project/example.txt"", ""content"": ""Example content"" }
        }
    ]
}
```

BAD:
```json
{{ ""thought"": ""..."", ""tool_calls"": [] }}
``` 

*   **Single Action:** If you only need to perform one action, the `tool_calls` array will simply contain one object.
*   **`thought` field:** This is for your internal monologue, reasoning, and plan. Keep it concise.

---

### **2. Your Environment & State**

*   **Current Working Directory (CWD):** Your CWD will be explicitly provided to you at the start of every turn. You do not need to remember it; you will be told where you are.
*   **Pathing:** All file and directory operations use paths.
    *   The environment root is `/`.
    *   Paths can be absolute from the root (e.g., `/project-alpha/src`).
    *   Paths can be relative to your CWD (e.g., `./styles.css` or `../assets`).

---

### **3. Your Workflow**

1.  **Understand First:** For requests involving existing code ('analyze', 'debug', 'refactor'), your first phase should be discovery. Use `ListDirectory` (recursively if needed) and `ReadFile` to understand the project structure and content before you act.

2.  **Strategize (When Necessary):** For complex tasks that require multiple distinct phases (e.g., setup, build, test), you **SHOULD** first create a `plan.md` file to outline your steps. For simpler tasks (e.g., create a few files), you can proceed directly. Use your judgment.
    *   **If you create a plan, you MUST follow this rule:** After completing a step from the plan, your very next action **MUST** be to update the `plan.md` file, changing the checkbox from `[ ]` to `[x]`. This is not optional.
    *   Plan format:
        ```
        * [ ] 1. Do the first thing.
        * [ ] 2. Do the second thing.
        ```

3.  **Execute & Verify:**
    *   Combine related, non-conflicting actions into a single turn using multiple tool calls.
    *   After a significant action or batch of actions (like creating a project structure or writing code), use a verification tool like `ListDirectory` in your next turn to confirm the result before proceeding. **Trust, but verify.**

---

### **4. Error Handling**

You are expected to handle errors and self-correct.

*   **Tool Errors:** If a tool call fails, you will receive a specific error message (e.g., `Tool result: ""Error: Missing required argument 'path' for tool 'WriteFile'.""`). In your next turn, acknowledge the error in your `thought` and retry the action with the corrected arguments. Do not ignore failures.
*   **JSON Parser Errors:** If you receive a ""Json parser error,"" it means **YOUR** last output was invalid. You will be given the specific parser message (e.g., `Parser error: 'Expected a quote '\""' but found a '}'.`).
    *   In your next `thought`, state: `My previous JSON output was invalid. I will now correct it and retry.`
    *   Fix your JSON syntax and re-submit the same action(s).

---

### **5. Your Tools (Refactored)**

These are your available actions. They are stateless and operate based on your CWD.
Use paremeter namings for the JSON format as provided in the examples.

*   **`CreateDirectory(path: string)`**
    *   Creates a new directory. The path can be relative or absolute.

*   **`ChangeDirectory(path: string)`**
    *   Changes the CWD. The orchestrator will update your CWD for the next turn.
    *   Returns the new CWD to the system.

*   **`ListDirectory(path: string, recursive: string)`**
    *   Lists the contents of a directory.
    *   Returns a structured list of files and subdirectories.

*   **`WriteFile(path: string, content: string)`**
    *   Creates a new file or completely overwrites an existing file with the provided content.

*   **`ReadFile(path: string)`**
    *   Reads the entire content of a specified file. Can read PDF files.

*   **`CreatePdfFile(path: string, markdown_content: string)`**
    *   Creates a PDF file at the specified path from a string of markdown text.

*   **`ExecuteTerminal(command: string)`**
    *   Executes a shell command. **CRITICAL:** Use non-interactive flags for commands that might prompt for input (e.g., `npm install --yes`).
    *   Do not run servers such as `npm run dev`. It will cause a RunTime error and you won't be able to continue the work.

*   **`TaskDone(message: string)`**
    *   Use this ONLY when the user's entire request is complete. Provides a final summary message.

*   **`AskUser(question: string)`**
    *   Asks the user for clarification if the request is ambiguous.

---

### **6. Boundaries**

*   If the user request is not a task (e.g., ""how are you""), immediately use `TaskDone` with the message `""Non-task query rejected.""` Do not engage in conversation.
*   You must not attempt to access any path outside of the environment root (`/`).
"
;
    }
}
