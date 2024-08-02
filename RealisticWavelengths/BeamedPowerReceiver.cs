using System;
using UnityEngine;
using KSP.Localization;

namespace BeamedPowerStandalone
{
    public class WirelessReceiver : PartModule
    {
        // UI-right click menu in flight
        [KSPField(guiName = "Power Receiver", isPersistant = true, guiActive = true, guiActiveEditor = false), UI_Toggle(scene = UI_Scene.Flight)]
        public bool Listening;

        [KSPField(guiName = "Received Power Limiter", isPersistant = true, guiActive = true, guiActiveEditor = false, guiUnits = "%"), UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 1, requireFullControl = true, scene = UI_Scene.Flight)]
        public float PowerLimiter;

        [KSPField(guiName = "Received Power", isPersistant = true, guiActive = true, guiActiveEditor = false, guiUnits = "EC/s")]
        public float ReceivedPower;

        [KSPField(guiName = "Receiving from", isPersistant = false, guiActive = true, guiActiveEditor = false)]
        public string ReceivingFrom;

        [KSPField(guiName = "Status", guiActive = true, guiActiveEditor = false, isPersistant = false)]
        public string State;

        [KSPField(guiName = "Core Temperature", groupName = "HeatInfo", groupDisplayName = "Heat Info", groupStartCollapsed = false, guiActive = true, guiActiveEditor = false, isPersistant = false)]
        public float CoreTemp;

        [KSPField(guiName = "Skin Temperature", groupName = "HeatInfo", guiActive = true, guiActiveEditor = false, isPersistant = false)]
        public float SkinTemp;

        [KSPField(guiName = "Waste Heat", groupName = "HeatInfo", guiActive = true, guiActiveEditor = false, isPersistant = false, guiUnits = "kW")]
        public float WasteHeat;

        // parameters set in part.cfg file
        [KSPField(isPersistant = false)]
        public string recvType;

        [KSPField(isPersistant = false)]
        public float recvDiameter;

        [KSPField(isPersistant = false)]
        public float recvEfficiency;

        [KSPField(isPersistant = false)]
        public float maxCoreTemp = 900f;

        [KSPField(isPersistant = false)]
        public float maxSkinTemp = 1200f;

        int initFrames; ModuleCoreHeat coreHeat; ReceivedPower receiver;
        readonly int EChash = PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id;
        string ExceedTempLimit = Localizer.Format("#LOC_BeamedPower_status_ExceededTempLimit");
        string operational = Localizer.Format("#LOC_BeamedPower_status_Operational");

        [KSPField(isPersistant = true)]
        public int counter;

        public void Start()
        {
            initFrames = 0;
            receiver = new ReceivedPower();
            Fields["CoreTemp"].guiUnits = "K/" + maxCoreTemp.ToString() + "K";
            Fields["SkinTemp"].guiUnits = "K/" + maxSkinTemp.ToString() + "K";

            if (recvType == "Spherical")
            {
                Fields["ReceivingFrom"].guiActive = false;
                Fields["counter"].isPersistant = false;
                Events["VesselCounter"].guiActive = false;
            }

            SetHeatParams();
            SetLocalization();
        }

        private void SetHeatParams()
        {
            this.part.AddModule("ModuleCoreHeat");
            coreHeat = this.part.Modules.GetModule<ModuleCoreHeat>();
            coreHeat.CoreTempGoal = maxCoreTemp * 1.4;  // placeholder value, there is no optimum temperature
            coreHeat.CoolantTransferMultiplier *= 3d;
            coreHeat.HeatRadiantMultiplier *= 2d;
        }

