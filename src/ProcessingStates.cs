namespace AISlop;

/// <summary>
/// Defines the states for processing the streaming response.
/// </summary>
public enum ProcessingState
{
    StreamingThought = 1 << 2,
    StreamingToolCalls = 2 << 2,
}