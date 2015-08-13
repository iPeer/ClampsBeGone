using ClampsBeGone.Logging;
using System;
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
        private List<Part> clampList = new List<Part>();

        public List<String> nonStockModules = new List<String> { "ExtendingLaunchClamp" /* EPL */ };
        public static Main Instance { get; protected set; }
        public static Settings Settings { get; protected set; }
        public static GUIManager GUIManager { get; protected set; }
        //private bool isWaitingForTimer = false;
        private bool ignoreSituationChanges = false;

        private LinkedList<Timer> timerList = new LinkedList<Timer>();

        public void Start()
        {
            // Startup
            if (!registered)
            {
                Instance = this;
                GameEvents.onVesselSituationChange.Add(onVesselSituationChangeNew);
                GameEvents.onFlightReady.Add(onFlightReady);
                GameEvents.onGameSceneSwitchRequested.Add(onGameSceneSwitchRequested);
                GameEvents.onCrewOnEva.Add(onCrewOnEVA);
                GameEvents.onCrewBoardVessel.Add(onCrewBoardVessel);
                GameEvents.onVesselChange.Add(onVesselChange);
                registered = true;
                Settings = new Settings(Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), "ClampsBeGone.cfg"));
                if (Settings.load())
                {
                    foreach (string n in Settings.get<string>("ModModuleNames").Split(','))
                    {
                        if (n.Equals("iPeerNonStockLaunchClampTester")) // We don't need this any more
                            continue;
                        if (!this.nonStockModules.Contains(n))
                            this.nonStockModules.Add(n);
                    }
                    Logger.Log("Loaded {0} Mod module names from file", this.nonStockModules.Count);
                }
                GUIManager = new GUIManager();
            }
        }

        private void onVesselChange(Vessel data)
        {
            this.ignoreSituationChanges = data.isEVA;
        }

        private void onCrewBoardVessel(GameEvents.FromToAction<Part, Part> data)
        {
            this.ignoreSituationChanges = false;
        }

        private void onCrewOnEVA(GameEvents.FromToAction<Part, Part> data)
        {
            this.ignoreSituationChanges = true;
        }

        public void onFlightReady()
        {
            Vessel active = FlightGlobals.fetch.activeVessel;

            if (active.isEVA || this.ignoreSituationChanges) { return; }

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
                Logger.Log(String.Format("{0} clamp(s) on this vessel", _clampList.Count));
                clampList.AddRange(_clampList);
            }
        }

        public void onVesselSituationChangeNew(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> data)
        {

            Logger.Log("Flight situation change: {0} -> {1}", data.from, data.to);

            if (FlightGlobals.fetch.activeVessel.isEVA || this.ignoreSituationChanges) { return; }

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
                if (Settings.get<bool>("UseDelay"))
                {
                    //if (this.isWaitingForTimer) { return; }
                    //this.isWaitingForTimer = true;

                    Logger.Log("Delaying clamp deletion by {0:N0} seconds due to useDelay setting", Settings.get<double>("DeleteDelay") / 1000d);

                    Timer t = new Timer();
                    t.Interval = Settings.get<double>("DeleteDelay");
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
            Logger.Log(String.Format("{0} vessel(s) to kill with fire", vessels.Count));
            foreach (Vessel v in new List<Vessel>(vessels)) // new List so we can modify the underlying object while iterating over it
            {
                if (v.HoldPhysics)
                {
                    Logger.Log("Cannot run on vessel {0} ({1}) because it is on rails.", v.id, v.vesselName);
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
                        Logger.Log(String.Format("Refunding the player {0:N2} funds for clamp vessel with {1:N0} part(s). Dry: {2:N2}, Fuel: {3:N2}", cost, v.parts.Count, dry, fuel));
                        Funding.Instance.AddFunds(cost, TransactionReasons.VesselRecovery);
                    }
                    else
                    {
                        Logger.Log("Couldn't properly refund the player for clamp vessel with {0} part(s) because something went wrong acquiring the protoVessel.", LogLevel.ERROR, v.parts.Count);
                        Logger.Log("Performing \"emergency\" unscaled refund of this vessel. Chances are this will be nowhere near what you paid for the parts (sorry - there's a reason this is a fallback :c).", LogLevel.ERROR);
                        float cost = 0f;
                        foreach (Part p in v.parts)
                        {
                            cost += p.partInfo.cost;
                        }
                        Logger.Log("Refunding the player {0:N2} funds for clamp vessel with {1:N0} part(s).", cost, v.parts.Count);
                        Funding.Instance.AddFunds(cost, TransactionReasons.VesselRecovery);
                    }
                }
                if (Settings.get<bool>("UseExplosions"))
                {
                    Logger.Log("Vessel '{1}' has {0} part(s) to explode", v.parts.Count, v.vesselName);
                    StartCoroutine(BlowUpEVERYTHING(v));
                }
                else
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

        private IEnumerator<WaitForSeconds> BlowUpEVERYTHING/*!*/(Vessel v)
        {
            while (v.parts.Count > 0)
            {
                // Based on code from TAC Self Destruct
                Part part = v.parts.Find(p => p != v.rootPart && !p.children.Any());
                if (part != null)
                {
                    part.explode();
                }
                else
                {
                    v.parts.ForEach(p => p.explode());
                }
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void addTimer(Timer timer)
        {
            if (this.timerList.Contains(timer))
                return;
            this.timerList.AddLast(timer);
            Logger.Log("Now {0} timer(s) running", this.timerList.Count);
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

        public void OnDestroy()
        {
            if (this.timerList.Count > 0)
            {
                Logger.Log("{0} timer(s) to destroy", this.timerList.Count);
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

            OnDestroy();

        }
    }
}
