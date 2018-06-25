using ColossalFramework.Plugins;
using System;
using System.IO;

namespace AutoLineColor
{
    public class Console
    {
        private static Console _instance;

        private readonly StreamWriter _log;
        private readonly bool _logOpened;

        private Console()
        {
#if DEBUG
            Debug = true;
#endif
            try
            {
                _log = new StreamWriter(new FileStream(Constants.LogFileName, FileMode.Append, FileAccess.Write,
                    FileShare.ReadWrite));
                _logOpened = true;
            }
            catch
            {
                WriteMessage("Could not open log file", PluginManager.MessageType.Warning);
            }
        }

        public static Console Instance => _instance ?? (_instance = new Console());

        public bool Debug { get; private set; }

        private static string FormatMessage(string msg, PluginManager.MessageType type)
        {
            string formatted;
            try
            {
                formatted = $"[AutoLineColor] {DateTime.Now:yyyy-MM-dd hh:mm:ss} ({type.ToString()}) {msg}";
            }
            catch (Exception e)
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, e.ToString());
                formatted = msg;
            }

            return formatted;
        }

        public void SetDebug(bool should_debug)
        {
            Debug = should_debug;
        }

        public void Message(string p)
        {
            this.WriteMessage(p, PluginManager.MessageType.Message);
        }

        // ReSharper disable once UnusedMember.Global
        public void Warning(string p)
        {
            this.WriteMessage(p, PluginManager.MessageType.Warning);
        }

        public void Error(string p)
        {
            this.WriteMessage(p, PluginManager.MessageType.Error);
        }

        private void WriteMessage(string p, PluginManager.MessageType Type)
        {
            if (!this.Debug)
            {
                return;
            }

            var msg = FormatMessage(p, Type);
            DebugOutputPanel.AddMessage(Type, msg);
            if (_logOpened)
            {
                _log.WriteLine(msg);
                _log.Flush();
            }

            //Unity engine logger
            switch (Type)
            {
                case PluginManager.MessageType.Error:
                    UnityEngine.Debug.LogError(msg);
                    break;
                case PluginManager.MessageType.Message:
                    UnityEngine.Debug.Log(msg);
                    break;
                case PluginManager.MessageType.Warning:
                    UnityEngine.Debug.LogWarning(msg);
                    break;
                default:
                    goto case PluginManager.MessageType.Message;
            }
        }
    }
}
