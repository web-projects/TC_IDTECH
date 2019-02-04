using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.LoggerManager
{
    public static class Logger
    {
        private static string dbLoggerPath = "";
        private static string fileLoggerPath = "";
        private static string eventLoggerPath = "";

        private static LogBase dbLogger = null;
        public static LogBase fileLogger = null;
        private static LogBase eventLogger = null;

        public static void SetFileLoggerName(string filepath)
        {
            fileLoggerPath = filepath;
        }

        public static void SetDBLoggerName(string filepath)
        {
            dbLoggerPath = filepath;
        }

        public static void SetEventLoggerName(string filepath)
        {
            eventLoggerPath = filepath;
        }

        // DEBUG LOGGING
        public static void debug(string message, LogTarget target = LogTarget.File)
        {
            switch(target)
            {
                case LogTarget.File:
                {
                    if(fileLoggerPath != string.Empty)
                    {
                        if(fileLogger == null)
                        {
                            fileLogger = new FileLogger(fileLoggerPath);
                        }
                        fileLogger.debug(message);
                    }
                    break;
                }
                case LogTarget.Database:
                {
                    if(dbLoggerPath != string.Empty)
                    {
                        if(dbLogger == null)
                        {
                            dbLogger = new DBLogger(dbLoggerPath);
                        }
                        dbLogger.debug(message);
                    }
                    break;
                }
                case LogTarget.EventLog:
                {
                    if(eventLoggerPath != string.Empty)
                    {
                        if(eventLogger == null)
                        {
                            eventLogger = new EventLogger(eventLoggerPath);
                        }
                        eventLogger.debug(message);
                    }
                    break;
                }
            }
        }
        public static void debug(string message, object arg1, LogTarget target = LogTarget.File)
        {
            string payload = String.Format(message, arg1);
            debug(payload, target);
        }
        public static void debug(string message, object arg1, object arg2, LogTarget target = LogTarget.File)
        {
            string payload = String.Format(message, arg1, arg2);
            debug(payload, target);
        }
        // INFO LOGGING
        public static void info(string message, LogTarget target = LogTarget.File)
        {
            switch(target)
            {
                case LogTarget.File:
                {
                    if(fileLoggerPath != string.Empty)
                    {
                        if(fileLogger == null)
                        {
                            fileLogger = new FileLogger(fileLoggerPath);
                        }
                        fileLogger.info(message);
                    }
                    break;
                }
                case LogTarget.Database:
                {
                    if(dbLoggerPath != string.Empty)
                    {
                        if(dbLogger == null)
                        {
                            dbLogger = new DBLogger(dbLoggerPath);
                        }
                        dbLogger.info(message);
                    }
                    break;
                }
                case LogTarget.EventLog:
                {
                    if(eventLoggerPath != string.Empty)
                    {
                        if(eventLogger == null)
                        {
                            eventLogger = new EventLogger(eventLoggerPath);
                        }
                        eventLogger.info(message);
                    }
                    break;
                }
            }
        }

        public static void info(string message, object arg1, LogTarget target = LogTarget.File)
        {
            string payload = String.Format(message, arg1);
            info(payload, target);
        }
        public static void info(string message, object arg1, object arg2, LogTarget target = LogTarget.File)
        {
            string payload = String.Format(message, arg1, arg2);
            info(payload, target);
        }
        // WARNING LOGGING
        public static void warning(string message, LogTarget target = LogTarget.File)
        {
            switch(target)
            {
                case LogTarget.File:
                {
                    if(fileLoggerPath != string.Empty)
                    {
                        if(fileLogger == null)
                        {
                            fileLogger = new FileLogger(fileLoggerPath);
                        }
                        fileLogger.warning(message);
                    }
                    break;
                }
                case LogTarget.Database:
                {
                    if(dbLoggerPath != string.Empty)
                    {
                        if(dbLogger == null)
                        {
                            dbLogger = new DBLogger(dbLoggerPath);
                        }
                        dbLogger.warning(message);
                    }
                    break;
                }
                case LogTarget.EventLog:
                {
                    if(eventLoggerPath != string.Empty)
                    {
                        if(eventLogger == null)
                        {
                            eventLogger = new EventLogger(eventLoggerPath);
                        }
                        eventLogger.warning(message);
                    }
                    break;
                }
            }
        }
        public static void warning(string message, object arg1, LogTarget target = LogTarget.File)
        {
            string payload = String.Format(message, arg1);
            warning(payload, target);
        }
        public static void warning(string message, object arg1, object arg2, LogTarget target = LogTarget.File)
        {
            string payload = String.Format(message, arg1, arg2);
            warning(payload, target);
        }
        // ERROR LOGGING
        public static void error(string message, LogTarget target = LogTarget.File)
        {
            switch(target)
            {
                case LogTarget.File:
                {
                    if(fileLoggerPath != string.Empty)
                    {
                        if(fileLogger == null)
                        {
                            fileLogger = new FileLogger(fileLoggerPath);
                        }
                        fileLogger.error(message);
                    }
                    break;
                }
                case LogTarget.Database:
                {
                    if(dbLoggerPath != string.Empty)
                    {
                        if(dbLogger == null)
                        {
                            dbLogger = new DBLogger(dbLoggerPath);
                        }
                        dbLogger.error(message);
                    }
                    break;
                }
                case LogTarget.EventLog:
                {
                    if(eventLoggerPath != string.Empty)
                    {
                        if(eventLogger == null)
                        {
                            eventLogger = new EventLogger(eventLoggerPath);
                        }
                        eventLogger.error(message);
                    }
                    break;
                }
            }
        }
        public static void error(string message, object arg1, LogTarget target = LogTarget.File)
        {
            string payload = String.Format(message, arg1);
            error(payload, target);
        }
        public static void error(string message, object arg1, object arg2, LogTarget target = LogTarget.File)
        {
            string payload = String.Format(message, arg1, arg2);
            error(payload, target);
        }
        // FATAL LOGGING
        public static void fatal(string message, LogTarget target = LogTarget.File)
        {
            switch(target)
            {
                case LogTarget.File:
                {
                    if(fileLoggerPath != string.Empty)
                    {
                        if(fileLogger == null)
                        {
                            fileLogger = new FileLogger(fileLoggerPath);
                        }
                        fileLogger.fatal(message);
                    }
                    break;
                }
                case LogTarget.Database:
                {
                    if(dbLoggerPath != string.Empty)
                    {
                        if(dbLogger == null)
                        {
                            dbLogger = new DBLogger(dbLoggerPath);
                        }
                        dbLogger.fatal(message);
                    }
                    break;
                }
                case LogTarget.EventLog:
                {
                    if(eventLoggerPath != string.Empty)
                    {
                        if(eventLogger == null)
                        {
                            eventLogger = new EventLogger(eventLoggerPath);
                        }
                        eventLogger.fatal(message);
                    }
                    break;
                }
            }
        }
        public static void fatal(string message, object arg1, LogTarget target = LogTarget.File)
        {
            string payload = String.Format(message, arg1);
            fatal(payload, target);
        }
        public static void fatal(string message, object arg1, object arg2, LogTarget target = LogTarget.File)
        {
            string payload = String.Format(message, arg1, arg2);
            fatal(payload, target);
        }
    }
}
