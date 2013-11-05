using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS
{
    public class StageValues : SpecialValue
    {
        Vessel vessel;

        public StageValues(Vessel vessel)
        {
            this.vessel = vessel;
        }

        public override object GetSuffix(string suffixName)
        {
            return GetResourceOfCurrentStage(suffixName);
        }

        private object GetResourceOfCurrentStage(String resourceName)
        {
            PartResourceDefinition resourceDefinition = PartResourceLibrary.Instance.resourceDefinitions.FirstOrDefault(rd => rd.name.Equals(resourceName, StringComparison.OrdinalIgnoreCase));
            if (resourceDefinition == null) // If it isn't here, it does not exist.
                return null;

            if (resourceDefinition.resourceFlowMode == ResourceFlowMode.ALL_VESSEL)
            {
                double? total = null;
                foreach (Part p in vessel.Parts)
                {
                    foreach (PartResource resource in p.Resources)
                    {
                        if (resource.info.id == resourceDefinition.id)
                        {
                            if (!total.HasValue)
                                total = 0;
                            total += resource.amount;
                        }
                    }
                }

                if (total.HasValue)
                    return total;
                else
                    return null;
            }
            else
            {
                List<Part> activeEngines = VesselUtils.GetListOfActivatedEngines(vessel);

                if (resourceDefinition.resourceFlowMode == ResourceFlowMode.NO_FLOW)
                {
                    double? total = null;
                    foreach (Part p in activeEngines)
                    {
                        foreach (PartResource resource in p.Resources)
                        {
                            if (resource.info.id == resourceDefinition.id)
                            {
                                if (!total.HasValue)
                                    total = 0;
                                total += resource.amount;
                            }
                        }
                    }

                    if (total.HasValue)
                        return total;
                    else
                        return null;
                }
                else if (resourceDefinition.resourceFlowMode == ResourceFlowMode.STACK_PRIORITY_SEARCH)
                {
                    /* Need to support FuelLine here? */
                    List<Part> visited = new List<Part>();
                    double? total = null;

                    foreach (Part part in activeEngines)
                    {
                        double? amount = prospectForResource(resourceDefinition, part, ref visited);
                        if (amount.HasValue)
                        {
                            if (total.HasValue)
                                total += amount;
                            else
                                total = amount.Value;
                        }
                    }

                    return total;
                }
                else
                    throw new kOSException("Don't know how to prospect for " + resourceDefinition.name);
            }
        }

        private double? prospectForResource(PartResourceDefinition resourceDefenition, Part part, ref List<Part> visited)
        {
            double? ret = null;

            if (visited.Contains(part))
                return null;

            visited.Add(part);

            foreach (PartResource resource in part.Resources)
            {
                if (resource.info.id == resourceDefenition.id)
                {
                    if (!ret.HasValue)
                        ret = resource.amount;
                    else
                        ret += resource.amount;
                }
            }

            foreach (AttachNode attachNode in part.attachNodes)
            {
                if (attachNode.attachedPart != null                                 //if there is a part attached here            
                        && attachNode.nodeType == AttachNode.NodeType.Stack             //and the attached part is stacked (rather than surface mounted)
                        && (attachNode.attachedPart.fuelCrossFeed                       //and the attached part allows fuel flow
                            )
                        && !(part.NoCrossFeedNodeKey.Length > 0                       //and this part does not forbid fuel flow
                                && attachNode.id.Contains(part.NoCrossFeedNodeKey)))     //    through this particular node
                {

                    double? amount = prospectForResource(resourceDefenition, attachNode.attachedPart, ref visited);
                    if (amount.HasValue)
                    {
                        if (!ret.HasValue)
                            ret = amount;
                        else
                            ret += amount;
                    }
                }
            }

            return ret;
        }
    }
}
