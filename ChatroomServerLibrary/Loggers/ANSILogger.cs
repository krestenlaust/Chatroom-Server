using System;
using System.Text;

namespace ChatroomServer.Loggers
{
    public class ANSILogger : Logger
    {
        /// <inheritdoc/>
        protected override void Output(string msg, LogType logType)
        {
            StringBuilder outputSb = new StringBuilder();

            outputSb.Append(logType switch
            {
                LogType.Debug => "\033[0;37m",
                LogType.Info => "\033[0;37m",
                LogType.Warning => "\033[0;33m",
                LogType.Error => "\033[0;31m",
                _ => throw new NotImplementedException(),
            });

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
