using System;
using UnityEngine;
using KSP.Localization;
using BeamedPowerStandalone;

namespace BeamedPowerPropulsion
{
    // optional part module, use for photon sails powered by beamed power
    public class PhotonSail : PartModule
    {
        [KSPField(guiName = "Received power", guiActive = true, guiActiveEditor = false, guiUnits = "kW")]
        public float ReceivedPower;

        [KSPField(guiName = "Received Power Limiter", isPersistant = false, guiActive = true, guiActiveEditor = false, guiUnits = "%"), UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 1, scene = UI_Scene.Flight)]
        public float PowerLimiter;

        [KSPField(guiName = "Status", guiActive = true, guiActiveEditor = false, isPersistant = false)]
        public string State;

        [KSPField(guiName = "Skin Temperature", guiActive = true, guiActiveEditor = false, isPersistant = false, groupName = "HeatInfo", groupDisplayName = "Heat Info", groupStartCollapsed = false)]
        public float SkinTemp;

        [KSPField(guiName = "Current Thrust", guiActive = true, guiActiveEditor = false, guiUnits = "N")]
        public float ThrustN;

        // parameters set in part.cfg
        [KSPField(isPersistant = false)]
        public float SurfaceArea;

        [KSPField(isPersistant = false)]
        public float Reflectivity;

        [KSPField(isPersistant = false)]
        public float partThrustMult = 1f;

        readonly int EChash = PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id;
        ModuleEngines engine; ReceivedPower receiver; Wavelengths waves = new Wavelengths();
        const float c = 299792452;

        // a lot of the usual part.cfg parameters for engines are now set within the code itself
        public void Start()
        {
            receiver = new ReceivedPower();
            Fields["SkinTemp"].guiUnits = "K/" + this.part.skinMaxTemp.ToString() + "K";

            try
            {
                engine = this.part.Modules.GetModule<ModuleEngines>();
            }
            catch
            {
                Debug.LogError(("BeamedPowerPropulsion.PhotonSail : ModuleEngines not found on part-" + this.part.partName));
            }

            try
            {
                // hiding redundant moduleengines ui parameters
                ((UI_FloatRange)engine.Fields["thrustPercentage"].uiControlFlight).scene = UI_Scene.None;
                ((UI_FloatRange)engine.Fields["thrustPercentage"].uiControlFlight).stepIncrement = 0.001f;
                engine.Fields["thrustPercentage"].guiActive = false;
                engine.Fields["thrustPercentage"].guiActiveEditor = false;
                engine.Fields["finalThrust"].guiActive = false;
                engine.Fields["fuelFlowGui"].guiActive = false;
                engine.Fields["status"].guiActive = false;
            }
            catch
            {
                Debug.LogWarning("BeamedPowerPropulsion.PhotonSail : Unable to edit engine module Fields");
            }

            SetLocalization();
        }

        private void SetLocalization()
        {
            //flight
            Fields["ReceivedPower"].guiName = Localizer.Format("#LOC_BeamedPower_RecvPower");
            Fields["PowerLimiter"].guiName = Localizer.Format("#LOC_BeamedPower_RecvPowerLimiter");
            Fields["ThrustN"].guiName = Localizer.Format("#LOC_BeamedPower_PhotonSail_CurrentThrust");
            Fields["State"].guiName = Localizer.Format("#LOC_BeamedPower_AblativeEngine_PowerStatus");
            Fields["SkinTemp"].guiName = Localizer.Format("#LOC_BeamedPower_SkinTemp");
            Fields["SkinTemp"].group.displayName = Localizer.Format("#LOC_BeamedPower_HeatInfo");
            //editor
            Fields["Distance"].guiName = Localizer.Format("#LOC_BeamedPower_CalcDistance");
            Fields["Distance"].group.displayName = Localizer.Format("#LOC_BeamedPower_ThrustCalcName");
            Fields["SourceDishDia"].guiName = Localizer.Format("#LOC_BeamedPower_CalcSourceDishDia");
            Fields["CalcEfficiency"].guiName = Localizer.Format("#LOC_BeamedPower_CalcSourceEfficiency");
            Fields["BeamedPower"].guiName = Localizer.Format("#LOC_BeamedPower_CalcPowerBeamed");
            Fields["Angle"].guiName = Localizer.Format("#LOC_BeamedPower_AblativeEngine_CalcAngle");
            Fields["Thrust"].guiName = Localizer.Format("#LOC_BeamedPower_CalcThrust");
            Fields["CalcWavelength"].guiName = Localizer.Format("#LOC_BeamedPower_CalcWavelength");
            Events["ToggleWavelength"].guiName = Localizer.Format("#LOC_BeamedPower_CalcToggleWavelength");
        }

