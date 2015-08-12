using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

/* Copied from AGroupOnStage, woo! */

namespace ClampsBeGone.Logging
{
    public class Logger
    {

        public static void Log(String message, params object[] data) { Log(message, LogLevel.NORMAL, data); }
        public static void Warning(String message, params object[] data) { LogWarning(message, data); }
        public static void LogWarning(String message, params object[] data) { Log(message, LogLevel.WARNING, data); }
        public static void Error(Exception e) { LogError(e); }
        public static void Log(Exception e) { LogError(e); }
        public static void LogError(String message, params object[] data) { Log(message, LogLevel.ERROR, data); }
        public static void LogError(Exception e) { Log("{0}", LogLevel.ERROR, e.StackTrace); }
        public static void LogClassMethod(object o, MethodBase m) { Log("{0}.{1}", LogLevel.NORMAL, o.GetType().FullName, m.Name); }
        [System.Diagnostics.Conditional("DEBUG")]
        public static void LogDebug(String message, params object[] data) { Log(message, LogLevel.DEBUG, data); }

        public static void Log(String message, LogLevel level, params object[] data)
        {

            message = String.Format(message, data);

            foreach (string line in Regex.Split(message, "\r\n"))
            {

                string logLine = String.Format("{1} {2}", DateTime.Now, "[ClampsBeGone]:", line);
                if (level == LogLevel.WARNING) // Warning (3)
                    UnityEngine.Debug.LogWarning(logLine);
                else if (level == LogLevel.ERROR) // Error (2)
                    UnityEngine.Debug.LogError(logLine);
                else // "normal" (1)
                    UnityEngine.Debug.Log(logLine);


            }

        }

    }

    public enum LogLevel
    {

        DEBUG = 0, // Not used
        NORMAL = 1,
        ERROR = 2,
        WARNING = 3

    }
}
