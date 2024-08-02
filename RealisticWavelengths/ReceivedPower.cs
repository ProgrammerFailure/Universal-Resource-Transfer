using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;

namespace BeamedPowerStandalone
{
    public class ReceivedPower
    {
        PlanetOcclusion occlusion = new PlanetOcclusion();
        VesselFinder vesselFinder = new VesselFinder();
        RelativisticEffects relativistic = new RelativisticEffects();

        List<Vessel> VesselList = new List<Vessel>();
        List<double> excessList = new List<double>();
        List<double> constantList = new List<double>();
        List<string> targetList = new List<string>();
        public List<string> wavelengthList = new List<string>();
        List<CelestialBody> planetList = new List<CelestialBody>();
        int frames = 20;

        //localization
        string occludedby = Localizer.Format("#LOC_BeamedPower_status_Occludedby") + " ";
        string operational = Localizer.Format("#LOC_BeamedPower_status_Operational");
        string vesselNone = Localizer.Format("#LOC_BeamedPower_Vessel_None");
        string warpEngaged = Localizer.Format("#LOC_BeamedPower_WarpDriveEngaged");

        private double FractionalFlux(Vector3d source_pos, Vector3d dest_pos, Part part)
        {
            Vector3d resultant = (source_pos - dest_pos).normalized;
            double Angle = Vector3d.Angle(resultant, -part.transform.up);
            double flux = Math.Cos(Math.PI * Angle / 180d);
            return flux;
        }

        public void Directional(Part thisPart, int counter, bool Listening, float percentagePower, double recvSize, double recvEfficiency,
            bool useSpotArea, bool useFacingVector, string state, out string status, out string VesselName, out double receivedpower, out int count)
        {
            frames += 1;
            if (frames == 40)
            {
                vesselFinder.SourceData(thisPart.vessel.GetDisplayName(), out VesselList, out excessList, out constantList, out targetList, out wavelengthList);
                frames = 0;
            }
            if (counter >= VesselList.Count)
            {
                counter = 0;
            }
            
            if (VesselList.Count > 0)
            {
                if (Listening & targetList[counter] == thisPart.vessel.GetDisplayName())
                {
                    Vector3d dest = thisPart.vessel.GetWorldPos3D(); 
                    double excess2 = excessList[counter]; double constant2 = constantList[counter];
                    VesselName = VesselList[counter].GetDisplayName();
                    Vector3d source = VesselList[counter].GetWorldPos3D();
                    double distance = Vector3d.Distance(source, dest);
                    double spotsize = (useSpotArea) ? Math.Pow((constant2 * distance / 2), 2) * 3.14 : constant2 * distance;

                    occlusion.IsOccluded(source, dest, wavelengthList[counter], out CelestialBody body, out bool occluded);

                    // adding EC that has been received
                    if (recvSize < spotsize)
                    {
                        receivedpower = Math.Round(((recvSize / spotsize) * recvEfficiency * excess2 * (percentagePower / 100)), 1);
                    }
                    else
                    {
                        receivedpower = Math.Round(((recvEfficiency * excess2) * (percentagePower / 100)), 1);
                    }
                    if (occluded)
                    {
                        receivedpower = 0;
                        state = occludedby + body.GetDisplayName().TrimEnd('N', '^');
                    }
                    else
                    {
                        state = operational;
                    }

                    receivedpower *= relativistic.RedOrBlueShift(VesselList[counter], thisPart.vessel, state, out state);
                    if (useFacingVector)
                    {
                        receivedpower *= FractionalFlux(source, dest, thisPart);
                    }
                    if (relativistic.WarpDriveEngaged(thisPart))
                    {
                        receivedpower = 0;
                        state = warpEngaged;
                    }
                }
                else
                {
                    receivedpower = 0;
                    VesselName = vesselNone;
                }
            }
            else
            {
                receivedpower = 0;
                VesselName = vesselNone;
            }
            status = state; count = counter;
        }

        public void Spherical(Part thisPart, bool Listening, float percentagePower, double recvSize,
             double recvEfficiency, bool useSpotArea, bool useFacingVector, string state, out string status, out double received_power)
        {
            frames += 1;
            if (frames == 40)
            {
                vesselFinder.SourceData(thisPart.vessel.GetDisplayName(), out VesselList, out excessList, out constantList, out targetList, out wavelengthList);
                frames = 0;
            }

            received_power = 0;
            if (VesselList.Count > 0)
            {
                if (Listening)
                {
                    Vector3d dest = thisPart.vessel.GetWorldPos3D();
                    planetList.Clear();
                    // adds up all the received power values from all vessels in CorrectVesselList 
                    for (int n = 0; n < VesselList.Count; n++)
                    {
                        if (targetList[n] == thisPart.vessel.GetDisplayName())
                        {
                            double excess2 = excessList[n]; double constant2 = constantList[n];
                            Vector3d source = VesselList[n].GetWorldPos3D();
                            double distance = Vector3d.Distance(source, dest);
                            double spotsize = (useSpotArea) ? Math.Pow(constant2 * distance / 2, 2) * 3.14 : constant2 * distance;
                            occlusion.IsOccluded(source, dest, wavelengthList[n], out CelestialBody celestial, out bool occluded);


                            // adding EC that has been received
                            if (recvSize < spotsize)
                            {
                                if (occluded == false)
                                {
                                    received_power += ((recvSize / spotsize) * recvEfficiency * excess2 * (percentagePower / 100))
                                        * relativistic.RedOrBlueShift(VesselList[n], thisPart.vessel, state, out state)
                                        * ((useFacingVector) ? FractionalFlux(source, dest, thisPart) : 1);
                                }
                            }
                            else
                            {
                                if (occluded == false)
                                {
                                    received_power += (recvEfficiency * excess2 * (percentagePower / 100))
                                        * relativistic.RedOrBlueShift(VesselList[n], thisPart.vessel, state, out state)
                                        * ((useFacingVector) ? FractionalFlux(source, dest, thisPart) : 1);
                                }
                            }
                            if (occluded)
                            {
                                planetList.Add(celestial);
                            }
                        }
                    }
                    if (planetList.Count > 0)
                    {
                        state = occludedby + planetList[planetList.Count - 1].GetDisplayName().TrimEnd('N', '^');
                    }
                    else
                    {
                        state = operational;
                    }
                    if (relativistic.WarpDriveEngaged(thisPart) & state != warpEngaged)
                    {
                        received_power = 0d;
                        state = warpEngaged;
                    }
                }
            }
            status = state;
        }
    }
}
