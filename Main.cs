﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Timers;
using UnityEngine;

namespace ClampsBeGone
{
    [KSPAddon(KSPAddon.Startup.Flight, true)]
    public class Main : MonoBehaviour
    {

        private bool registered = false;
        private List<String> nonStockModules = new List<String> { "iPeerNonStockLaunchClampTester" /* Tester*/, "ExtendingLaunchClamp" /* EPL */ };
        private List<Part> clampList = new List<Part>();
        private bool hasLaunched = false;

        private bool useDelay = false;
        private double deleteDelay = 10000d;
        //private bool isWaitingForTimer = false;

        private LinkedList<Timer> timerList = new LinkedList<Timer>();

        public void Start()
        {
            // Startup
            if (!registered)
            {
                GameEvents.onVesselSituationChange.Add(onVesselSituationChangeNew);
                GameEvents.onFlightReady.Add(onFlightReady);
                GameEvents.onGameSceneSwitchRequested.Add(onGameSceneSwitchRequested);
                registered = true;
                loadSettings();
            }
        }

        private void loadSettings()
        {
            string config = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), "ClampsBeGone.cfg");
            if (!File.Exists(config))
            {
                ConfigNode _new = new ConfigNode();
                _new.AddValue("ModModuleNames", String.Join(",", nonStockModules.ToArray()));
                _new.AddValue("useDelay", this.useDelay);
                _new.AddValue("deleteDelay", this.deleteDelay);
                _new.Save(config);
                return;
            }
            ConfigNode node = ConfigNode.Load(config);
            if (!node.HasValue("ModModuleNames"))
            {
                Log("ConfigNode does not have 'ModModuleNames' entry, aborting loading from file.", LogLevel.ERROR);
            }
            else
            {
                foreach (string n in node.GetValue("ModModuleNames").Split(','))
                {
                    if (!this.nonStockModules.Contains(n))
                        this.nonStockModules.Add(n);
                }
                Log("Loaded {0} Mod module names from file", this.nonStockModules.Count);
            }
            if (node.HasValue("useDelay"))
                this.useDelay = Convert.ToBoolean(node.GetValue("useDelay"));

