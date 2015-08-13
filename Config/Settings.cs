using ClampsBeGone.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/* Copied from AGroupOnStage, woo! */

namespace ClampsBeGone
{
    public class Settings
    {

        public string configPath;
        public Dictionary<string, object> SETTINGS_MAP;
        private bool hasChanged = false;

        public Settings(string path)
        {
            this.configPath = path;

            this.SETTINGS_MAP = new Dictionary<string, object> {
                {"ModModuleNames", "ExtendingLaunchClamp"},
                {"UseDelay", false},
                {"DeleteDelay", 10000d},
                {"GUIPosX", 0},
                {"GUIPosY", 0},
                {"UseExplosions", false}
            };

        }

        public void copyTo(Dictionary<string, object> target)
        {
            if (target == null)
                target = new Dictionary<string, object>();
            foreach (string s in this.SETTINGS_MAP.Keys)
                target.Add(s, this.SETTINGS_MAP[s]);
        }

        public void setTo(Dictionary<string, object> newSettings)
        {
            foreach (string s in newSettings.Keys)
                this.SETTINGS_MAP[s] = newSettings[s];
            this.hasChanged = true;
        }

        public Dictionary<string, object> getCopy()
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();
            this.copyTo(ret);
            return ret;
        }

        public void set(string s, object v)
        {
            if (this.SETTINGS_MAP.ContainsKey(s))
            {
                if (!get(s).Equals(v.ToString()))
                    this.hasChanged = true;
                this.SETTINGS_MAP[s] = v;
            }
            else
                Logger.LogWarning("Something attempted to write undefined setting '{0}' to '{1}'!", s, v);
        }

        public T get<T>(string setting)
        {
            if (this.SETTINGS_MAP.ContainsKey(setting))
            {
                if (this.SETTINGS_MAP[setting] is T)
                    return (T)this.SETTINGS_MAP[setting];
                else
                {
                    try
                    {
                        return (T)Convert.ChangeType(this.SETTINGS_MAP[setting], typeof(T));
                    }
                    catch (InvalidCastException)
                    {
                        return default(T);
                    }
                }
            }
            Logger.LogWarning("Attempted to read invalid setting '{0}'!", setting);
            return default(T);
        }

        public string get(string setting)
        {
            if (this.SETTINGS_MAP.ContainsKey(setting))
                return this.SETTINGS_MAP[setting].ToString();
            Logger.LogWarning("Attempted to read invalid setting '{0}'!", setting);
            return null;
        }

        public bool load()
        {

            Logger.Log("CBG is loading settings");
            ConfigNode node = ConfigNode.Load(this.configPath);
            if (node == null || node.CountValues == 0) { Logger.Log("No settings to load!"); return false; }

            Dictionary<string, object> _new = new Dictionary<string, object>();

            List<string> keys = new List<String>(this.SETTINGS_MAP.Keys);

            foreach (string s in keys)
                if (node.HasValue(s))
                    _new.Add(s, node.GetValue(s));
            this.setTo(_new);
            Logger.Log("Done loading settings!");
            return true;

        }

        public void save()
        {
            if (!this.hasChanged)
                return;
            this.hasChanged = false;
            Logger.Log("CBG is saving config...");
            ConfigNode node = new ConfigNode();
            foreach (string s in this.SETTINGS_MAP.Keys)
                node.AddValue(s, this.SETTINGS_MAP[s]);
            node.Save(this.configPath);
            /*if (get<bool>("LogNodeSaving"))
                Logger.Log("{0}", node.ToString());*/
            Logger.Log("Done saving settings!");

        }

        public void removeFile()
        {
            try
            {
                File.Delete(this.configPath);
            }
            catch (Exception e)
            {
                Logger.LogError("Couldn't delete config file: {0}", e.ToString());
            }
        }

    }
}
