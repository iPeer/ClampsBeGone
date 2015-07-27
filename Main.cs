﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ClampsBeGone
{
    [KSPAddon(KSPAddon.Startup.Flight, true)]
    public class Main : MonoBehaviour
    {

        private bool registered = false;
        private List<String> nonStockModules = new List<String>{ "iPeerNonStockLaunchClampTester" /* Tester*/, "ExtendingLaunchClamp" /* EPL */ };
        private List<Part> clampList = new List<Part>();

        public void Start()
        {
            // Startup
            if (!registered)
            {
                GameEvents.onVesselSituationChange.Add(onVesselSituationChangeNew);
                GameEvents.onFlightReady.Add(onFlightReady);
                registered = true;
                loadCustomModuleList();
            }
        }

        private void loadCustomModuleList()
        {
            string config = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), "ClampsBeGone.cfg");
            if (!File.Exists(config))
            {
                ConfigNode _new = new ConfigNode();
                _new.AddValue("ModModuleNames", String.Join(",", nonStockModules.ToArray()));
                _new.Save(config);
                return;
            }
            ConfigNode node = ConfigNode.Load(config);
            if (!node.HasValue("ModModuleNames"))
            {
                Log("ConfigNode does not have 'ModModuleNames' entry, aborting loading from file.", LogLevel.ERROR);
                return;
            }
            foreach (string n in node.GetValue("ModModuleNames").Split(','))
            {
                if (!this.nonStockModules.Contains(n))
                    this.nonStockModules.Add(n);
            }
            Log("Loaded {0} Mod module names from file", this.nonStockModules.Count);
        }

        public void onFlightReady() // Not currently working
        {
            Vessel active = FlightGlobals.fetch.activeVessel;
            if (active.situation == Vessel.Situations.PRELAUNCH)
            {
                if (clampList.Count > 0)
                    clampList.Clear();
                List<Part> _clampList = new List<Part>();
                foreach (Part p in active.parts)
                {
                    if (p.Modules.OfType<LaunchClamp>().Any()) // Stock
                        _clampList.Add(p);
                        foreach (PartModule pm in p.Modules)
                        {
                            if (nonStockModules.Contains(pm.name))
                                _clampList.Add(p);
                        }

                }
                Log(String.Format("{0} clamp(s) on this vessel", _clampList.Count));
                clampList.AddRange(_clampList);
            }
        }

        public void onVesselSituationChangeNew(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> data)
        {
            if (data.from == Vessel.Situations.PRELAUNCH)
            {
                List<Vessel> vessels = new List<Vessel>();

                // So many loops!
                foreach (Part p in this.clampList)
                {
                    uint fID = p.flightID;
                    foreach (Vessel v in FlightGlobals.fetch.vessels)
                    {
                        if (v.parts.Any(a => a.flightID == fID))
                            vessels.Add(v);
                    }
                    //vessels.AddRange(FlightGlobals.fetch.vessels.FindAll(a => a.parts.All(b => b.flightID == fID)));
                }

               Log(String.Format("{0} vessel(s) to kill with fire", vessels.Count));
                foreach (Vessel v in vessels)
                {
                    if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                    {
                        float cost = 0f;
                        foreach (Part p in v.parts)
                            cost += p.partInfo.cost;
                        Log(String.Format("Refunding the player {0:N2} funds for clamp vessel with {1:N0} part(s)", cost, v.parts.Count));
                        Funding.Instance.AddFunds(cost, TransactionReasons.VesselRecovery);
                    }
                    v.Die/*InAFire*/();
                }

                this.clampList.Clear();
                vessels.Clear();

            }
        }

        public void onVesselSituationChange(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> data)
        {
            if (data.from == Vessel.Situations.PRELAUNCH)
            {
                List<Vessel> clampVessels = FlightGlobals.fetch.vessels.FindAll(a => a.parts.Count == 1 && a.parts.First().Modules.OfType<LaunchClamp>().Any());
                foreach (string nsm in nonStockModules)
                    clampVessels.AddRange(FlightGlobals.fetch.vessels.FindAll(a => a.parts.Count == 1 && a.parts.First().Modules[nsm] != null));
                foreach (Vessel v in clampVessels)
                {
                    if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                    {
                        Part p = v.parts.First();
                        Funding.Instance.AddFunds(p.partInfo.cost, TransactionReasons.VesselRecovery);
                    }
                    v.Die/*InAFire*/();
                }
            }
        }

        private enum LogLevel
        {
            NORMAL,
            DEBUG,
            WARNING,
            ERROR
        }
        private void Log(string msg, params object[] fillers)
        {
            Log(msg, LogLevel.NORMAL, fillers);
        }
        private void Log(string msg, LogLevel level, params object[] fillers)
        {

            string message = "[ClampsBeGone]: "+String.Format(msg, fillers);

            if (level == LogLevel.ERROR)
                PDebug.Error(message);
            else if (level == LogLevel.WARNING)
                PDebug.Warning(message);
            else
                PDebug.Log(message);

        }

    }
}
