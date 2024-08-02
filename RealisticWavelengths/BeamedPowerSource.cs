using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;

namespace BeamedPowerStandalone
{
    public class WirelessSource : PartModule
    {
        // creating things on part right click menu (flight)
        [KSPField(guiName = "Power Transmitter", isPersistant = true, guiActive = true, guiActiveEditor = false), UI_Toggle(scene = UI_Scene.Flight)]
        public bool Transmitting;

        [KSPField(guiName = "Power to Beam", isPersistant = true, guiActive = true, guiActiveEditor = false, guiUnits = "EC/s"), UI_FloatRange(minValue = 0, maxValue = 100000, stepIncrement = 1, scene = UI_Scene.Flight)]
        public float PowerBeamed;

        [KSPField(guiName = "Beamed Power", isPersistant = true, guiActive = true, guiActiveEditor = false, guiUnits = "EC/s", guiActiveUnfocused = true, unfocusedRange = 10000000000000)]
        public float Excess;

        [KSPField(isPersistant = true)]
        public float Constant;

        [KSPField(guiName = "Transmitting To", isPersistant = true, guiActive = true, guiActiveEditor = false)]
        public string TransmittingTo;

        [KSPField(guiName = "Status", isPersistant = false, guiActive = true, guiActiveEditor = false)]
        public string State;

        [KSPField(guiName = "Core Temperature", groupName = "HeatInfo", groupDisplayName = "Heat Info", groupStartCollapsed = false, guiActive = true, guiActiveEditor = false, isPersistant = false)]
        public float CoreTemp;

        [KSPField(guiName = "Skin Temperature", groupName = "HeatInfo", guiActive = true, guiActiveEditor = false, isPersistant = false)]
        public float SkinTemp;

        [KSPField(guiName = "Waste Heat", groupName = "HeatInfo", guiActive = true, guiActiveEditor = false, isPersistant = false, guiUnits = "kW")]
        public float WasteHeat;

        // 'dish_diameter', 'efficiency', and 'wavelength' are set in part.cfg file:
        [KSPField(isPersistant = false)]
        public float DishDiameter;

        [KSPField(isPersistant = true)]
        public string Wavelength;

        [KSPField(isPersistant = true)]
        public float Efficiency;
         
        [KSPField(isPersistant = false)]
        public float maxCoreTemp = 900f;

        [KSPField(isPersistant = false)]
        public float maxSkinTemp = 1200f;

        List<ConfigNode> receiversList; int frames; int initFrames;
        VesselFinder vesselFinder = new VesselFinder(); ModuleCoreHeat coreHeat;
        Wavelengths waves = new Wavelengths();

        string operational = Localizer.Format("#LOC_BeamedPower_status_Operational");
        string ExceedTempLimit = Localizer.Format("#LOC_BeamedPower_status_ExceededTempLimit");
        string VesselNone = Localizer.Format("#LOC_BeamedPower_Vessel_None");

        public void Start()
        {
            frames = 20; initFrames = 0;
            receiversList = new List<ConfigNode>();
            Fields["CoreTemp"].guiUnits = "K/" + maxCoreTemp.ToString() + "K";
            Fields["SkinTemp"].guiUnits = "K/" + maxSkinTemp.ToString() + "K";

            SetHeatParams();
            SetLocalization();
        }

