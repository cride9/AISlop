using System.Text;

namespace AISlop
{
    static class Logging
    {
        public static void DisplayAgentThought(string thought, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write("Slop Agent: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{thought}\n");
        }

        public static void DisplayAgentThought(ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write("Slop Agent: ");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void DisplayToolCallUsage(string toolcall)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("Toolcall: ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"{toolcall}\n");
        }
    }

    class MultiTextWriter : TextWriter
    {
        private readonly TextWriter[] _writers;
        public MultiTextWriter(params TextWriter[] writers) => _writers = writers;
        public override Encoding Encoding => Encoding.UTF8;
        public override void Write(char value)
        {
            foreach (var w in _writers) w.Write(value);
        }
        public override void WriteLine(string? value)
        {
            foreach (var w in _writers) w.WriteLine(value);
        }
    }
}
