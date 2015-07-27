using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ClampsBeGone
{
    [KSPAddon(KSPAddon.Startup.Flight, true)]
    public class Main : MonoBehaviour
    {

        private bool registered = false;

        public void Start()
        {
            // Startup
            if (!registered)
            {
                GameEvents.onVesselSituationChange.Add(onVesselSituationChange);
                registered = true;
            }
        }

        public void onVesselSituationChange(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> data)
        {
            if (data.from == Vessel.Situations.PRELAUNCH)
            {
                List<Vessel> clampVessels = FlightGlobals.fetch.vessels.FindAll(a => a.parts.Count == 1 && a.parts.First().Modules.OfType<LaunchClamp>().Any());
                foreach (Vessel v in clampVessels)
                {
                    v.Die/*InAFire*/();
                }
            }
        }

    }
}