        private void SetLocalization()
        {
            //flight
            Fields["Transmitting"].guiName = Localizer.Format("#LOC_BeamedPower_WirelessSource_PowerTransmitter");
            Fields["PowerBeamed"].guiName = Localizer.Format("#LOC_BeamedPower_WirelessSource_PowerToBeam");
            Fields["Excess"].guiName = Localizer.Format("#LOC_BeamedPower_WirelessSource_BeamedPower");
            Fields["TransmittingTo"].guiName = Localizer.Format("#LOC_BeamedPower_WirelessSource_TransmittingTo");
            Fields["State"].guiName = Localizer.Format("#LOC_BeamedPower_Status");
            Fields["CoreTemp"].guiName = Localizer.Format("#LOC_BeamedPower_CoreTemp");
            Fields["SkinTemp"].guiName = Localizer.Format("#LOC_BeamedPower_SkinTemp");
            Fields["WasteHeat"].guiName = Localizer.Format("#LOC_BeamedPower_WasteHeat");
            Fields["CoreTemp"].group.displayName = Localizer.Format("#LOC_BeamedPower_HeatInfo");
            Events["VesselCounter"].guiName = Localizer.Format("#LOC_BeamedPower_Vessels_Cyclethrough");
            Actions["ToggleBPTransmitter"].guiName = Localizer.Format("#LOC_BeamedPower_Actions_ToggleSource");
            Actions["ActivateBPTransmitter"].guiName = Localizer.Format("#LOC_BeamedPower_Actions_ActivateSource");
            Actions["DeactivateBPTransmitter"].guiName = Localizer.Format("#LOC_BeamedPower_Actions_DeactivateSource");
            //editor
            Fields["Distance"].guiName = Localizer.Format("#LOC_BeamedPower_CalcDistance");
            Fields["RecvDiameter"].guiName = Localizer.Format("#LOC_BeamedPower_CalcRecvDiameter");
            Fields["RecvEfficiency"].guiName = Localizer.Format("#LOC_BeamedPower_CalcRecvEfficiency");
            Fields["PowerBeamed"].guiName = Localizer.Format("#LOC_BeamedPower_CalcPowerBeamed");
            Fields["PowerReceived"].guiName = Localizer.Format("#LOC_BeamedPower_CalcResult");
        }

        private void SetHeatParams()
        {
            this.part.AddModule("ModuleCoreHeat");
            coreHeat = this.part.Modules.GetModule<ModuleCoreHeat>();
            coreHeat.CoreTempGoal = maxCoreTemp * 1.4;  // placeholder value, there is no optimum temperature
            coreHeat.CoolantTransferMultiplier *= 2d;
            coreHeat.HeatRadiantMultiplier *= 2d;
        }

        [KSPField(isPersistant = true)]
        public int counter;

        [KSPEvent(guiName = "Cycle through vessels", guiActive = true, isPersistent = false, requireFullControl = true)]
        public void VesselCounter()
        {
            counter = (counter < receiversList.Count - 1) ? counter += 1 : counter = 0;
        }

        // getting resource id of 'Electric Charge'
        public int EChash = PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id;

        // setting action group capability
        [KSPAction(guiName = "Toggle Power Transmitter")]
        public void ToggleBPTransmitter(KSPActionParam param)
        {
            Transmitting = Transmitting ? false : true;
        }

        [KSPAction(guiName = "Activate Power Transmitter")]
        public void ActivateBPTransmitter(KSPActionParam param)
        {
            Transmitting = Transmitting ? true : true;
        }

        [KSPAction(guiName = "Deactivate Power Transmitter")]
        public void DeactivateBPTransmitter(KSPActionParam param)
        {
            Transmitting = Transmitting ? false : false;
        }

        // adding part info to part description tab in editor
        public string GetModuleTitle()
        {
            return "WirelessSource";
        }
        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_BeamedPower_WirelessSource_ModuleName");
        }
        public override string GetInfo()
        {
            waves.Wavelength(Wavelength, out _ , out _, out string wavelengthLocalized);

            return Localizer.Format("#LOC_BeamedPower_WirelessSource_ModuleInfo",
                DishDiameter.ToString(),
                wavelengthLocalized,
                (Efficiency * 100).ToString(),
                maxCoreTemp.ToString(),
                maxSkinTemp.ToString());
        }

        private void AddHeatToCore()
        {
            CoreTemp = (float)(Math.Round(coreHeat.CoreTemperature, 1));
            SkinTemp = (float)(Math.Round(this.part.skinTemperature, 1));

            if (CoreTemp > maxCoreTemp | SkinTemp > maxSkinTemp)
            {
                State = ExceedTempLimit;
                Transmitting = false;
            }
            if (State == ExceedTempLimit & (CoreTemp >= maxCoreTemp * 0.7 | SkinTemp >= maxSkinTemp * 0.7))
            {
                Transmitting = false;
            }
            else if (CoreTemp < maxCoreTemp * 0.7 & SkinTemp < maxSkinTemp * 0.7)
            {
                State = operational;
            }
            double heatModifier = (double)HighLogic.CurrentGame.Parameters.CustomParams<BPSettings>().PercentHeat / 100;
            double heatExcess = (1 - Efficiency) * (Excess / Efficiency) * heatModifier;
            WasteHeat = (float)Math.Round(heatExcess, 1);
            coreHeat.AddEnergyToCore(heatExcess * 0.7 * TimeWarp.fixedDeltaTime);  // first converted to kJ
            this.part.AddSkinThermalFlux(heatExcess * 0.3);      // some heat added to skin
        }

