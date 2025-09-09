# Planned Improvements for Slop AI Agent

## 1. Executive Summary

The current agent design is a strong foundation with a clear, strict workflow. However, as identified, its rigidity and the complexity of its tools create a high cognitive load for a small language model (LLM) like a 4B parameter model. These models excel at simple, atomic tasks but struggle with multi-step reasoning, complex state management, and remembering abstract rules.

The core theme of these proposed improvements is **Simplicity, Predictability, and Explicitness**. We will reduce the number of "magic" behaviors, make tool actions more atomic, and shift state management from hidden internal variables to an explicit part of the agent loop. This will make the agent easier to debug and more reliable, as the LLM will have fewer complex rules to follow and will be less likely to make incorrect assumptions.

---

## 2. System Prompt & Agent Workflow Improvements

The instructions are the primary interface to the LLM. Simplifying them and aligning them with how small models "think" is the highest-leverage change.

### 2.1. Allow Multiple Tool Calls (High Priority)
- **Problem:** The current rule of **"Strictly ONE tool call per response"** is a major bottleneck. It forces the agent into a slow, multi-turn loop for simple sequences like "check if a file exists, then read it." A small model can easily lose context between these turns.
- **Proposed Solution:** Modify the JSON output format to accept an array of tool calls.
    ```json
    {
        "thought": "Okay, I need to read the plan and then list the files to see what's next. I can do both at once.",
        "tool_calls": [
            {
                "tool": "ReadFile",
                "args": { "filename": "plan.md" }
            },
            {
                "tool": "ListDirectory",
                "args": { "recursive": "false" }
            }
        ]
    }
    ```
- **Benefit:** This allows the agent to batch related, non-conflicting actions (especially read-only actions), significantly speeding up the "Discovery" phase and reducing the chance of context loss.

### 2.2. Simplify Navigation and Path Handling
- **Problem:** The current `OpenFolder` tool is confusing. It has special logic for the "magic string" `workspace` and tries to guess the user's intent. This ambiguity is a classic failure point for agents.
- **Proposed Solution:**
    1.  Replace `OpenFolder` with a more standard `ChangeDirectory` tool.
    2.  Introduce standard path conventions in the prompt:
        -   The workspace root is `/`.
        -   Paths can be relative (`./subfolder`, `../`) or absolute from the workspace root (`/project/src`).
    3.  The agent's CWD (Current Working Directory) must be explicitly passed back to it in every prompt from the orchestrator.
        -   Example prompt addition: `Tool result: "Directory created."\nYour CWD is: "/ProjectX/src"`
- **Benefit:** The model no longer has to guess or remember complex rules. It sees its current location and can formulate a standard, predictable path for its next action.

### 2.3. Relax the "Mandatory `plan.md`" Rule
- **Problem:** Forcing the creation of `plan.md` for *any* multi-step task is too rigid. A small model might correctly identify a simple two-step task (e.g., `CreateDirectory`, `CreateFile`) and can execute it directly. Forcing a plan adds unnecessary latency and another potential point of failure.
- **Proposed Solution:** Change the instruction from "your first output MUST be the creation of `plan.md`" to "For complex tasks, you SHOULD first create a `plan.md` file to outline your steps."
- **Benefit:** Gives the model more autonomy. It can choose to plan when necessary but can also proceed directly for simple requests, improving efficiency.

### 2.4. Remove the "Bulk Creation via Terminal" Rule
- **Problem:** Requiring the use of `ExecuteTerminal` with complex commands like `mkdir folder1\subfolder folder2\subfolder` is asking too much of a 4B model. It is very likely to get the command syntax wrong, especially for cross-platform compatibility.
- **Proposed Solution:** Remove this rule entirely. If you implement **Proposal 2.1 (Multiple Tool Calls)**, the agent can achieve the same result with a more reliable and explicit series of `CreateDirectory` calls in a single turn.
    ```json
    "tool_calls": [
        { "tool": "CreateDirectory", "args": { "path": "folder1/subfolder" } },
        { "tool": "CreateDirectory", "args": { "path": "folder2/subfolder" } }
    ]
    ```
- **Benefit:** The agent uses tools it understands well, leading to a much higher success rate than crafting complex shell commands.

---

## 3. Tool Design & Implementation Improvements (`Tools.cs`)

The C# tool implementations contain hidden state and side effects that make the system unpredictable. We will refactor them to be **stateless and atomic**.

