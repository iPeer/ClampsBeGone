using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ClampsBeGone.Toolbar
{
    public class Manager
    {

        public enum ToolbarType
        {
            NONE,
            STOCK,
            BLIZZY
        }

        public static Manager Instance { get; protected set; }

        public ToolbarType Type { get; private set; }
        ApplicationLauncherButton _button;
        IButton bButton;
        public bool ButtonAdded { get; private set; }

        public void Awake()
        {
            if (Instance == null)
                Instance = this;
            // Get the toolbar type from the settings
            Type = getTypeFromString(Main.Settings.get<string>("ToolbarType"));
            if (Type == ToolbarType.NONE) { return; }
            else if (Type == ToolbarType.STOCK || (Type == ToolbarType.BLIZZY && !ToolbarManager.ToolbarAvailable))
            {
                if (ApplicationLauncher.Ready)
                    onGUIApplicationLauncherReady();
                else
                    GameEvents.onGUIApplicationLauncherReady.Add(onGUIApplicationLauncherReady);
            }
            else
            {
                IButton _button = ToolbarManager.Instance.add("ClampsBeGone", "ClampsBeGone");
                _button.TexturePath = "iPeer/ClampsBeGone/Texturees/000";
                _button.OnClick += (e) => { Main.GUIManager.toggleGUI(); };
            }
        }

        private void onGUIApplicationLauncherReady()
        {
            if (ButtonAdded) { return; }
            GameEvents.onGUIApplicationLauncherReady.Remove(onGUIApplicationLauncherReady);
            _button = ApplicationLauncher.Instance.AddModApplication(
                Main.GUIManager.toggleGUI,
                Main.GUIManager.toggleGUI,
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW,
                GameDatabase.Instance.GetTexture("iPeer/ClampsBeGone/Textures/Toolbar", false)
            );
            ButtonAdded = true;
        }

        private void removeApplicationLauncherButton()
        {
            if (!ButtonAdded) { return; }
            ApplicationLauncher.Instance.RemoveModApplication(_button);
            ButtonAdded = false;
        }

        public void OnDestroy()
        {
            Instance = null;
            removeApplicationLauncherButton();
        }

        public void LateUpdate()
        {
            if (GameSettings.MODIFIER_KEY.GetKey() && Input.GetKeyDown("C"))
            {
                Main.GUIManager.toggleGUI();
            }
        }

        public ToolbarType getTypeFromString(string type)
        {
            switch (type)
            {
                case "STOCK":
                case "stock":
                    return ToolbarType.STOCK;
                case "BLIZZY":
                case "blizzy":
                    return ToolbarType.BLIZZY;
                default:
                    return ToolbarType.NONE;
            }
        }

        public int currentTypeAsInt()
        {
            return typeAsInt(Type);
        }

        public int typeAsInt(ToolbarType type)
        {
            switch (type)
            {
                case ToolbarType.STOCK:
                    return 1;
                case ToolbarType.BLIZZY:
                    return 2;
                default: 
                    return 0;
            }
        }

    }
}
