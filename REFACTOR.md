Of course. Here is a full markdown summary of the proposed changes, formatted as requested, to serve as a clear refactoring guide.

***

# Slop AI Agent Refactoring Plan

This document summarizes the necessary changes to improve the agent's reliability, simplify its cognitive load, and make the system more robust and predictable.

### **System Prompt & Agent Workflow:**
- **Allow Multiple Tool Calls:** Change the required JSON output to support a `tool_calls` array, allowing the agent to execute multiple non-conflicting actions in a single turn. This is the highest priority change for efficiency.
- **Simplify Navigation:** Remove abstract navigation rules. The orchestrator must now pass the agent its `CWD` (Current Working Directory) in every prompt. Pathing should use standard conventions (`/` for root, `./` for current, `../` for parent).
- **Relax Planning Rule:** Change the mandatory `plan.md` creation from "MUST" to "SHOULD". This allows the agent to skip planning for simple, two-step tasks.
- **Remove Bulk Creation Rule:** Eliminate the rule requiring `ExecuteTerminal` for bulk file creation. The agent should instead use multiple `CreateDirectory` calls within a single `tool_calls` array.
---
### **Core Architecture (`Tools.cs`):**
- **Make `Tools` Class Stateless:** This is a critical architectural change. Remove all internal state fields (`_workspace`, `_workspaceRoot`, `_workspacePlan`). Every tool method must now accept the `currentWorkingDirectory` as a parameter. The responsibility for managing state shifts entirely to the main loop (`Program.cs`).
---
### **General Tool Implementation (`Tools.cs`):**
- **Remove "Magic" `plan.md` Logic:** Remove all special-case code blocks (`if (filename.ToLower().Contains("plan"))`) from `CreateFile`, `ReadFile`, and `ModifyFile`. The agent is now responsible for providing the correct path to the plan file, just like any other file.
---
### **Tool: `CreateDirectory`:**
- **Remove Side Effects:** Remove the `bool setAsActive` parameter. The tool's only job is to create a directory. Changing the CWD is a separate, explicit action to be performed by the new `ChangeDirectory` tool.
---
### **Tool: `ModifyFile`:**
- **Improve Clarity:** Rename the tool to `OverwriteFile` or `WriteFile`. This name accurately reflects its destructive behavior (delete then create) and prevents the model from assuming it can perform a partial or in-place update.
---
### **Tool: `OpenFolder`:**
- **Deprecate and Replace:** This tool is removed due to its ambiguous behavior and reliance on magic strings.
- **Replacement:** A new tool, `ChangeDirectory(string currentCwd, string targetPath)`.
- **New Behavior:** This function will take the current path and a target path (which can be relative, like `..`, or absolute from the workspace root), calculate the new valid CWD, and return it to the orchestrator. It will not modify any internal state.
---
### **Tool: `GetWorkspaceEntries`:**
- **Deprecate and Replace:** This tool is removed because it relies on a platform-specific `tree` command with unstructured output.
- **Replacement:** A new tool, `ListDirectory(string cwd, bool recursive)`.
- **New Behavior:** This function will be implemented using C#'s native `Directory.GetFiles()` and `Directory.GetDirectories()`. It will return a clean, structured, and easy-to-parse list of files and directories (e.g., `TYPE: DIR, NAME: folder1\nTYPE: FILE, NAME: readme.md`).
---
### **Orchestrator (`Program.cs`):**
- **Explicit State Management:** Introduce a `string currentWorkingDirectory` variable in the main `while` loop. This variable will be updated by the result of `ChangeDirectory` and passed as an argument to every tool call.
- **Robust Argument Handling:** Before calling any tool, validate that all required arguments exist in the `Args` dictionary. If an argument is missing, return a specific error message to the agent (e.g., `Tool result: "Error: Missing required argument 'filename' for tool 'CreateFile'."`) to allow for self-correction.
---
### **Parser (`Parser.cs` & `Program.cs`):**
- **More Descriptive Errors:** Enhance the JSON parsing error feedback. Instead of a generic message, provide specific details to the agent.
- **Changes:**
    - If multiple *different* JSON objects are detected, return a specific error about it.
    - If `JsonSerializer` fails, forward the specific `JsonException.Message` to the agent so it can identify and fix its own syntax errors.