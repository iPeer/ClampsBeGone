using ClampsBeGone.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ClampsBeGone
{
    public class GUIManager : MonoBehaviour 
    {

        public ApplicationLauncherButton _button { get; private set; }
        public bool guiVisible { get; set; }

        private readonly int windowID = 252677785;
        private Rect _windowPos = new Rect();
        private bool buttonAdded = false;

        private string nonStockModules = "";
        private double deleteDelay = 10d;
        private bool useDelay = false;
        private bool useExplosions = false;

        private bool hasInitStyles = false;

        private string[] toolbarSettings = new string[]{"Off", "Stock", "Blizzy"};
        private int toolbarOption = 1; //                 0       1         2

        GUIStyle _windowStyle, _labelStyle, _textBoxStyle, _buttonStyle, _sliderStyle, _sliderThumbStyle, _toggleStyle;

        public GUIManager()
        {
            /*if (ApplicationLauncher.Ready)
                onGUIApplicationLauncherReady();
            else
                GameEvents.onGUIApplicationLauncherReady.Add(onGUIApplicationLauncherReady);*/
        }

        /*private void onGUIApplicationLauncherReady()
        {
            if (buttonAdded) { return; }
            GameEvents.onGUIApplicationLauncherReady.Remove(onGUIApplicationLauncherReady);
            _button = ApplicationLauncher.Instance.AddModApplication(
                toggleGUI,
                toggleGUI,
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW,
                GameDatabase.Instance.GetTexture("iPeer/ClampsBeGone/Textures/Toolbar", false)
            );
            buttonAdded = true;
        }

        private void removeApplicationLauncherButton()
        {
            if (!buttonAdded) { return; }
            ApplicationLauncher.Instance.RemoveModApplication(_button);
            buttonAdded = false;
        }*/

        public void toggleGUI()
        {

            if (guiVisible)
                RenderingManager.RemoveFromPostDrawQueue(windowID, OnGUI);
            else
            {
                this.nonStockModules = Main.Settings.get<string>("ModModuleNames");
                this.deleteDelay = Main.Settings.get<double>("DeleteDelay") / 1000d;
                this.useDelay = Main.Settings.get<bool>("UseDelay");
                this.useExplosions = Main.Settings.get<bool>("UseExplosions");
                _windowPos.x = Main.Settings.get<float>("GUIPosX");
                _windowPos.y = Main.Settings.get<float>("GUIPosY");
                RenderingManager.AddToPostDrawQueue(windowID, OnGUI);
            }
            guiVisible = !guiVisible;

        }

        private void initStyles()
        {

            Logger.Log("Settings up styles...");
            GUISkin[] skins = Resources.FindObjectsOfTypeAll(typeof(GUISkin)) as GUISkin[];
            GUISkin skin = HighLogic.Skin;
            foreach (GUISkin s in skins) // Favour "Unity" or "GameSkin" skins
            {
#if DEBUG
                Logger.Log(s.name);
#endif
                if (s.name.Equals("Unity") || s.name.Equals("GameSkin"))
                {
                    skin = s;
                    break;
                }
            }
            hasInitStyles = true;
            //GUISkin skin = GUI.skin;
            _windowStyle = new GUIStyle(skin.window);
            _windowStyle.stretchHeight = true;
            _windowStyle.stretchWidth = true;
            /*_windowStyle.fixedHeight = 200f;
            _windowStyle.fixedWidth = 360f;*/
            _labelStyle = new GUIStyle(skin.label);
            _labelStyle.stretchWidth = true;
            _toggleStyle = new GUIStyle(skin.toggle);
            _toggleStyle.stretchWidth = true;
            _textBoxStyle = new GUIStyle(skin.textField);
            _sliderStyle = new GUIStyle(skin.horizontalSlider);
            _sliderThumbStyle = new GUIStyle(skin.horizontalSliderThumb);
            _buttonStyle = new GUIStyle(skin.button);
        }

        public void OnGUI()
        {

            if (!hasInitStyles) { initStyles(); }

            // GUI Sanity checks

            _windowPos.x = Mathf.Clamp(_windowPos.x, 0f, Screen.width - _windowPos.width);
            _windowPos.y = Mathf.Clamp(_windowPos.y, 0f, Screen.height - _windowPos.height);


            _windowPos = GUILayout.Window(windowID, _windowPos, OnWindow, "ClampsBeGone Config", _windowStyle, GUILayout.MinHeight(200f), GUILayout.MinWidth(360f), GUILayout.MaxWidth(360f));

        }

        public void OnWindow(int id)
        {
            GUILayout.BeginVertical(/*GUILayout.MinHeight(360f), GUILayout.MinWidth(200f)*/);
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.Label("Custom clamp modules (separate using commas):", _labelStyle);
                    this.nonStockModules = GUILayout.TextField(this.nonStockModules, _textBoxStyle, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true));
                }
                GUILayout.EndVertical();
                GUILayout.Space(10f);
                this.useExplosions = GUILayout.Toggle(this.useExplosions, "Explosive ordnance mode", _toggleStyle); // Previously in ClampsBeGone testing: http://ipeer.auron.co.uk/KpifS.png
                this.useDelay = GUILayout.Toggle(this.useDelay, "Delay removal of launch clamps", _toggleStyle);
                GUILayout.Space(10f);
                GUILayout.BeginVertical();
                {
                    GUILayout.Label("Removal delay:", _labelStyle);
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(String.Format("{0:N0}s", this.deleteDelay), _labelStyle, GUILayout.MinWidth(30f), GUILayout.MaxWidth(30f));
                        this.deleteDelay = GUILayout.HorizontalSlider((Single)this.deleteDelay, 1f, 120f, _sliderStyle, _sliderThumbStyle);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();

                if (GUILayout.Button("Save & Close", _buttonStyle))
                {
                    Main.Settings.set("GUIPosX", _windowPos.x);
                    Main.Settings.set("GUIPosY", _windowPos.y);
                    Main.Settings.set("ModModuleNames", this.nonStockModules);
                    Main.Instance.nonStockModules = this.nonStockModules.Split(',').ToList();
                    Main.Settings.set("UseDelay", this.useDelay);
                    Main.Settings.set("DeleteDelay", Math.Round(this.deleteDelay) * 1000d);
                    Main.Settings.set("UseExplosions", this.useExplosions);
                    Main.Settings.save();
                    toggleGUI();
                }

                if (GUILayout.Button("Close", _buttonStyle))
                {
                    toggleGUI();
                }

                if (GUILayout.Button("Dump size info", _buttonStyle))
                {
                    Logger.Log("{0}", _windowPos);
                    Logger.Log("W: {0}, H: {1}", _windowPos.width, _windowPos.height);
                }

            }
            GUILayout.EndVertical();

            GUI.DragWindow();

        }

    }
}
