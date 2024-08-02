using System;
using UnityEngine;
using BeamedPowerStandalone;
using KSP.Localization;

namespace BeamedPowerPropulsion
{
    // part module for beamed power thermal engines (microwave thermal engine analog)
    // it has to be applied to a part with ModuleEnginesFX
    public class ThermalEngine : PartModule
    {
        [KSPField(guiName = "Received Power", isPersistant = false, guiActive = true, guiActiveEditor = false, guiUnits = "kW")]
        public float ReceivedPower;

        [KSPField(guiName = "Received Power Limiter", isPersistant = false, guiActive = true, guiActiveEditor = false, guiUnits = "%"), UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 1, scene = UI_Scene.Flight)]
        public float PowerLimiter;

        [KSPField(guiName = "Receiver Status", guiActive = true, guiActiveEditor = false, isPersistant = false)]
        public string State;

        [KSPField(guiName = "Core Temperature", groupName = "HeatInfo", groupDisplayName = "Thermal Receiver Heat Info", groupStartCollapsed = false, guiActive = true, guiActiveEditor = false, isPersistant = false, guiUnits = "")]
        public float CoreTemp;

        [KSPField(guiName = "Skin Temperature", groupName = "HeatInfo", guiActive = true, guiActiveEditor = false, isPersistant = false, guiUnits = "")]
        public float SkinTemp;

        [KSPField(guiName = "Waste Heat", groupName = "HeatInfo", guiActive = true, guiActiveEditor = false, isPersistant = false, guiUnits = "kW")]
        public float WasteHeat;

        [KSPField(guiName = "Propellant", isPersistant = false, guiActive = true, guiActiveEditor = false)]
        public string PropellantName;

        // parameters set in part.cfg file
        [KSPField(isPersistant = false)]
        public float recvDiameter;

        [KSPField(isPersistant = false)]
        public float recvEfficiency;        // efficiency of built-in thermal receiver

        [KSPField(isPersistant = false)]
        public float thermalEfficiency;     // meant to be engine heat exchanger's efficiency in tranferring heat to propellant

        [KSPField(isPersistant = false)]
        public float thrustMult = 1f;

        [KSPField(isPersistant = false)]
        public string IsJetEngine = "False";

        [KSPField(isPersistant = false)]
        public float maxCoreTemp = 1000f;

        [KSPField(isPersistant = false)]
        public float maxSkinTemp = 1200f;

        //declaring frequently used variables
        int initFrames; ModuleEnginesFX engine; Wavelengths waves = new Wavelengths();
        ModuleCoreHeat coreHeat; ReceivedPower receiver; float percentThrust;
        readonly int EChash = PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id;
        string operational = Localizer.Format("#LOC_BeamedPower_status_Operational");
        string ExceedTempLimit = Localizer.Format("#LOC_BeamedPower_status_ExceededTempLimit");
        string engineOff = Localizer.Format("#LOC_BeamedPower_ThermalEngine_EngineOff");

        public void Start()
        {
            initFrames = 0;
            try
            {
                engine = this.part.Modules.GetModule<ModuleEnginesFX>();
            }
            catch
            {
                Debug.LogError(("BeamedPowerPropulsion.ThermalEngine : ModuleEnginesFX not found on part-" + this.part.partName));
            }
            receiver = new ReceivedPower();
            Fields["CoreTemp"].guiUnits = "K/" + maxCoreTemp.ToString() + "K";
            Fields["SkinTemp"].guiUnits = "K/" + maxSkinTemp.ToString() + "K";
            
            try
            {
                // editing engine's thrust limiter field's gui
                engine.Fields["thrustPercentage"].guiName = Localizer.Format("#LOC_BeamedPower_PercentMaxThrust");
                engine.Fields["thrustPercentage"].guiUnits = "%";
                ((UI_FloatRange)engine.Fields["thrustPercentage"].uiControlFlight).scene = UI_Scene.Flight;
                engine.Fields["thrustPercentage"].guiActiveEditor = false;
            }
            catch
            {
                Debug.LogWarning("BeamedPowerPropulsion.ThermalEngine : Unable to edit engine module Field");
            }

            SetHeatParams();
            SetLocalization();
        }

        private void SetHeatParams()
        {
            this.part.AddModule("ModuleCoreHeat");
            coreHeat = this.part.Modules.GetModule<ModuleCoreHeat>();
            coreHeat.CoreTempGoal = maxCoreTemp * 1.4;  // placeholder value, there is no optimum temperature
            coreHeat.CoolantTransferMultiplier *= 2d;
            coreHeat.radiatorCoolingFactor *= 1.5d;
        }

