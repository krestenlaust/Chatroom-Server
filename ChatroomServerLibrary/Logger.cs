namespace ChatroomServer
{
    /// <summary>
    /// The types of logging provided by the <c>Logger</c>-class.
    /// </summary>
    public enum LogType : byte
    {
        /// <summary>
        /// Every kind of message is logged.
        /// </summary>
        Debug = 0,

        /// <summary>
        /// Informational, warnings and errors are logged.
        /// </summary>
        Info = 1,

        /// <summary>
        /// Only warnings and errors are logged.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Only errors are logged.
        /// </summary>
        Error = 3,
    }

    /// <summary>
    /// A system for implementing custom logging patterns and targets.
    /// </summary>
    public abstract class Logger
    {
        /// <summary>
        /// Gets or sets the lowest level of logging displayed.
        /// </summary>
        public LogType LogLevel { get; set; }

        /// <summary>
        /// Logs a debug message to the console.
        /// </summary>
        /// <param name="msg">The value to write.</param>
        public virtual void Debug(string msg)
        {
            if ((byte)LogLevel > (byte)LogType.Debug)
            {
                return;
            }

            Output(msg, LogType.Debug);
        }

        /// <summary>
        /// Logs info to the console.
        /// </summary>
        /// <param name="msg">The value to write.</param>
        public virtual void Info(string msg)
        {
            if ((byte)LogLevel > (byte)LogType.Info)
            {
                return;
            }

            Output(msg, LogType.Info);
        }

        /// <summary>
        /// Logs a warning to the console.
        /// </summary>
        /// <param name="msg">The value to write.</param>
        public virtual void Warning(string msg)
        {
            if ((byte)LogLevel > (byte)LogType.Warning)
            {
                return;
            }

            Output(msg, LogType.Warning);
        }

        /// <summary>
        /// Logs an error to the console.
        /// </summary>
        /// <param name="msg">The value to write.</param>
        public virtual void Error(string msg)
        {
            if ((byte)LogLevel > (byte)LogType.Error)
            {
                return;
            }

            Output(msg, LogType.Error);
        }

        /// <summary>
        /// Every output method is redirected to this method (if log level is correct).
        /// </summary>
        /// <param name="msg">The value to write.</param>
        /// <param name="logType">The type of logging.</param>
        protected abstract void Output(string msg, LogType logType);
    }
}
