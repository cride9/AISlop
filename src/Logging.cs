namespace AISlop
{
    static class Logging
    {
        public static void DisplayAgentThought(string thought, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write("Slop Agent: ");
            Console.ForegroundColor = ConsoleColor.White; // reset for the text
            Console.WriteLine($"{thought}\n");
        }

        public static void DisplayAgentThought(ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write("Slop Agent: ");
            Console.ForegroundColor = ConsoleColor.White; // reset for the text
        }

        public static void DisplayToolCallUsage(string toolcall)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("Toolcall: ");
            Console.ForegroundColor = ConsoleColor.DarkGray; // reset for the text
            Console.WriteLine($"{toolcall}\n");
        }
    }
}