        private void SetLocalization()
        {
            //flight
            Fields["Listening"].guiName = Localizer.Format("#LOC_BeamedPower_Receiver_PowerReceiver");
            Fields["PowerLimiter"].guiName = Localizer.Format("#LOC_BeamedPower_RecvPowerLimiter");
            Fields["ReceivedPower"].guiName = Localizer.Format("#LOC_BeamedPower_RecvPower");
            Fields["State"].guiName = Localizer.Format("#LOC_BeamedPower_Status");
            Fields["CoreTemp"].guiName = Localizer.Format("#LOC_BeamedPower_CoreTemp");
            Fields["CoreTemp"].group.displayName = Localizer.Format("#LOC_BeamedPower_HeatInfo");
            Fields["SkinTemp"].guiName = Localizer.Format("#LOC_BeamedPower_SkinTemp");
            Fields["WasteHeat"].guiName = Localizer.Format("#LOC_BeamedPower_WasteHeat");
            Actions["ToggleBPReceiver"].guiName = Localizer.Format("#LOC_BeamedPower_Actions_ToggleReceiver");
            Actions["ActivateBPReceiver"].guiName = Localizer.Format("#LOC_BeamedPower_Actions_ActivateReceiver");
            Actions["DeactivateBPReceiver"].guiName = Localizer.Format("#LOC_BeamedPower_Actions_DeactivateReceiver");
            Events["VesselCounter"].guiName = Localizer.Format("#LOC_BeamedPower_Vessels_Cyclethrough");
            //editor
            Fields["Distance"].guiName = Localizer.Format("#LOC_BeamedPower_CalcDistance");
            Fields["Distance"].group.displayName = Localizer.Format("#LOC_BeamedPower_RecvPowerCalcName");
            Fields["SourceDishDia"].guiName = Localizer.Format("#LOC_BeamedPower_CalcSourceDishDia");
            Fields["CalcEfficiency"].guiName = Localizer.Format("#LOC_BeamedPower_CalcSourceEfficiency");
            Fields["BeamedPower"].guiName = Localizer.Format("#LOC_BeamedPower_CalcPowerBeamed");
            Fields["PowerReceived"].guiName = Localizer.Format("#LOC_BeamedPower_CalcResult");
            Fields["CalcWavelength"].guiName = Localizer.Format("#LOC_BeamedPower_CalcWavelength");
            Events["ToggleWavelength"].guiName = Localizer.Format("#LOC_BeamedPower_CalcToggleWavelength");
        }

        [KSPEvent(guiName = "Cycle through vessels", guiActive = true, isPersistent = false, requireFullControl = true)]
        public void VesselCounter()
        {
            counter += 1;
        }

        // setting action group capability
        [KSPAction(guiName = "Toggle Power Receiver")]
        public void ToggleBPReceiver(KSPActionParam param)
        {
            Listening = Listening ? false : true;
        }

        [KSPAction(guiName = "Activate Power Receiver")]
        public void ActivateBPReceiver(KSPActionParam param)
        {
            Listening = Listening ? true : true;
        }

        [KSPAction(guiName = "Deactivate Power Receiver")]
        public void DeactivateBPReceiver(KSPActionParam param)
        {
            Listening = Listening ? false : false;
        }