        private void SetLocalization()
        {
            // flight
            Fields["ReceivedPower"].guiName = Localizer.Format("#LOC_BeamedPower_RecvPower");
            Fields["PowerLimiter"].guiName = Localizer.Format("#LOC_BeamedPower_RecvPowerLimiter");
            Fields["State"].guiName = Localizer.Format("#LOC_BeamedPower_ThermalEngine_RecvStatus");
            Fields["CoreTemp"].guiName = Localizer.Format("#LOC_BeamedPower_CoreTemp");
            Fields["SkinTemp"].guiName = Localizer.Format("#LOC_BeamedPower_SkinTemp");
            Fields["WasteHeat"].guiName = Localizer.Format("#LOC_BeamedPower_WasteHeat");
            Fields["CoreTemp"].group.displayName = Localizer.Format("#LOC_BeamedPower_ThermalReceiverHeatInfo");
            Fields["PropellantName"].guiName = Localizer.Format("#LOC_BeamedPower_Propellant");
            //editor
            Fields["Distance"].guiName = Localizer.Format("#LOC_BeamedPower_CalcDistance");
            Fields["Distance"].group.displayName = Localizer.Format("#LOC_BeamedPower_ThrustCalcName");
            Fields["SourceDishDia"].guiName = Localizer.Format("#LOC_BeamedPower_CalcSourceDishDia");
            Fields["CalcEfficiency"].guiName = Localizer.Format("#LOC_BeamedPower_CalcSourceEfficiency");
            Fields["BeamedPower"].guiName = Localizer.Format("#LOC_BeamedPower_CalcPowerBeamed");
            Fields["Isp"].guiName = Localizer.Format("#LOC_BeamedPower_CalcEngineIsp");
            Fields["Thrust"].guiName = Localizer.Format("#LOC_BeamedPower_CalcThrust");
            Fields["CalcWavelength"].guiName = Localizer.Format("#LOC_BeamedPower_CalcWavelength");
            Events["ToggleWavelength"].guiName = Localizer.Format("#LOC_BeamedPower_CalcToggleWavelength");
        }

