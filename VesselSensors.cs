using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    public class VesselSensors : SpecialValue
    {
        private Vessel target;

        public VesselSensors(Vessel target)
        {
            this.target = target;
        }
        public override object GetSuffix(string suffixName)
        {
            if (suffixName == "LIGHT")
            {
                Single totalSunAOA = 0;
                foreach (Part part in target.Parts)
                {
                    if (part.State == PartStates.ACTIVE)
                    {
                        foreach (ModuleDeployableSolarPanel c in part.FindModulesImplementing<ModuleDeployableSolarPanel>())
                        {
                            totalSunAOA += c.sunAOA;
                        }
                    }
                }
                return totalSunAOA;
            }
            foreach (Part part in target.Parts)
            {
                if (part.State == PartStates.ACTIVE)
                {
                    foreach (ModuleEnviroSensor sensor in part.FindModulesImplementing<ModuleEnviroSensor>())
                    {
                        if (sensor.sensorType == suffixName)
                            return new VesselSensor(sensor);
                    }
                }
            }

            return base.GetSuffix(suffixName);
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            return base.SetSuffix(suffixName, value);
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    public class VesselSensor : SpecialValue
    {
        private ModuleEnviroSensor sensor;

        public override object Value
        {
            get
            {
                return getSensorValue();
            }
        }

        public VesselSensor(ModuleEnviroSensor sensor)
        {
            this.sensor = sensor;
        }

        public override object GetSuffix(string suffixName)
        {
            if (suffixName == "ACTIVE") return sensor.sensorActive;

            return base.GetSuffix(suffixName);
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            if (suffixName == "ACTIVE")
            {
                sensor.sensorActive = (Boolean)value;
                return true;
            }

            return base.SetSuffix(suffixName, value);
        }

        private Object getSensorValue()
        {
            switch (sensor.sensorType)
            {
                case "ACC":
                    return new Vector(FlightGlobals.getGeeForceAtPosition(sensor.part.transform.position) - sensor.vessel.acceleration);
                case "PRES":
                    return (Single)FlightGlobals.getStaticPressure();
                case "TEMP":
                    return sensor.part.temperature;
                case "GRAV":
                    return new Vector(FlightGlobals.getGeeForceAtPosition(sensor.part.transform.position));
                default:
                    throw new kOSException("Sensor of type '" + sensor.sensorType + "' isn't yet supported by kOS");
            }
        }
    }
}
