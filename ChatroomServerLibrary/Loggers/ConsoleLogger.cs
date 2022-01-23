using System;
using System.Text;

namespace ChatroomServer.Loggers
{
    /// <summary>
    /// Uses <c>Console</c> by default.
    /// </summary>
    public class ConsoleLogger : Logger
    {
        /// <inheritdoc/>
        protected override void Output(string msg, LogType logType, (string Title, object Value)[] attributes)
        {
            StringBuilder outputSb = new StringBuilder();

            outputSb.Append('[');
            outputSb.Append(DateTime.Now.ToString("mm:ss"));
            outputSb.Append(']');

            outputSb.Append('[');
            outputSb.Append(Enum.GetName(typeof(LogType), logType));
            outputSb.Append(']');
            outputSb.Append(' ');

            foreach (var item in attributes)
            {
                outputSb.Append('[');
                outputSb.Append(item.Title);

                if (!(item.Value is null))
                {
                    outputSb.Append(": ");
                    outputSb.Append(item.Value);
                }

                outputSb.Append(']');
            }

            outputSb.Append(' ');
            outputSb.Append(msg);

            ConsoleColor currentColor = Console.ForegroundColor;
            Console.ForegroundColor = logType switch
            {
                LogType.Debug => ConsoleColor.White,
                LogType.Info => ConsoleColor.White,
                LogType.Warning => ConsoleColor.DarkYellow,
                LogType.Error => ConsoleColor.Red,
                _ => throw new NotImplementedException(),
            };

            Console.WriteLine(outputSb.ToString());
            Console.ForegroundColor = currentColor;
        }
    }
}