        // adding part info to part description tab in editor
        public string GetModuleTitle()
        {
            return "ThermalEngine";
        }
        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_BeamedPower_ThermalEngine_ModuleName");   // "Thermal Engine"
        }
        public override string GetInfo()
        {
            return Localizer.Format("#LOC_BeamedPower_ThermalEngine_ModuleInfo",
                (recvDiameter).ToString(),
                (recvEfficiency * 100).ToString(),
                (thermalEfficiency * 100).ToString(),
                maxCoreTemp.ToString(),
                maxSkinTemp.ToString());
        }

        // adds heat to receiver core, not engine core
        // which is why the temp limits are so low
        private void AddHeatToReceiverCore()
        {
            CoreTemp = (float)(Math.Round(coreHeat.CoreTemperature, 1));
            SkinTemp = (float)(Math.Round(this.part.skinTemperature, 1));
            
            if (CoreTemp > maxCoreTemp | SkinTemp > maxSkinTemp)
            {
                State = ExceedTempLimit;
                engine.Shutdown(); PowerLimiter = 0;
            }
            if (State == ExceedTempLimit & (CoreTemp >= maxCoreTemp * 0.7 | SkinTemp >= maxSkinTemp * 0.7))
            {
                engine.Shutdown(); PowerLimiter = 0;
            }
            else if (CoreTemp < maxCoreTemp * 0.7 & SkinTemp < maxSkinTemp * 0.7)
            {
                State = operational;
            }
            double heatModifier = (double)HighLogic.CurrentGame.Parameters.CustomParams<BPSettings>().PercentHeat / 100;
            double heatExcess = (ReceivedPower * (1f / recvEfficiency - thermalEfficiency)) * heatModifier;

            WasteHeat = (float)Math.Round(heatExcess, 1);
            coreHeat.AddEnergyToCore(heatExcess * 0.5 * TimeWarp.fixedDeltaTime);  // first converted to kJ
            this.part.AddSkinThermalFlux(heatExcess * 0.4);     // waste heat from receiver + waste heat from engine
        }

        private void LockGimbal()
        {
            if (this.part.Modules.Contains<ModuleGimbal>() && engine.GetCurrentThrust() < 1f)
            {
                this.part.Modules.GetModule<ModuleGimbal>().gimbalActive = false;
            }
            else if (this.part.Modules.Contains<ModuleGimbal>() && engine.GetCurrentThrust() > 1f)
            {
                this.part.Modules.GetModule<ModuleGimbal>().gimbalActive = true;
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
                    AddHeatToReceiverCore();
                }
                
                // received power code
                receiver.Spherical(this.part, true, PowerLimiter, recvDiameter, recvEfficiency, false, false, State,
                    out State, out double recvPower);

                if (engine.getFlameoutState | !engine.getIgnitionState)
                {
                    recvPower = 0d;
                    State = engineOff;
                }
                ReceivedPower = (float)(Math.Round(recvPower, 1));

                // code related to engine module
                float currentisp = engine.realIsp;
                float ApproxTemp = (IsJetEngine == "True") ? 1100f : 2400f;

                PropellantName = engine.propellants[0].displayName; string propellantname = engine.propellants[0].name;
                float shc = PartResourceLibrary.Instance.GetDefinition(propellantname).specificHeatCapacity * 1.5f / 1000f;    // in kJ kg^-1 K^-1

                // calculate thrust based on power received
                float Thrust = (ReceivedPower * thermalEfficiency * 0.4f / (shc * ApproxTemp)) * 9.8f * currentisp;   // in kN
                Thrust = (Thrust < 10f) ? 0f : Thrust; // minimum thrust (min received power) below this there isnt enough heat to reach optimum temperature
                percentThrust = Thrust * thrustMult / engine.maxThrust;
                engine.thrustPercentage = Mathf.Clamp((float)Math.Round(percentThrust * 100, 2), 0f, 100f);

                this.vessel.GetConnectedResourceTotals(EChash, out double ECamount, out double maxAmount);
                if (ECamount / maxAmount < 0.05f)
                {
                    engine.thrustPercentage = 0f;
                }

                LockGimbal();
            }
        }

        // thrust calculator
        [KSPField(guiName = "Distance", groupName = "calculator4", groupDisplayName = "Thrust Calculator", groupStartCollapsed = true, guiUnits = "Mm", guiActive = false, guiActiveEditor = true), UI_FloatRange(minValue = 0, maxValue = 10000000, stepIncrement = 0.001f, scene = UI_Scene.Editor)]
        public float Distance;

        [KSPField(guiName = "Source Dish Diameter", groupName = "calculator4", guiUnits = "m", guiActive = false, guiActiveEditor = true), UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 0.5f, scene = UI_Scene.Editor)]
        public float SourceDishDia;

        [KSPField(guiName = "Source Efficiency", groupName = "calculator4", guiActive = false, guiActiveEditor = true, guiUnits = "%"), UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 1, scene = UI_Scene.Editor)]
        public float CalcEfficiency;

        [KSPField(guiName = "Power Beamed", groupName = "calculator4", guiUnits = "EC/s", guiActive = false, guiActiveEditor = true), UI_FloatRange(minValue = 0, maxValue = 100000, stepIncrement = 1, scene = UI_Scene.Editor)]
        public float BeamedPower;

        [KSPField(guiName = "Engine Isp", groupName = "calculator4", guiUnits = "s", guiActive = false, guiActiveEditor = true), UI_FloatRange(minValue = 0, maxValue = 1500, stepIncrement = 5, scene = UI_Scene.Editor)]
        public float Isp;

        [KSPField(guiName = "Thrust", groupName = "calculator4", guiUnits = "kN", guiActive = false, guiActiveEditor = true)]
        public float Thrust;

        [KSPField(guiName = "Beamed Wavelength", groupName = "calculator4", guiActiveEditor = true, guiActive = false)]
        public string CalcWavelength = Localizer.Format("#LOC__BeamedPower_Wavelength_gamma");

        double wavelength_num = 5E-11d; int wavelengthIndex = 0;

        [KSPEvent(guiName = "Toggle Wavelength", guiActive = false, guiActiveEditor = true, groupName = "calculator4")]
        public void ToggleWavelength()
        {
            wavelengthIndex = (wavelengthIndex < 5) ? wavelengthIndex + 1 : 0;
            string[] all_wavelengths = new string[] { "GammaRays", "XRays", "Ultraviolet", "Infrared", "Microwaves", "Radiowaves" };
            waves.Wavelength(all_wavelengths[wavelengthIndex], out _, out _, out CalcWavelength);
            wavelength_num = waves.WavelengthNum(this.part, all_wavelengths[wavelengthIndex]);
        }
        
        public void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                float spotSize = (float)(1.44 * wavelength_num * Distance * 1000000 / SourceDishDia);
                double powerReceived = (spotSize > recvDiameter) ?
                    recvDiameter / spotSize * BeamedPower * (CalcEfficiency / 100) * recvEfficiency : BeamedPower * (CalcEfficiency / 100) * recvEfficiency;

                string propellantname = engine.propellants[0].name;
                float shc = PartResourceLibrary.Instance.GetDefinition(propellantname).specificHeatCapacity * 1.5f / 1000f;    // in kJ kg^-1 K^-1
                Thrust = Mathf.Clamp((float)Math.Round(((powerReceived * thermalEfficiency * 0.4f) / (shc * 2400f)) * 9.8f * Isp, 1), 
                    0f, engine.GetMaxThrust());
                Thrust = (Thrust < 10f) ? 0f : Thrust * thrustMult;
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                engine.thrustPercentage = Mathf.Clamp((float)Math.Round(percentThrust * 100, 2), 0f, 100f);
                LockGimbal();
            }
        }
    }
}
