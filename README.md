# AISlop Agent

AISlop is a grumpy but highly competent AI agent designed to efficiently complete tasks. It operates by thinking through a problem, executing a series of tool calls, and verifying the results.

## Features

- **Extensible Toolset:** Comes with a variety of built-in tools for file system operations, PDF manipulation, and terminal command execution.
- **Autonomous Operation:** Capable of running in a loop to complete complex tasks without user intervention.
- **Interactive Mode:** Allows for user clarification and follow-up tasks.
- **Logging:** Can generate a log file of the entire an operation for debugging and review.

## Getting Started

### Prerequisites

- .NET 8 SDK
- An Ollama instance with a suitable model (e.g., `qwen2:1.5b-instruct`)

### Running the Project

1. **Clone the repository:**
   ```bash
   git clone https://github.com/your-username/AISlop.git
   cd AISlop
   ```

2. **Update the model name:**
   In `Program.cs`, change `"qwen2:1.5b-instruct"` to the model you have available in Ollama.

3. **Run the application:**
   ```bash
   dotnet run
   ```

4. **Provide a task:**
   When prompted, enter the task you want the agent to perform. To generate a log file of the session, add the `--log` flag to your task.

   ```
   Task: (use --log to generate log form chat)
   Create a new directory called 'my-project', and inside it, create a file named 'index.html' with the content '<h1>Hello, World!</h1>'. --log
   ```

## Framework and Developer Docs

The agent operates on a simple loop: it receives a task, thinks about the necessary steps, executes the corresponding tools, and then evaluates the output to determine the next action. This process continues until the task is marked as complete.

### Core Components

- **AgentHandler.cs:** The main orchestrator that manages the agent's lifecycle, handles user input, and calls the AI model.
- **AIWrapper.cs:** A wrapper for the `LlmTornado` library that facilitates communication with the Ollama API. It includes the system prompt that defines the agent's personality and operational guidelines.
- **Tools.cs:** A collection of methods that the agent can call to interact with the environment. These are the "tools" that the AI model can choose from.
- **Parser.cs:** Responsible for parsing the JSON output from the AI model into a series of tool calls that the `AgentHandler` can execute.

### Available Tools

The following tools are available to the agent. They are defined in `Tools.cs` and the expected JSON format for them is specified in the `AIWrapper.cs` system prompt.

*   `CreateDirectory(path: string)`: Creates a new directory.
*   `ChangeDirectory(path: string)`: Changes the current working directory.
*   `ListDirectory(path: string, recursive: boolean = false)`: Lists the contents of a directory.
*   `WriteFile(path: string, content: string)`: Creates a new file or overwrites an existing one.
*   `ReadFile(path: string)`: Reads the content of a file.
*   `CreatePdfFile(path: string, markdown_content: string)`: Creates a PDF file from markdown text.
*   `ReadTextFromPdf(path: string)`: Reads the text content from a PDF file.
*   `ExecuteTerminal(command: string)`: Executes a shell command.
*   `TaskDone(message: string)`: Marks the current task as complete.
*   `AskUser(question: string)`: Prompts the user for clarification.