        private void AnimationState()
        {
            if (this.part.Modules.Contains<ModuleDeployablePart>())
            {
                if (this.part.Modules.GetModule<ModuleDeployablePart>().deployState != ModuleDeployablePart.DeployState.EXTENDED)
                {
                    ReceivedPower = 0f;
                }
            }
        }

        // adding part info to part description tab in editor
        public string GetModuleTitle()
        {
            return "PhotonSail";
        }
        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_BeamedPower_PhotonSail_ModuleName");
        }
        public override string GetInfo()
        {
            return Localizer.Format("#LOC_BeamedPower_PhotonSail_ModuleInfo",
                SurfaceArea.ToString(),
                (Reflectivity * 100).ToString());
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                receiver.Spherical(this.part, true, PowerLimiter, SurfaceArea, 1d, true, true, State, out State, out double recvPower);
                ReceivedPower = (float)Math.Round(recvPower, 1);
                AnimationState();

                double impulse = ReceivedPower * 1000f * partThrustMult / c;

                // adding heat to part's skin
                double heatModifier = HighLogic.CurrentGame.Parameters.CustomParams<BPSettings>().PercentHeat;
                this.part.AddSkinThermalFlux((float)((1 - Reflectivity) * ReceivedPower * ((heatModifier / 100) * 0.7)));
                SkinTemp = (float)Math.Round(this.part.skinTemperature, 1);

                // code related to the engine module
                double thrustMult = HighLogic.CurrentGame.Parameters.CustomParams<BPSettings>().photonthrust;
                float Thrust = (float)(impulse * Reflectivity * (engine.realIsp / 30592000) * thrustMult); // in N
                ThrustN = (float)Math.Round(engine.GetCurrentThrust() * 1000, 3);
                float percentThrust = Thrust / (engine.maxThrust * 1000);
                engine.thrustPercentage = Mathf.Clamp((float)Math.Round(percentThrust * 100, 3), 0f, 100f);

                this.vessel.GetConnectedResourceTotals(EChash, out double ECamount, out double maxAmount);
                if (ECamount / maxAmount < 0.01f)
                {
                    engine.thrustPercentage = 0f;
                }

                // replenishes photons resource
                int fuelId = engine.propellants[0].resourceDef.id;
                this.part.GetConnectedResourceTotals(fuelId, out double Fuelamount, out _);
                if (Fuelamount < 10d)
                {
                    this.part.RequestResource(fuelId, -100d); // increases quantity of the photons resource
                }

                ReceivedPower = Math.Abs(ReceivedPower);
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

        [KSPField(guiName = "Angle to source", groupName = "calculator4", guiUnits = "°", guiActive = false, guiActiveEditor = true), UI_FloatRange(minValue = 0, maxValue = 90, stepIncrement = 1, scene = UI_Scene.Editor)]
        public float Angle;

        [KSPField(guiName = "Thrust", groupName = "calculator4", guiUnits = "N", guiActive = false, guiActiveEditor = true)]
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
                float spotArea = (float)(Math.Pow((1.44 * wavelength_num * Distance * 1000000d / SourceDishDia), 2) * 3.14);
                double powerReceived = Math.Cos(Angle * Math.PI / 180) * ((spotArea > SurfaceArea) ?
                    SurfaceArea / spotArea * BeamedPower * (CalcEfficiency / 100) : BeamedPower * (CalcEfficiency / 100));

                double impulse = powerReceived * 1000f / c;
                Thrust = Mathf.Clamp((float)Math.Round((impulse * Reflectivity * 
                    HighLogic.CurrentGame.Parameters.CustomParams<BPSettings>().photonthrust), 3), 0f, engine.GetMaxThrust() * 1000f);
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                engine.Fields["propellantReqMet"].guiActive = false;
            }
        }
    }
}
