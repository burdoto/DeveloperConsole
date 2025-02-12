﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System.Linq;
using UnityEngine;

namespace BepInEx
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class DeveloperConsole : BaseUnityPlugin
    {
        public const string GUID = "com.bepis.bepinex.developerconsole";
        public const string PluginName = "Developer Console";
        public const string Version = "1.0";
        internal static new ManualLogSource Logger;

        private bool showingUI = false;
        private static string TotalLog = "";
        private Rect UI = new Rect(900, 0, 900, 500);
        private static Vector2 scrollPosition = Vector2.zero;
        private GUIStyle logTextStyle = new GUIStyle();

        public static ConfigFile BepinexConfig { get; } = new ConfigFile(Utility.CombinePaths(Paths.ConfigPath, "BepInEx.cfg"), false);

        public static ConfigEntry<int> LogDepth { get; private set; }
        public static ConfigEntry<int> fontSize { get; private set; }
        public static ConfigEntry<bool> LogUnity { get; private set; }
        public static ConfigEntry<KeyboardShortcut> ToggleUIShortcut { get; private set; }

        public DeveloperConsole()
        {
            LogDepth = Config.Bind("Config", "Log buffer size", 16300, "Size of the log buffer in characters.");
            fontSize = Config.Bind("Config", "Font Size", 14, new ConfigDescription("Adjusts the fontSize of the log text.", new AcceptableValueRange<int>(8, 80)));
            LogUnity = Config.Bind("Logging", "UnityLogListening", true, "Enables showing unity log messages in the BepInEx logging system.");
            ToggleUIShortcut = Config.Bind("Config", "Toggle UI Shortcut", new KeyboardShortcut(KeyCode.Pause), "Toggles the visibility of the developer console.");
            
            logTextStyle.normal.textColor = Color.white;
            
            Logging.Logger.Listeners.Add(new LogListener());
            Logger = base.Logger;
        }

        private static void OnEntryLogged(LogEventArgs logEventArgs)
        {
            string current = $"{TotalLog}\r\n{logEventArgs.Data?.ToString()}";
            if (current.Length > LogDepth.Value)
            {
                var trimmed = current.Remove(0, 1000);

                // Trim until the first newline to avoid partial line
                var newlineHit = false;
                trimmed = new string(trimmed.SkipWhile(x => !newlineHit && !(newlineHit = (x == '\n'))).ToArray());

                current = "--LOG TRIMMED--\n" + trimmed;
            }
            TotalLog = current;

            scrollPosition = new Vector2(0, float.MaxValue);
        }

        protected void OnGUI()
        {
            if (showingUI)
                UI = GUILayout.Window(589, UI, WindowFunction, "Developer Console");
        }

        protected void Update()
        {
            if (ToggleUIShortcut.Value.IsDown())
                showingUI = !showingUI;
        }

        private void WindowFunction(int windowID)
        {
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Clear console"))
                    TotalLog = "Log cleared";
                if (GUILayout.Button("Dump scene"))
                    SceneDumper.DumpScene();

                LogUnity.Value = GUILayout.Toggle(LogUnity.Value, "Unity");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GUI.skin.box);
            {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.FlexibleSpace();
                        logTextStyle.fontSize = fontSize.Value;
                        GUILayout.TextArea(TotalLog, logTextStyle);
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();

            switch(Event.current.button)
            {
                case 0://Left mouse button window drag - move
                    GUI.DragWindow();
                    break;
                case 1://Right mouse button window drag - resize
                    if(Event.current.type == EventType.MouseDrag)
                        UI.width += Event.current.delta.x;
                        UI.height += Event.current.delta.y;
                    break;
            }
        }
    }
}
