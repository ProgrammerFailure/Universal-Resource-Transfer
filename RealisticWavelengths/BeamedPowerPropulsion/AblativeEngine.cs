using System;
using UnityEngine;
using KSP.Localization;
using BeamedPowerStandalone;

namespace BeamedPowerPropulsion
{
    public class AblativeEngine : PartModule
    {
        [KSPField(guiName = "Received Power", isPersistant = true, guiActive = true, guiActiveEditor = false, guiUnits = "kW")]
        public float ReceivedPower;

        [KSPField(guiName = "Received Power Limiter", isPersistant = true, guiActive = true, guiActiveEditor = false, guiUnits = "%"), UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 1, requireFullControl = true, scene = UI_Scene.Flight)]
        public float PowerLimiter;

        [KSPField(guiName = "Receiving from", isPersistant = false, guiActive = true, guiActiveEditor = false)]
        public string ReceivingFrom;

        [KSPField(guiName = "Power Status", guiActive = true, guiActiveEditor = false, isPersistant = false)]
        public string State;

        [KSPField(guiName = "Skin Temperature", guiActive = true, guiActiveEditor = false, isPersistant = false, groupName = "HeatInfo", groupDisplayName = "Heat Info", groupStartCollapsed = false)]
        public float SkinTemp;

        [KSPField(guiName = "Propellant", isPersistant = false, guiActive = true, guiActiveEditor = false)]
        public string PropellantName;

        [KSPField(guiName = "Propellant Loss", isPersistant = false, guiActive = true, guiActiveEditor = false, guiUnits = "U/s")]
        public float Loss;

        // parameters set in part.cfg file
        [KSPField(isPersistant = false)]
        public float SurfaceArea;

        [KSPField(isPersistant = false)]
        public float Efficiency;

        [KSPField(isPersistant = false)]
        public float thrustMult = 1f;

        // increment through each transmitter vessel
        [KSPField(isPersistant = true)]
        public int counter;

        [KSPEvent(guiName = "Cycle through vessels", guiActive = true, isPersistent = false, requireFullControl = true)]
        public void VesselCounter()
        {
            counter += 1;
        }

        ModuleEnginesFX engine; ReceivedPower receiver; Wavelengths waves = new Wavelengths(); float percentThrust;
        readonly int EChash = PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id;
        string operational = Localizer.Format("#LOC_BeamedPower_status_Operational");
        string engineOff = Localizer.Format("#LOC_BeamedPower_ThermalEngine_EngineOff");

        public void Start()
        {
            Fields["SkinTemp"].guiUnits = "K/" + this.part.maxTemp.ToString() + "K";
            try
            {
                engine = this.part.Modules.GetModule<ModuleEnginesFX>();
            }
            catch
            {
                Debug.LogError(("BeamedPowerPropulsion.AblativeEngine : ModuleEnginesFX not found on part-" + this.part.partName));
            }
            receiver = new ReceivedPower();
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
                Debug.LogWarning("BeamedPowerPropulsion.AblativeEngine : Unable to edit engine module Field");
            }