        private void SyncAnimationState()
        {
            if (part.Modules.Contains<ModuleDeployableAntenna>())
            {
                if (part.Modules.GetModule<ModuleDeployableAntenna>().deployState != ModuleDeployableAntenna.DeployState.EXTENDED)
                {
                    Transmitting = false;
                }
            }
        }

        // main block of code - runs every physics frame
        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (initFrames < 60)
                {
                    initFrames += 1;
                }
                else
                {
                    AddHeatToCore();
                }
                SyncAnimationState();

                this.vessel.GetConnectedResourceTotals(EChash, out double amount, out double maxAmount);
                if (amount/maxAmount < 0.2d)
                {
                    PowerBeamed = 0f;
                }

                if (Transmitting)
                {
                    frames += 1;
                    if (frames == 40)
                    {
                        try
                        {
                            vesselFinder.ReceiverData(this.vessel.GetDisplayName(), out receiversList);
                        }
                        catch
                        {
                            Debug.LogError("BeamedPowerStandalone.WirelessSource : Unable to load receiver vessel list.");
                        }
                        frames = 0;
                    }

                    counter = (counter < receiversList.Count) ? counter : counter = 0;
                    try
                    {
                        TransmittingTo = receiversList[counter].GetValue("name");
                    }
                    catch
                    {
                        TransmittingTo = VesselNone;
                    }

                    // a bunch of math
                    double wavelengthNum = waves.WavelengthNum(this.part, Wavelength);
                    Constant = Convert.ToSingle((1.44 * wavelengthNum) / DishDiameter);
                    Excess = Convert.ToSingle(Math.Round((PowerBeamed * Efficiency), 1));

                    if (HighLogic.CurrentGame.Parameters.CustomParams<BPSettings>().BackgroundProcessing == false)
                    {
                        // reducing amount of EC in craft in each frame (makes it look like continuous EC consumption)
                        this.part.RequestResource(EChash, (double)(PowerBeamed * TimeWarp.fixedDeltaTime));
                    }
                }
                else
                {
                    Excess = 0; 
                }
            }
        }

        // adds received power calculator in transmitter part's right click menu in editor
        [KSPField(guiName = "Distance", groupName = "calculator3", groupDisplayName = "Received Power Calculator", groupStartCollapsed = true, guiUnits = "Mm", guiActive = false, guiActiveEditor = true), UI_FloatRange(minValue = 0, maxValue = 10000000, stepIncrement = 0.001f, scene = UI_Scene.Editor)]
        public float Distance;

        [KSPField(guiName = "Receiver Diameter", groupName = "calculator3", guiUnits = "m", guiActive = false, guiActiveEditor = true), UI_FloatRange(minValue = 0, maxValue = 50, stepIncrement = 0.5f, scene = UI_Scene.Editor)]
        public float RecvDiameter;

        [KSPField(guiName = "Receiver Efficiency", groupName = "calculator3", guiActive = false, guiActiveEditor = true, guiUnits = "%"), UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 1, scene = UI_Scene.Editor)]
        public float RecvEfficiency;

        [KSPField(guiName = "Power Beamed", groupName = "calculator3", guiUnits = "EC/s", guiActive = false, guiActiveEditor = true), UI_FloatRange(minValue = 0, maxValue = 100000, stepIncrement = 1, scene = UI_Scene.Editor)]
        public float BeamedPower;

        [KSPField(guiName = "Result", groupName = "calculator3", guiUnits = "EC/s", guiActive = false, guiActiveEditor = true)]
        public float PowerReceived;

        public void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                float spot_size = (float)(1.44 * waves.WavelengthNum(this.part, Wavelength) * Distance * 1000000d / DishDiameter);
                PowerReceived = (spot_size > RecvDiameter) ?
                    RecvDiameter / spot_size * BeamedPower * Efficiency * (RecvEfficiency / 100) : BeamedPower * Efficiency * (RecvEfficiency / 100);
                PowerReceived = (float)Math.Round(PowerReceived, 1);
            }
        }
    }
}
