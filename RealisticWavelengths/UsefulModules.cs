using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;
using CommNet.Occluders;

namespace BeamedPowerStandalone
{
    public class Wavelengths
    {
        string gamma = Localizer.Format("#LOC_BeamedPower_Wavelength_gamma");
        string x_rays = Localizer.Format("#LOC_BeamedPower_Wavelength_xrays");
        string uv = Localizer.Format("#LOC_BeamedPower_Wavelength_uv");
        string infrared = Localizer.Format("#LOC_BeamedPower_Wavelength_infrared");
        string microwave = Localizer.Format("#LOC_BeamedPower_Wavelength_microwave");
        string radiowave = Localizer.Format("#LOC_BeamedPower_Wavelength_radiowave");
        string unknown = Localizer.Format("#LOC_BeamedPower_Wavelength_unknown");

        public void Wavelength(string wavelength, out string[] wavelengthsLOC, out string[] wavelengths, out string outWavelength)
        {
            wavelengthsLOC = new[] { gamma, x_rays, uv, infrared, microwave, radiowave };
            wavelengths = new[] { "GammaRays", "XRays", "Ultraviolet", "Infrared", "Microwaves", "Radiowaves" };

            if (wavelength == "GammaRays")
            {
                outWavelength = gamma;
            }
            else if (wavelength == "XRays")
            {
                outWavelength = x_rays;
            }
            else if (wavelength == "Ultraviolet")
            {
                outWavelength = uv;
            }
            else if (wavelength == "Infrared")
            {
                outWavelength = infrared;
            }
            else if (wavelength == "Microwaves")
            {
                outWavelength = microwave;
            }
            else if (wavelength == "Radiowaves")
            {
                outWavelength = radiowave;
            }
            else
            {
                outWavelength = unknown;
            }
        }
        public double WavelengthNum(Part thisPart, string Wavelength)
        {
            double wavelengthnum;
            if (Wavelength == "GammaRays")
            {
                wavelengthnum = 5E-11d;
            }
            else if (Wavelength == "XRays")
            {
                wavelengthnum = 1E-9d;
            }
            else if (Wavelength == "Ultraviolet")
            {
                wavelengthnum = 5E-8d;
            }
            else if (Wavelength == "Infrared")
            {
                wavelengthnum = 1E-4d;
            }
            else if (Wavelength == "Microwaves")
            {
                wavelengthnum = 1E-2d;
            }
            else if (Wavelength == "Radiowaves")
            {
                wavelengthnum = 1d;
            }
            else
            {
                wavelengthnum = 8E-4d;       // defaults to infrared if invalid wavelength is set in .cfg
                Debug.LogWarning("BeamedPowerStandalone : Incorrect Wavelength set in .cfg file of part- " + thisPart.partName);
            }
            return wavelengthnum;
        }
    }
    public class VesselFinder
    {
        // Loading all vessels that have WirelessSource module, and adding them to a list to use later
        public void SourceData(string vesselName, out List<Vessel> vesselList, out List<double> excess, out List<double> constant, out List<string> target, out List<string> wave)
        {
            ConfigNode Node = ConfigNode.Load(KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs");
            ConfigNode FlightNode = Node.GetNode("GAME").GetNode("FLIGHTSTATE");

            vesselList = new List<Vessel>(); excess = new List<double>();
            constant = new List<double>(); target = new List<string>();
            wave = new List<string>();

            foreach (ConfigNode vesselnode in FlightNode.GetNodes("VESSEL"))
            {
                if (vesselnode.GetValue("name") != vesselName)
                {
                    foreach (ConfigNode partnode in vesselnode.GetNodes("PART"))
                    {
                        if (partnode.HasNode("MODULE"))
                        {
                            foreach (ConfigNode module in partnode.GetNodes("MODULE"))
                            {
                                if (module.GetValue("name") == "WirelessSource")
                                {
                                    foreach (Vessel vessel in FlightGlobals.Vessels)
                                    {
                                        if (vesselnode.GetValue("name") == vessel.GetDisplayName())
                                        {
                                            vesselList.Add(vessel);
                                            if (vessel.loaded)
                                            {
                                                foreach (Part part in vessel.Parts)
                                                {
                                                    if (part.Modules.Contains<WirelessSource>())
                                                    {
                                                        excess.Add(Convert.ToDouble(part.Modules.GetModule<WirelessSource>().Fields.GetValue("Excess")));
                                                        constant.Add(Convert.ToDouble(part.Modules.GetModule<WirelessSource>().Fields.GetValue("Constant")));
                                                        target.Add(Convert.ToString(part.Modules.GetModule<WirelessSource>().Fields.GetValue("TransmittingTo")));
                                                        wave.Add(Convert.ToString(part.Modules.GetModule<WirelessSource>().Fields.GetValue("Wavelength")));
                                                        break;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                excess.Add(Convert.ToDouble(module.GetValue("Excess")));
                                                constant.Add(Convert.ToDouble(module.GetValue("Constant")));
                                                target.Add(module.GetValue("TransmittingTo"));
                                                wave.Add(module.GetValue("Wavelength"));
                                            }
                                            break;
                                        }
                                    }
                                    break;
                                }
                                else if (module.GetValue("name") == "WirelessReflector")
                                {
                                    foreach (Vessel vessel in FlightGlobals.Vessels)
                                    {
                                        if (vesselnode.GetValue("name") == vessel.GetDisplayName())
                                        {
                                            vesselList.Add(vessel);
                                            if (vessel.loaded)
                                            {
                                                foreach (Part part in vessel.Parts)
                                                {
                                                    if (part.Modules.Contains<WirelessReflector>())
                                                    {
                                                        excess.Add(Convert.ToDouble(part.Modules.GetModule<WirelessReflector>().Fields.GetValue("Excess")));
                                                        constant.Add(Convert.ToDouble(part.Modules.GetModule<WirelessReflector>().Fields.GetValue("Constant")));
                                                        target.Add(Convert.ToString(part.Modules.GetModule<WirelessReflector>().Fields.GetValue("TransmittingTo")));
                                                        wave.Add(Convert.ToString(part.Modules.GetModule<WirelessReflector>().Fields.GetValue("Wavelength")));
                                                        break;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                excess.Add(Convert.ToDouble(module.GetValue("Excess")));
                                                constant.Add(Convert.ToDouble(module.GetValue("Constant")));
                                                target.Add(module.GetValue("TransmittingTo"));
                                                wave.Add(module.GetValue("Wavelength"));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // gets all receiver spacecraft's confignodes from savefile
        public void ReceiverData(string vesselName, out List<ConfigNode> receiversList)
        {
            ConfigNode Node = ConfigNode.Load(KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs");
            ConfigNode FlightNode = Node.GetNode("GAME").GetNode("FLIGHTSTATE");

            receiversList = new List<ConfigNode>();

            foreach (ConfigNode vesselnode in FlightNode.GetNodes("VESSEL"))
            {
                if (vesselnode.GetValue("name") != vesselName)
                {
                    foreach (ConfigNode partnode in vesselnode.GetNodes("PART"))
                    {
                        if (partnode.HasNode("MODULE"))
                        {
                            foreach (ConfigNode module in partnode.GetNodes("MODULE"))
                            {
                                if (module.GetValue("name") == "WirelessReceiver")
                                {
                                    receiversList.Add(vesselnode);
                                    break;
                                }
                                else if (module.GetValue("name") == "WirelessReflector" | module.GetValue("name") == "ThermalEngine")
                                {
                                    receiversList.Add(vesselnode);
                                    break;
                                }
                                else if (module.GetValue("name") == "AblativeEngine" | module.GetValue("name") == "PhotonSail")
                                {
                                    receiversList.Add(vesselnode);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public class PlanetOcclusion
    {
        // checks for occlusion by each celestial body
        public void IsOccluded(Vector3d source, Vector3d dest, string wavelength, out CelestialBody celestialBody, out bool occluded)
        {
            bool planetocclusion = HighLogic.CurrentGame.Parameters.CustomParams<BPSettings>().planetOcclusion;
            Transform transform2; double radius2; celestialBody = new CelestialBody(); occluded = new bool();

            if (planetocclusion)
            {
                for (int x = 0; x < FlightGlobals.Bodies.Count; x++)
                {
                    transform2 = FlightGlobals.Bodies[x].transform;
                    radius2 = FlightGlobals.Bodies[x].Radius;
                    celestialBody = FlightGlobals.Bodies[x];
                    float occlusionMult;
                    if (wavelength == "Radiowaves") { occlusionMult = 0.3f; }
                    else if (wavelength == "Microwaves") { occlusionMult = 0.6f; }
                    else if (wavelength == "Infrared") { occlusionMult = 0.7f; }
                    else if (wavelength == "Ultraviolet") { occlusionMult = 0.9f; }
                    else if (wavelength == "XRays") { occlusionMult = 0.95f; }
                    else if (wavelength == "GammaRays") { occlusionMult = 1f; }
                    else { occlusionMult = 0.9f; }
                    radius2 *= occlusionMult;

                    OccluderHorizonCulling occlusion = new OccluderHorizonCulling(transform2, radius2, radius2, radius2);
                    occlusion.Update();
                    occluded = occlusion.Raycast(source, dest);
                    if (occluded == true)
                    {
                        break;
                    }
                }
            }
            else
            {
                occluded = false;
            }
        }
    }

    public class RelativisticEffects
    {
        bool relativistic; const float c = 299792452; Vector3d prevPos = Vector3d.zero;
        string exceeded_c = Localizer.Format("#LOC_BeamedPower_ExceededC");

        public double RedOrBlueShift(Vessel source, Vessel dest, string state, out string status)
        {
            relativistic = HighLogic.CurrentGame.Parameters.CustomParams<BPSettings>().relativistic; double powerMult;
            double v = Vector3d.Magnitude(source.orbit.GetWorldSpaceVel() - dest.orbit.GetWorldSpaceVel());
            v *= Math.Cos(Vector3d.Angle((source.GetWorldPos3D() - dest.GetWorldPos3D()), 
                (source.orbit.GetWorldSpaceVel() - dest.orbit.GetWorldSpaceVel())));

            if (relativistic)
            {
                if (v >= c - 1)
                {
                    powerMult = 0;
                    status = exceeded_c;
                }
                else
                {
                    powerMult = Math.Sqrt((1 + v / c) / (1 - v / c));
                    status = state;
                }
            }
            else
            {
                powerMult = 1;
                status = state;
            }
            return powerMult;
        }

        public bool WarpDriveEngaged(Part part)
        {
            relativistic = HighLogic.CurrentGame.Parameters.CustomParams<BPSettings>().relativistic; bool warping = false;
            if (relativistic)
            {
                Vector3d position =  part.vessel.GetWorldPos3D();
                double displacement = Vector3d.Distance(position, prevPos);
                double v = displacement / TimeWarp.fixedDeltaTime;
                if (v > part.vessel.orbit.GetWorldSpaceVel().magnitude * 2d)
                {
                    warping = true;
                }
                prevPos = position;
            }
            return warping;
        }
    }
}