        // adding part info to part description tab in editor
        public string GetModuleTitle()
        {
            return "WirelessReceiver";
        }
        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_BeamedPower_Receiver_ModuleName");
        }
        public override string GetInfo()
        {
            return Localizer.Format("#LOC_BeamedPower_Receiver_ModuleInfo",
                recvType,
                recvDiameter.ToString(),
                (recvEfficiency * 100).ToString(),
                maxCoreTemp.ToString(),
                maxSkinTemp.ToString());
        }

        // adds heat mechanics to this receiver
        private void AddHeatToCore()
        {
            CoreTemp = (float)(Math.Round(coreHeat.CoreTemperature, 1));
            SkinTemp = (float)(Math.Round(this.part.skinTemperature, 1));

            if (CoreTemp > maxCoreTemp | SkinTemp > maxSkinTemp)
            {
                State = ExceedTempLimit;
                Listening = false;
            }
            if (State == ExceedTempLimit & (CoreTemp >= maxCoreTemp * 0.7 | SkinTemp >= maxSkinTemp * 0.7))
            {
                Listening = false;
            }
            else if (CoreTemp < maxCoreTemp * 0.7 & SkinTemp < maxSkinTemp * 0.7)
            {
                State = operational;
            }
            double heatModifier = (double)HighLogic.CurrentGame.Parameters.CustomParams<BPSettings>().PercentHeat / 100;
            double heatExcess = (1 - recvEfficiency) * (ReceivedPower / recvEfficiency) * heatModifier;
            WasteHeat = (float)Math.Round(heatExcess, 1);
            coreHeat.AddEnergyToCore(heatExcess * 0.3 * TimeWarp.fixedDeltaTime);  // first converted to kJ
            this.part.AddSkinThermalFlux(heatExcess * 0.7);     // some heat added to skin
        }

        private void SyncAnimationState()
        {
            if (part.Modules.Contains<ModuleDeployableAntenna>())
            {
                if (part.Modules.GetModule<ModuleDeployableAntenna>().deployState != ModuleDeployableAntenna.DeployState.EXTENDED)
                {
                    Listening = false;
                }
            }
        }

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

                double recvPower;
                if (recvType == "Directional")
                {
                    receiver.Directional(this.part, counter, Listening, PowerLimiter, recvDiameter, recvEfficiency,
                        false, false, State, out State, out ReceivingFrom, out recvPower, out counter);
                }
                else if (recvType == "Spherical")
                {
                    receiver.Spherical(this.part, Listening, PowerLimiter, recvDiameter, recvEfficiency, false,
                        false, State, out State, out recvPower);
                }
                else
                {
                    counter = 0; recvPower = 0d;
                    Debug.LogWarning("BeamedPowerStandalone.WirelessReceiver : Invalid recvType set in part.cfg of " + this.part.partName);
                }

                ReceivedPower = (float)Math.Round(recvPower, 1);

                if (HighLogic.CurrentGame.Parameters.CustomParams<BPSettings>().BackgroundProcessing == false)
                {
                    this.part.RequestResource(EChash, -(double)ReceivedPower * TimeWarp.fixedDeltaTime);
                }
            }
        }

        // adds received power calculator to receivers right-click menu in editor

        [KSPField(guiName = "Distance", groupName = "calculator1", groupDisplayName = "Received Power Calculator", groupStartCollapsed = true, guiUnits = "Mm", guiActive = false, guiActiveEditor = true, isPersistant = false), UI_FloatRange(minValue = 0, maxValue = 10000000, stepIncrement = 0.001f, scene = UI_Scene.Editor)]
        public float Distance;

        [KSPField(guiName = "Source Dish Diameter", groupName = "calculator1", guiUnits = "m", guiActive = false, guiActiveEditor = true, isPersistant = false), UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 0.5f, scene = UI_Scene.Editor)]
        public float SourceDishDia;

        [KSPField(guiName = "Source Efficiency", groupName = "calculator1", guiActive = false, guiActiveEditor = true, guiUnits = "%", isPersistant = false), UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 1, scene = UI_Scene.Editor)]
        public float CalcEfficiency;

        [KSPField(guiName = "Power Beamed", groupName = "calculator1", guiUnits = "EC/s", guiActive = false, guiActiveEditor = true, isPersistant = false), UI_FloatRange(minValue = 0, maxValue = 100000, stepIncrement = 1, scene = UI_Scene.Editor)]
        public float BeamedPower;

        [KSPField(guiName = "Result", groupName = "calculator1", guiUnits = "EC/s", guiActive = false, guiActiveEditor = true, isPersistant = false)]
        public float PowerReceived;

        [KSPField(guiName = "Beamed Wavelength", groupName = "calculator1", guiActiveEditor = true, guiActive = false, isPersistant = false)]
        public string CalcWavelength = Localizer.Format("#LOC__BeamedPower_Wavelength_gamma");

        double wavelength_num = 5E-11d; int wavelengthIndex = 0;

        [KSPEvent(guiName = "Toggle Wavelength", guiActive = false, guiActiveEditor = true, groupName = "calculator1", isPersistent = false)]
        public void ToggleWavelength()
        {
            wavelengthIndex = (wavelengthIndex < 5)? wavelengthIndex + 1 : 0;
            string[] all_wavelengths = new string[] { "GammaRays", "XRays", "Ultraviolet", "Infrared", "Microwaves", "Radiowaves"};
            waves.Wavelength(all_wavelengths[wavelengthIndex], out _, out _, out CalcWavelength);
            wavelength_num = waves.WavelengthNum(this.part, all_wavelengths[wavelengthIndex]);
        }

        Wavelengths waves = new Wavelengths();

        public void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                float spot_size = (float)(1.44 * wavelength_num * Distance * 1000000d / SourceDishDia);
                PowerReceived = (spot_size > recvDiameter) ?
                    recvDiameter / spot_size * BeamedPower * (CalcEfficiency / 100) * recvEfficiency : 
                    BeamedPower * (CalcEfficiency / 100) * recvEfficiency;
                
                PowerReceived = (float)Math.Round(PowerReceived, 1);
            }
        }
    }
}
 