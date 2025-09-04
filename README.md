# AI Slop - A C# AI Agent for Local File System Management

AI Slop is a simple yet powerful C# console application that demonstrates how to build an AI agent capable of interacting with your local file system. It uses a local Large Language Model (LLM) via [Ollama](https://ollama.com/) to understand natural language commands and translate them into a series of file and directory operations.

The agent operates on a "think-act" loop: it first reasons about the user's request, then selects a single tool to execute, receives feedback, and continues until the task is complete.

## ✨ Features

-   **Natural Language Interface**: Manage your files and directories by simply telling the AI what you want to do (e.g., "create a new project folder called 'WebApp' and add an `index.html` file inside it").
-   **Local First**: Powered by Ollama, allowing you to use powerful open-source models running entirely on your own machine. No API keys or internet connection required (after model download).
-   **Step-by-Step Reasoning**: The agent outputs its "thought process" before each action, providing transparency into its plan.
-   **Extensible Toolset**: Easily add new C# methods to the `Tools.cs` class to expand the agent's capabilities.
-   **Interactive Console**: Engage in a back-and-forth conversation with the agent, providing clarification or new tasks.

## ⚙️ How It Works

The project follows a classic agentic loop, orchestrated by the main `Program.cs` file:

1.  **User Input**: You provide a high-level task (e.g., "Create a file named notes.txt and write 'Hello World' in it.").
2.  **LLM Prompting**: The task is sent to the LLM along with a detailed system prompt. This prompt instructs the AI to act as a "File System Assistant" and to respond in a specific JSON format containing its `thought` and a `tool_call`.
3.  **JSON Parsing**: The application parses the LLM's JSON response to extract the chosen tool and its arguments.
4.  **Tool Execution**: The corresponding C# method in `Tools.cs` is executed (e.g., `CreateFile("notes.txt", "Hello World")`).
5.  **Feedback Loop**: The result of the tool execution (e.g., "Success: File created.") is sent back to the LLM as context for its next step.
6.  **Iteration**: The LLM uses the feedback to continue its plan, selecting the next tool until it determines the task is finished and calls the `TaskDone` tool.



## 🛠️ Available Tools

The agent's capabilities are defined by the tools listed in its system prompt. Here is the current set:

| Tool                  | Description                                                                                              | Arguments                                                                     |
| --------------------- | -------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| `CreateDirectory`     | Creates a new directory in the current working directory.                                                | `name`: The name of the directory.                                            |
| `CreateFile`          | Creates a new file with content. Overwrites if it exists.                                                | `filename`, `content`: The name and text content of the file.                 |
| `ReadFile`            | Reads the entire content of a file.                                                                      | `filename`: The name of the file to read.                                     |
| `ModifyFile`          | Inserts text into an existing file at a specific line and character position.                            | `filename`, `lineNumber`, `charIndex`, `insertText`                           |
| `GetWorkspaceEntries` | Lists all files and folders in the current working directory.                                            | _(none)_                                                                      |
| `OpenFolder`          | Changes the current working directory. Use `../` to navigate up.                                         | `folderName`: The folder to navigate into.                                    |
| `ReadTextFromPDF`     | Reads the text content of a PDF file.                                                                    | `filename`: The name of the PDF file to read.                                 |
| `TaskDone`            | Signals that the entire user request has been successfully completed.                                    | `message`: A summary of what was accomplished.                                |
| `AskUser`             | Pauses execution and asks the user for clarification if the request is ambiguous.                        | `message`: The question to ask the user.                                      |

## 🚀 Getting Started

### Prerequisites

1.  **.NET 8 SDK**: Ensure you have the .NET 8 SDK or later installed.
2.  **Ollama**: You must have [Ollama](https://ollama.com/) installed and running.
3.  **An Ollama Model**: You need a model capable of function/tool calling. This project was developed with `qwen3-coder:30b-a3b-q4_K_M`, but other models like `llama3`, `codellama`, or other variants of `qwen` should also work well.

    Pull the recommended model with:
    ```bash
    ollama pull qwen3-coder:30b-a3b-q4_K_M
    ```

### Installation & Running

1.  **Clone the repository:**
    ```bash
    git clone <repository-url>
    cd AISlop
    ```

2.  **Configure the Agent (Optional):**
    Open `AIWrapper.cs`. By default, it connects to Ollama at `http://localhost:11434` and uses the `qwen3-coder:30b-a3b-q4_K_M` model. You can change these values if your setup is different.

    ```csharp
    // in AIWrapper.cs
    public class AIWrapper
    {
        // Change the URL if your Ollama instance is hosted elsewhere
        TornadoApi api = new TornadoApi(new Uri("http://localhost:11434"));
        
        public AIWrapper(string model)
        {
            // Change the model name here or at the instantiation in Program.cs
            _conversation = api.Chat.CreateConversation(new ChatModel(model));
            _conversation.AddSystemMessage(_systemInstructions);
        }
        // ...
    }
    ```

3.  **Run the application:**
    ```bash
    dotnet run
    ```

### Usage Example

Once the application is running, it will prompt you for a task.

```
Task:
> create a python project folder named 'hello-world', and inside it, create a file app.py that prints 'Hello from AI Slop!'
```

The agent will then begin its work, showing you its thoughts and actions.

```
Agent: I need to create the main directory for the project first.

Agent: Now that I've created the 'hello-world' directory, I need to navigate into it before creating the Python file.

Agent: I am inside the 'hello-world' directory. Now I will create the app.py file with the requested content.

Agent: I have successfully created the project structure and the initial Python file as requested.
```

After the task is done, you can inspect the newly created `./workspace/hello-world/app.py` file. The application will then prompt you for a new task. Type `end` to exit.

## 📦 Dependencies

This project relies on the excellent [LlmTornado](https://github.com/tryAGI/LlmTornado) library for communicating with the Ollama API.

## 🤝 Contributing

Contributions are welcome! Feel free to open an issue to report a bug, suggest a feature, or submit a pull request to add new tools or improve the agent's logic.

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.