            if (node.HasValue("deleteDelay"))
            {
                try
                {
                    this.deleteDelay = Convert.ToDouble(node.GetValue("deleteDelay"));
                }
                catch { } // do nothing if it errors (as we already have a default set)
            }

        }

        public void onFlightReady()
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
                        if (!_clampList.Contains(p))
                            _clampList.Add(p);
                    foreach (PartModule pm in p.Modules) // Not stock
                    {
                        if (nonStockModules.Contains(pm.moduleName))
                            if (!_clampList.Contains(p))
                                _clampList.Add(p);
                    }

                }
                Log(String.Format("{0} clamp(s) on this vessel", _clampList.Count));
                clampList.AddRange(_clampList);
            }
        }

        public void onVesselSituationChangeNew(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> data)
        {

            Log("Flight situation change: {0} -> {1}", data.from, data.to);

            if (data.from == Vessel.Situations.PRELAUNCH || this.clampList.Count > 0)
            {
                List<Vessel> vessels = new List<Vessel>();
                List<Part> parts = new List<Part>(this.clampList);

                foreach (Part p in parts)
                {
                    uint fID = p.flightID;
                    foreach (Vessel v in FlightGlobals.fetch.vessels)
                    {
                        if (v.parts.Any(a => a.flightID == fID))
                        {
                            if (v != FlightGlobals.fetch.activeVessel)
                            {
                                this.clampList.Remove(p);
                            }
                            else // Fix for FASA clamps?
                                continue;
                            if (!vessels.Contains(v))
                                vessels.Add(v);
                        }
                    }
                    //vessels.AddRange(FlightGlobals.fetch.vessels.FindAll(a => a.parts.All(b => b.flightID == fID)));
                }
                if (vessels.Count == 0 ) { return; }
                if (this.useDelay)
                {
                    //if (this.isWaitingForTimer) { return; }
                    //this.isWaitingForTimer = true;

                    Log("Delaying clamp deletion by {0:N0} seconds due to useDelay setting", this.deleteDelay / 1000d);

                    Timer t = new Timer();
                    t.Interval = this.deleteDelay;
                    t.Elapsed += (sender, e) => onTimerElapsed(t, vessels);
                    t.Enabled = true;
                    //this.timerList.AddLast(t);
                    addTimer(t);

                }
                else
                {
                    removeVessels(vessels);
                }
            }
        }

        public void removeVessels(List<Vessel> vessels, bool fromTimer = false)
        {
            Log(String.Format("{0} vessel(s) to kill with fire", vessels.Count));
            foreach (Vessel v in new List<Vessel>(vessels)) // new List so we can modify the underlying object while iterating over it
            {
                if (v.HoldPhysics)
                {
                    Log("Cannot run on vessel {0} ({1}) because it is on rails.", v.id, v.vesselName);
                    //if (fromTimer)
                    //    vessels.Remove(v); // Only remove if we're using delay
                    continue;
                }
                //Log("Vessel == null? {0}", v == null);
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                {
                    if (v.protoVessel != null)
                    {
                        float cost = 0f, fuel = 0f, dry = 0f;
                        foreach (ProtoPartSnapshot pps in v.protoVessel.protoPartSnapshots)
                        {
                            //Log("Snapshot == null? {0}", pps == null);
                            float o1, o2;
                            cost += ShipConstruction.GetPartCosts(pps, pps.partInfo, out o1, out o2);
                            fuel += o2;
                            dry += o1;
                        }
                        Log(String.Format("Refunding the player {0:N2} funds for clamp vessel with {1:N0} part(s). Dry: {2:N2}, Fuel: {3:N2}", cost, v.parts.Count, dry, fuel));
                        Funding.Instance.AddFunds(cost, TransactionReasons.VesselRecovery);
                    }
                    else
                    {
                        Log("Couldn't properly refund the player for clamp vessel with {0} part(s) because something went wrong acquiring the protoVessel.", LogLevel.ERROR, v.parts.Count);
                        Log("Performing \"emergency\" unscaled refund of this vessel. Chances are this will be nowhere near waht you paid for the parts (sorry - there's a reason this is a fallback :c).", LogLevel.ERROR);
                        float cost = 0f;
                        foreach (Part p in v.parts)
                        {
                            cost += p.partInfo.cost;
                        }
                        Log("Refunding the player {0:N2} funds for clamp vessel with {1:N0} part(s).", cost, v.parts.Count);
                        Funding.Instance.AddFunds(cost, TransactionReasons.VesselRecovery);
                    }
                }
                v.Die/*InAFire*/();
                Destroy(v); // Probably the equivalent of stamping on a bug after you hit it with a newspaper.
            }

            // This bit of code is like a rebelious teenagers' bedroom! (The quotes are items of clothing!)

            //if (fromTimer)
            //{
                /*if (vessels.Count > 0)
                {
                    Log("Still {0} vessel(s) to remove.", vessels.Count);
                }
                else
                {
                    Log("No more vessels to remove, disabling & disposing of timer.");*/
                    //this.timer.Enabled = false;
                    //this.timer.Dispose();
                    //this.isWaitingForTimer = false;
                //}
            //}
            //else
            //{
                vessels.Clear();
            //}

            //this.clampList.Clear();

        }

        private void addTimer(Timer timer)
        {
            if (this.timerList.Contains(timer))
                return;
            this.timerList.AddLast(timer);
            Log("Now {0} timer(s) running", this.timerList.Count);
        }

        private void onTimerElapsed(Timer t, List<Vessel> vessels)
        {
            removeVessels(vessels, true);
            t.Enabled = false;
            t.Stop(); // Sanity
            this.timerList.Remove(t);
            t.Dispose();
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

            string message = "[ClampsBeGone]: " + String.Format(msg, fillers);

            if (level == LogLevel.ERROR)
                PDebug.Error(message);
            else if (level == LogLevel.WARNING)
                PDebug.Warning(message);
            else
                PDebug.Log(message);

        }

        public void Destroy()
        {
            if (this.timerList.Count > 0)
            {
                Log("{0} timer(s) to destroy", this.timerList.Count);
                foreach (Timer t in this.timerList) 
                {
                    t.Enabled = false;
                    t.Stop(); //Sanity
                    t.Dispose();
                }
            }
        }


        private void onGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> data) 
        {

            Destroy();

        }
    }
}
