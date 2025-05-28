# KSP-Beamed-Power-Standalone-mod
This is a mod (plugin) that adds Beamed Power code and Beamed Power Propulsion parts to the stock game (KSP).
There are currently 3 parts in the mod, a photon-sail, ablative engine and thermal engine. All three parts are for
beamed power propulsion, since there are plenty of good part models for transmitters and receivers already out there.
This mod is currently licensed as Public Domain Work. This license now applies to all work by me.

-------------- Basic Info ------------------------------------------------------------------------------------------------------------------------------

There are three part modules this adds that you can use: WirelessSource, WirelessReceiver, and WirelessReflector. 
At the moment, you'll have to add these part modules to any part you want to use for beamed power. Instructions for how to do that: 

WirelessSource:
If you want to make a part a beamed power transmitter, you need to add the WirelessSource part module to the part's config file. 
You can either directly add this to a part's config file, or use a Module Manager patch. It'll look something like this: 

MODULE
{
	name = WirelessSource
	DishDiameter = 10	// in metres, pick a suitable value
	Wavelength = Long	// must be either 'Long'(short microwave) or 'Short'(short ultraviolet)
	Efficiency = 0.8	// value must be in range of 0.0 to 1.0
}

--------------------------------------------------------------------------------------------------------------------------------------------------------

WirelessReceiver:
The syntax is shown below. This is a part module for beamed power receivers, you need to add the WirelessReceiver part module 
to the part's config file. The recvType sets what type of receiver this part is. Directional means it can only receive from 
one source at a time. Spherical receivers receive from all transmitters in the save file, and you don't get to choose which 
spacecraft to receive from. Spherical receivers can be a little bit cheating, so I would suggest setting a lower efficiency 
for them than directional receivers.

MODULE
{
	name = WirelessReceiver
	recvType = Spherical	// either Spherical or Directional
	recvDiameter = 2.5	// in metres, pick an appropriate value
	recvEfficiency = 0.6	// a value between 0.0 and 1.0
}

--------------------------------------------------------------------------------------------------------------------------------------------------------

WirelessReflector
This is a part module for beamed power reflectors. They are the beamed power equivalent to relay antennas. 
They take power from a source and reflect to a receiver. They are especially useful when having to deal with planetary occlusion.
Some reflectors can amplify power received from tranmitter, so the receiver gets more power than usual.
The syntax for this part module is shown below:

MODULE
{
	name = WirelessReflector
	Reflectivity = 0.98		// value between 0.0 and 1.0 , think of this as albedo
	ReflectorDiameter = 20	// in m
	
	// optional parameters
	CanAmplify = True
	Efficiency = 0.8	// efficiency of power amplifying
	wavelength = Long	// wavelength of amplifying, this must match the wavelength of beam from transmitter
						// if it doesn't, the reflector will not amplify the power.
}

------------- Optional Part modules (WIP) ------------------------------------------------------------------------------------------------------------------------ 

These are part modules for beamed power propulsion. The plan currently is that there will be three optional part modules: 
PhotonSail, ThermalEngine and AblativeEngine. All three part modules must be applied to a part that is already an engine
(it must have a thrustVectorTransform and have ModuleEnginesFX) in order to function. Also set heatProduction on the
ModuleEnginesFX module to 0, as the mod calculates engine heat using a seperate module.

--------------------------------------------------------------------------------------------------------------------------------------------------------

PhotonSail
This is a part module for sails pushed by the photons beamed by transmitters (parts with module WirelessSource). 
You can simulate something like Breakthrough-Starshot project with this part module applied to a suitable looking part.
For the ModuleEnginesFX parameters, set maxThrust = 2.0, and vac isp = 30000000.

MODULE
{
	name = PhotonSail
	SurfaceArea = 100  // in m^2
	Reflectivity = 0.95  // value between 0.0 and 1.0, think  of this as albedo
	Wavelength = Long  // either 'Long' or 'Short', tells the code what is optimum wavelength for this photon sail
}
RESOURCE
{
	name = Photons		// just a dummy resource, you dont have to worry about this resource being depleted
	amount = 1000
	maxAmount = 1000
}

-------------------------------------------------------------------------------------------------------------------------------------------------------- 

ThermalEngine
This is a part module for engines which heat their propellant thermally, directly using energy from beamed power.
The real life analog of this engine is the Microwave-thermal rocket, or the Laser-thermal rocket, depending on the wavelength
used. The syntax for applying this part module is shown below:

MODULE
{
	name = ThermalEngine
	recvDiameter = 3.75
	recvEfficiency = 0.8	// efficiency of built-in thermal receiver
	thermalEfficiency = 0.6  // efficiency of heat-exchanger in moving heat from receiver to propellant
}

A good maxThrust to set for this engine would be 4000 - 6000 kN. A good VacIsp would be 900. If you have CommunityResourcePack
and CryoTanks, I'd highly recommend changing the engine's propellant to LqdHydrgen, but keep the fuel ratio at 1.0.

--------------------------------------------------------------------------------------------------------------------------------------------------------

AblativeEngine
This is a part module for engines which get their thrust from a material being ablated off their surface, through the heat
from the power beamed to them. In game, the best fuel to use for this engine is the stock Ablator. Syntax shown below:

MODULE
{
	name = AblativeEngine
	SurfaceArea = 14.0		// in m^2
	Efficiency = 0.4		// a value between 0.0 and 1.0
}

The maxThrust I would set for this engine would be 6000 - 8000 kN. A good VacIsp would be 3000. 
Again, keep the fuel ratio at 1.0.

