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
        protected override void Output(string msg, LogType logType)
        {
            Console.ForegroundColor = logType switch
            {
                LogType.Debug => ConsoleColor.White,
                LogType.Info => ConsoleColor.White,
                LogType.Warning => ConsoleColor.DarkYellow,
                LogType.Error => ConsoleColor.Red,
                _ => throw new NotImplementedException(),
            };

            StringBuilder outputSb = new StringBuilder();

            outputSb.Append('[');
            outputSb.Append(DateTime.Now.ToString("mm:ss"));
            outputSb.Append(']');

            outputSb.Append('[');
            outputSb.Append(Enum.GetName(typeof(LogType), logType));
            outputSb.Append(']');

            outputSb.Append(' ');
            outputSb.Append(msg);

            Console.WriteLine(outputSb.ToString());
        }
    }
}