            SetLocalization();
        }

        private void SetLocalization()
        {
            //flight
            Fields["ReceivedPower"].guiName = Localizer.Format("#LOC_BeamedPower_RecvPower");
            Fields["PowerLimiter"].guiName = Localizer.Format("#LOC_BeamedPower_RecvPowerLimiter");
            Fields["ReceivingFrom"].guiName = Localizer.Format("#LOC_BeamedPower_RecvFrom");
            Fields["State"].guiName = Localizer.Format("#LOC_BeamedPower_AblativeEngine_PowerStatus");
            Fields["SkinTemp"].guiName = Localizer.Format("#LOC_BeamedPower_SkinTemp");
            Fields["SkinTemp"].group.displayName = Localizer.Format("#LOC_BeamedPower_HeatInfo");
            Fields["PropellantName"].guiName = Localizer.Format("#LOC_BeamedPower_Propellant");
            Fields["Loss"].guiName = Localizer.Format("#LOC_BeamedPower_AblativeEngine_PropellantLoss");
            Events["VesselCounter"].guiName = Localizer.Format("#LOC_BeamedPower_Vessels_Cyclethrough");
            //editor
            Fields["Distance"].guiName = Localizer.Format("#LOC_BeamedPower_CalcDistance");
            Fields["Distance"].group.displayName = Localizer.Format("#LOC_BeamedPower_ThrustCalcName");
            Fields["SourceDishDia"].guiName = Localizer.Format("#LOC_BeamedPower_CalcSourceDishDia");
            Fields["CalcEfficiency"].guiName = Localizer.Format("#LOC_BeamedPower_CalcSourceEfficiency");
            Fields["BeamedPower"].guiName = Localizer.Format("#LOC_BeamedPower_CalcPowerBeamed");
            Fields["Isp"].guiName = Localizer.Format("#LOC_BeamedPower_CalcEngineIsp");
            Fields["Angle"].guiName = Localizer.Format("#LOC_BeamedPower_AblativeEngine_CalcAngle");
            Fields["Thrust"].guiName = Localizer.Format("#LOC_BeamedPower_CalcThrust");
            Fields["CalcWavelength"].guiName = Localizer.Format("#LOC_BeamedPower_CalcWavelength");
            Events["ToggleWavelength"].guiName = Localizer.Format("#LOC_BeamedPower_CalcToggleWavelength");
        }

        public string GetModuleTitle()
        {
            return "AblativeEngine";
        }
        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_BeamedPower_AblativeEngine_ModuleName");
        }
        public override string GetInfo()
        {
            return Localizer.Format("#LOC_BeamedPower_AblativeEngine_ModuleInfo",
                SurfaceArea.ToString(),
                (Efficiency * 100).ToString());
        }

        // main block of code runs every physics frame only in flight scene
        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                State = operational;
                
                // received power code
                receiver.Directional(this.part, counter, true, PowerLimiter, SurfaceArea, Efficiency, true, true,
                    State, out State, out ReceivingFrom, out double received_power, out counter);

                this.part.GetConnectedResourceTotals(engine.propellants[0].id, out double amount, out _);
                if (amount <= 0.5d)
                {
                    received_power = 0f;
                    State = engineOff;
                }

                if (received_power > 0f)
                {
                    ReceivedPower = (float)Math.Round(received_power, 1);
                }
                else
                {
                    ReceivedPower = 0f;
                }

                // code related to engine module
                float currentisp = engine.realIsp;

                // getting propellant resource definition
                PropellantName = engine.propellants[0].displayName; string propellantname = engine.propellants[0].name;
                float shc = PartResourceLibrary.Instance.GetDefinition(propellantname).specificHeatCapacity * 1.5f / 1000f;    // in kJ kg^-1 K^-1
                float density = PartResourceLibrary.Instance.GetDefinition(propellantname).density * 1000;  // in kg/l

                // calculate thrust based on power received
                float Thrust = (float)(received_power * 0.2f / (shc * 8600f)) * 9.8f * currentisp;    // in kN
                Thrust = (Thrust < 20f) ? 0f : Thrust; // minimum thrust (min received power) below this there isnt enough heat to exceed metal's boiling point

                // propellant loss
                Loss = (float)((received_power / Efficiency) * (1 - Efficiency) * 0.2f / (shc * 2600f * density));  // units/s (l/s)
                Loss = (engine.GetCurrentThrust() > 1f) ? Loss : 0f;
                this.part.RequestResource(propellantname, (double)Loss * Time.fixedDeltaTime);

                // adding heat to part's skin
                double heatModifier = HighLogic.CurrentGame.Parameters.CustomParams<BPSettings>().PercentHeat;    // in kW
                this.part.AddThermalFlux(received_power * (1 - Efficiency) * 0.8 * (heatModifier / 100));

                // adjust thrust limiter, based on what max thrust should be as calculated
                percentThrust = Thrust * thrustMult / engine.maxThrust;
                engine.thrustPercentage = Mathf.Clamp((float)Math.Round(percentThrust * 100, 2), 0f, 100f);

                this.vessel.GetConnectedResourceTotals(EChash, out double ECamount, out double maxAmount);
                if (ECamount / maxAmount < 0.01f)
                {
                    engine.thrustPercentage = 0f;
                }
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

        [KSPField(guiName = "Engine Isp", groupName = "calculator4", guiUnits = "s", guiActive = false, guiActiveEditor = true), UI_FloatRange(minValue = 0, maxValue = 5000, stepIncrement = 5, scene = UI_Scene.Editor)]
        public float Isp;

        [KSPField(guiName = "Angle to source", groupName = "calculator4", guiUnits = "°", guiActive = false, guiActiveEditor = true), UI_FloatRange(minValue = 0, maxValue = 90, stepIncrement = 1, scene = UI_Scene.Editor)]
        public float Angle;

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

        // only runs in editor (code for thrust calculator)
        public void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                engine.thrustPercentage = 100f;
                float spotArea = (float)Math.Pow((1.44 * wavelength_num * Distance * 1000000 / SourceDishDia)/2, 2) * 3.14f;
                double powerReceived = (spotArea > SurfaceArea) ?
                    (SurfaceArea / spotArea) * BeamedPower * (CalcEfficiency / 100) * Efficiency : BeamedPower * (CalcEfficiency / 100) * Efficiency;
                powerReceived *= Math.Cos(Angle * Math.PI / 180);

                string propellantname = engine.propellants[0].name;
                float shc = PartResourceLibrary.Instance.GetDefinition(propellantname).specificHeatCapacity * 1.5f / 1000f;    // in kJ kg^-1 K^-1
                Thrust = Mathf.Clamp((float)Math.Round((powerReceived * 0.2f / (shc * 8600f)) * 9.8d * Isp, 1), 
                    0f, engine.GetMaxThrust());
                Thrust = (Thrust < 20f) ? 0f : Thrust;
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                engine.thrustPercentage = Mathf.Clamp((float)Math.Round(percentThrust * 100, 2), 0f, 100f);
            }
        }
    }
}
