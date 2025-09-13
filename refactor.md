# Architectural Refactor: Transition to a Streaming, Event-Driven System

## 1. Overview

This document outlines the architectural refactoring of the agent's core logic. We are moving from a monolithic, synchronous response-processing model to a decoupled, streaming, and event-driven architecture.

**Primary Goals:**
-   **Enhanced User Experience:** Provide immediate, real-time feedback to the user.
-   **Efficient Resource Usage:** Stream large data payloads (like file contents) directly to their destination without buffering them entirely in memory.
-   **Future-Proofing:** Create a solid foundation for building advanced UIs (e.g., a Blazor web application) and more complex, interactive tools.

## 2. The "Before" State: Synchronous Model

The previous architecture followed a simple, blocking sequence:

1.  Send a prompt to the LLM.
2.  Wait and receive the **entire** JSON response containing all thoughts and tool calls.
3.  Parse the complete JSON string into a list of tool call objects.
4.  Iterate through the list and execute each tool call sequentially.
5.  Collect all results and send them back to the LLM in the next turn.

**Limitations:**
-   High perceived latency; the user sees nothing until the full response is generated.
-   Inefficient memory usage for tool calls with large arguments (`WriteFile`).
-   Difficult to provide real-time updates for long-running tools (`ExecuteTerminal`).

## 3. The "After" State: Streaming, Event-Driven Model

The new architecture is built on a non-blocking, asynchronous data flow.

### Core Components

1.  **Streaming JSON Parser:** A lightweight, state-machine-based parser that processes the LLM's response stream chunk-by-chunk. It doesn't need the complete document to understand the structure. As it identifies key parts of the JSON (a tool call starting, an argument's value, etc.), it fires events.

2.  **Event Bus (Publisher/Subscriber):** A central messaging system that decouples components. The parser is the primary *publisher*. Tool Handlers are the *subscribers*. This allows us to add or modify tool logic without altering the parser.

3.  **Stateful Tool Handlers:** Each tool (or group of related tools) is managed by a dedicated `Handler` class. These handlers subscribe to the events from the Event Bus and perform actions. They can be stateful (e.g., holding an open `FileStream`) to manage long-running, streaming operations.

### New Data Flow

1.  Send a prompt to the LLM and open a response stream.
2.  As chunks of data arrive, they are fed into the **Streaming JSON Parser**.
3.  The parser recognizes patterns and publishes events to the **Event Bus** (e.g., `OnToolCallStarted: "WriteFile"`, `OnArgumentChunkReceived: "content", "import os..."`).
4.  **Tool Handlers**, subscribed to these events, react in real-time. For example, the `WriteFileHandler` immediately writes the received `content` chunk to disk. The `TerminalOutputHandler` prints plain text to the console.

## 4. Tool Handler Categories

Tools are managed by handlers based on their behavior:

-   **Streamable Argument Tools (`WriteFile`, `CreatePdfFile`):** These handlers are stateful. They begin an operation when a tool call starts (e.g., opening a file) and process incoming argument chunks incrementally until the tool call is finished.

-   **Blocking Execution Tools (`ListDirectory`, `ReadFile`):** These handlers are simpler. They buffer their arguments and wait for the `OnToolCallFinished` event before executing their logic in a single, blocking action.

-   **Specialized I/O Handlers (`ExecuteTerminal`):** This handler can stream its *output*. It starts the process on `OnToolCallFinished` and then listens to the process's `stdout`/`stderr` streams, publishing new events for each line of output received.

-   **Control Flow Tools (`AskUser`, `TaskDone`):** These handlers emit high-level control events that are intercepted by the main application loop to pause, resume, or terminate the generation process.

## 5. Benefits of This Refactor

-   **Real-time UX:** Users see file contents being written and terminal commands executing as they happen.
-   **Modularity & Testability:** The parser, event bus, and each handler can be developed and tested in isolation.
-   **Scalability:** The system is better equipped to handle large outputs and a greater number of concurrent tools without running into memory limits.
-   **Foundation for Web UI:** This event-driven model maps directly to technologies like SignalR, making the transition to a responsive Blazor front-end significantly easier.