### 3.1. Make the `Tools` Class Stateless (Highest Priority)
- **Problem:** The `Tools` class manages the CWD (`_workspace`) and the plan file path (`_workspacePlan`) internally. This is dangerous because the agent's state is hidden from the main loop. It makes debugging difficult and prevents the system from being thread-safe or supporting multiple agents.
- **Proposed Solution:**
    1.  Remove the `_workspace`, `_workspaceRoot`, and `_workspacePlan` fields from the `Tools` class.
    2.  Modify every tool method to accept the `currentWorkingDirectory` as a parameter.
        -   `public string CreateFile(string cwd, string filename, string content)`
        -   `public string ListDirectory(string cwd, bool recursive)`
    3.  The main loop in `Program.cs` will be responsible for tracking the CWD and passing it to the tools.
- **Benefit:** Creates a clean separation of concerns. The `Tools` class is just a collection of functions, and the `Program.cs` is the state machine. The system becomes vastly more predictable and testable.

### 3.2. Refactor and Simplify Tools
- **`CreateDirectory(string name, bool setAsActive)`**
    - **Problem:** The `setAsActive` parameter is a side effect. A tool should do one thing.
    - **Solution:** Remove the `setAsActive` parameter. The tool should only create a directory. If the agent wants to move into it, it must make a separate, explicit call to `ChangeDirectory`.
- **`ModifyFile(string filename, string text)`**
    - **Problem:** The name is misleading. The implementation is a destructive `Delete` then `Create`.
    - **Solution:** Rename the tool to `WriteFile` or `OverwriteFile` to make its behavior explicit. This prevents the model from assuming it can perform a partial update.
        -   `public string WriteFile(string cwd, string filename, string content)`
- **`OpenFolder(string folderName)`**
    - **Problem:** Ambiguous behavior and magic strings, as discussed above.
    - **Solution:** Replace it with `public (string newCwd, string message) ChangeDirectory(string currentCwd, string targetPath)`. This tool's job is to calculate the new valid CWD and return it to the main loop to update the state. It should handle `.` and `..` and prevent escaping the workspace root.
- **`GetWorkspaceEntries()`**
    - **Problem:** Relies on a Windows-specific shell command (`tree /f`) which produces unstructured output.
    - **Solution:** Rename to `ListDirectory` and implement it using `Directory.GetFiles()` and `Directory.GetDirectories()`. The output should be a clean, structured list. Add a `recursive` boolean parameter.
        -   `public string ListDirectory(string cwd, bool recursive)`
        -   **Output:** `TYPE: DIR, NAME: folder1\nTYPE: FILE, NAME: readme.md`
- **Remove Special `plan.md` Logic**
    - **Problem:** The special handling of `plan.md` in `CreateFile`, `ReadFile`, and `ModifyFile` is "magic" behavior that will confuse the LLM.
    - **Solution:** Remove all `if (filename.ToLower().Contains("plan"))` blocks. The agent should be responsible for providing the correct path to the plan file just like any other file.

---

## 4. Orchestrator & Parser Improvements (`Program.cs` & `Parser.cs`)

The main loop can be improved to provide better feedback to the LLM and handle tool execution more robustly.

### 4.1. Explicit State Management in the Main Loop
- **Problem:** Consequence of the stateless `Tools` class. The state needs a new home.
- **Proposed Solution:** In `Program.cs`, create a `string currentWorkingDirectory` variable, initialize it to the workspace root, and update it based on the output from the `ChangeDirectory` tool. Pass this variable into every tool call.
- **Benefit:** The state of the agent is now explicit and easy to log and debug.

### 4.2. Robust Argument Handling
- **Problem:** The current `switch` statement accesses `toolcall.Args["key"]` directly. If the LLM forgets to provide an argument, this will crash the program with a `KeyNotFoundException`.
- **Proposed Solution:** Before calling a tool, validate that all required arguments are present in the `Args` dictionary. If an argument is missing, return a specific, actionable error message to the LLM.
    -   **Example Error:** `Tool result: "Error: Missing required argument 'filename' for tool 'CreateFile'."`
- **Benefit:** Allows the LLM to self-correct. It sees exactly what it did wrong and can retry the call with the correct arguments in the next turn.

### 4.3. More Descriptive JSON Parsing Errors
- **Problem:** The current JSON error message is generic: `"Json parser error: {count} json detected!"`.
- **Proposed Solution:** Provide more specific feedback.
    -   If `ExtractJson` finds multiple *different* JSON objects, the error should be: `"Your output contained multiple, non-identical JSON objects. You must only output a single JSON response."`
    -   If `JsonSerializer.Deserialize` fails, wrap it in a `try-catch` and forward the `JsonException.Message` to the LLM. Example: `"Your JSON is invalid. Parser error: 'Expected a quote '\"' but found a '}'.'"`.
- **Benefit:** Gives the model precise information to fix its own syntax errors.