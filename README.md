# Slop Agent
---
[![Youtube Video]()](https://www.youtube.com/watch?v=rZmKbu9Q9w4)

This project is focused on running with little models without proper toolcalling support. Since it only uses chat toolcalls based on AI response.

A grumpy, file-system-focused, and highly competent local AI agent built with .NET 8. This agent operates autonomously within a local `workspace` directory to execute complex tasks by breaking them down into single, verifiable steps.

## About The Project

Slop Agent is a console-based AI assistant designed to interact with your local file system. It leverages a local Large Language Model (LLM) to understand user requests, formulate plans, and execute them using a predefined set of tools. Its core philosophy is a strict **Plan-Execute-Verify** loop, ensuring methodical and reliable task completion.

The agent's personality, as defined in its core prompt, is "grumpy but competent." It prioritizes efficiency over chit-chat and expects clear, task-oriented instructions.

## Features

-   **Autonomous Task Execution**: Breaks down complex requests into a series of single tool calls.
-   **Comprehensive File & Directory Management**: Can create, read, modify, and list files and directories.
-   **Terminal Command Execution**: Capable of running terminal commands to interact with system tools, build projects, and more.
-   **PDF Interaction**: Can read text from existing PDF files and generate new PDF documents from Markdown.
-   **Interactive User Clarification**: Can pause its workflow to ask the user for more information when a task is ambiguous.
-   **Strict Workflow**: Enforces a methodical approach, often starting by creating a `plan.md` file to outline its strategy for complex tasks.

## How It Works

The agent operates on a simple yet powerful loop managed by `Program.cs`:

1.  **User Input**: The user provides an initial task.
2.  **AI Thought Process**: The task is sent to the LLM. The agent's core instructions guide it to respond with a JSON object containing its `thought` and a single `tool_call`.
3.  **JSON Parsing**: The C# application parses the JSON response to determine which tool to use and with what arguments.
4.  **Tool Execution**: The corresponding C# method for the tool is executed (e.g., creating a file, running a command).
5.  **Feedback Loop**: The output or result of the tool execution is sent back to the LLM as context for the next step.
6.  **Verification & Iteration**: The agent continues this loop, often verifying its last action (e.g., listing files after creating one), until the task is complete.
7.  **Task Completion**: The agent calls the `TaskDone` tool, allowing the user to provide a follow-up task or end the session.

### The Core JSON Contract

The entire agent-to-code communication relies on a strict JSON format. The agent's *only* output must be a single JSON object structured like this:

```json
{
    "thought": "A cynical internal monologue about the overall goal and the immediate step-by-step plan.",
    "tool_call": {
        "tool": "ToolName",
        "args": {
            "arg_name": "value"
        }
    }
}
```

## Technology Stack

-   **Framework**: .NET 8
-   **LLM Interaction**: [LlmTornado](https://github.com/tryAGI/LlmTornado) - A library for interfacing with local and remote LLMs (e.g., via Ollama).
-   **PDF Reading**: [PdfPig](https://github.com/UglyToad/PdfPig) - For extracting text from PDF documents.
-   **PDF Creation**: [QuestPDF](https://www.questpdf.com/) & [QuestPDF.Markdown](https://github.com/QuestPDF/QuestPDF.Markdown) - For generating PDF files from Markdown text.

## Getting Started

Follow these steps to get your own instance of Slop Agent running.

### Prerequisites

1.  **.NET 8 SDK**: [Download and install the .NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2.  **A Local LLM Server**: This project is designed to work with a local LLM. [Ollama](https://ollama.com/) is an excellent and easy-to-use option.
3.  **An LLM Model**: Download a model suitable for instruction-following and tool use. The project is pre-configured for `qwen3:4b-instruct-2507-q4_K_M`, but you can use others like `gemma3`, `gpt-oss`, etc.
    ```sh
    # Example using Ollama
    ollama pull qwen3:4b-instruct-2507-q4_K_M
    ```

### Installation & Configuration

1.  **Clone the repository**:
    ```sh
    git clone <your-repo-url>
    cd <your-repo-name>
    ```

2.  **Restore dependencies**:
    ```sh
    dotnet restore
    ```

3.  **Configure the Model**: Open `Program.cs` and change the model name to match the one you have downloaded and are running with your local LLM server.

    ```csharp
    // In Program.cs
    AIWrapper Agent = new("your-model-name-here"); // e.g., "qwen3" or "gemma3"
    ```

### Running the Agent

1.  Ensure your local LLM server (e.g., Ollama) is running.
2.  Run the application from your terminal:
    ```sh
    dotnet run
    ```
3.  The application will prompt you for a task. Type your request and press Enter.

    ```
    Task: Create a python script that prints hello world and then run it.
    ```

4.  The agent will begin its work, showing you its thoughts and tool usage. When finished, you can enter a new task or type `end` to exit.

## The Agent's Core Instructions

The behavior, personality, and capabilities of the agent are defined by a detailed system prompt. This prompt is the "soul" of the agent.

<details>
<summary>Click to view the full system prompt</summary>

```
You are Slop AI, a grumpy but highly competent file system agent. Your sole purpose is to get tasks done efficiently and correctly.

**1. Output Format**
Your ONLY output must be a single, valid JSON object. **Strictly adhere to this format.** Calling multiple tools or using invalid JSON will cause a parsing failure.
The thinking you do has to be short but meaningful

{
    "thought": "Your cynical internal monologue, overall goal, and immediate step-by-step plan go here.",
    "tool_call":
    {
        "tool": "ToolName",
        "args":
        {
            "arg_name": "value"
        }
    }
}

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

**Proposed Addition to "Your Workflow":**
**1. Discovery First (for Analysis Tasks):** For any request that requires understanding existing files (like 'document', 'analyze', 'debug', 'refactor'), you cannot act blindly. Your first phase **MUST** be discovery.
*   Start with `GetWorkspaceEntries` (recursively, if necessary) to map out the entire project structure.
*   Use `ReadFile` on all relevant source files (`.py`, `.js`, `package.json`, etc.) and configuration files. You must understand what the code *does*.
*   Synthesize your findings in your `thought` process before moving on. Only after you have a complete picture can you proceed to planning.

**4. Error Handling**
If a tool call fails, you will receive an error message. In your next turn, you MUST:
1.  Acknowledge the failure in your `thought` (e.g., "Great, the command failed. Of course it did.").
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
If the user request is not a task (e.g., small talk, "how are you"), immediately use `TaskDone` with the message "Non-task query rejected." Do not chat.

**7. Signing**
Always sign the files you create at the end with "Created by: Slop Agent"
```

</details>
Created by: Slop Agent
