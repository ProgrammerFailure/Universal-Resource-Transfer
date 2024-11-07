﻿using System;
using UnityEngine;

namespace UniversalResourceTransfer
{
    // as vessel modules are processed in the background, this adds background vessel resource management
    public class BackgroundProcessing : VesselModule
    {
        static string ManagedResource;
        int ResourceHash; int frames; double requestAmount;
        
        public override void OnLoadVessel()
        {
            string ConfigFilePath = KSPUtil.ApplicationRootPath + "GameData/UniversalResourceTransfer/Settings.cfg";
            ConfigNode MainNode;
            MainNode = ConfigNode.Load(ConfigFilePath);
            ManagedResource = MainNode.GetNode("BPSettings").GetNode("ResourceSettings").GetValue("ManagedResource");
            ResourceHash = PartResourceLibrary.Instance.GetDefinition(ManagedResource).id;
        }

        private double LoadVesselPowerData()
        {
            ConfigNode Node = ConfigNode.Load(KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs");
            ConfigNode FlightNode = Node.GetNode("GAME").GetNode("FLIGHTSTATE"); 

            foreach (ConfigNode partnode in FlightNode.GetNode("VESSEL", "name", this.vessel.GetDisplayName()).GetNodes("PART"))
            {
                if (partnode.HasNode("MODULE"))
                {
                    foreach (ConfigNode module in partnode.GetNodes("MODULE"))
                    {
                        if (module.GetValue("name") == "WirelessSource")
                        {
                            double powerBeamed = Convert.ToDouble(module.GetValue("powerBeamed"));
                            requestAmount = powerBeamed;
                            break;
                        }
                        else if (module.GetValue("name") == "WirelessReceiver")
                        {
                            double receivedPower = Convert.ToDouble(module.GetValue("receivedPower"));
                            requestAmount = -receivedPower;
                            break;
                        }
                        else if (module.GetValue("name") == "WirelessReceiverDirectional")
                        {
                            double receivedPower = Convert.ToDouble(module.GetValue("receivedPower"));
                            requestAmount = -receivedPower;
                            break;
                        }
                        else
                        {
                            requestAmount = 0;
                            break;
                        }
                    }
                }
                else
                {
                    requestAmount = 0;
                }
            }
            return requestAmount;
        }

        public void FixedUpdate()
        {
            frames = (frames == 31) ? frames = 0 : frames + 1;
            if (HighLogic.CurrentGame.Parameters.CustomParams<BPSettings>().BackgroundProcessing == true && frames == 30)
            {
                requestAmount = LoadVesselPowerData();
            }
            if (HighLogic.CurrentGame.Parameters.CustomParams<BPSettings>().BackgroundProcessing == true)
            {
                this.vessel.RequestResource(this.vessel.Parts[0], ResourceHash, requestAmount, false);
            }
        }
    }
}
