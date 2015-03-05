using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LabNation.Interfaces
{
    public abstract class DecoderParameter
    {
        public string ShortName { get; private set; }
        public string Description { get; private set; }
        public Array PossibleValues { get; protected set; }
        public object DefaultValue { get; protected set; }

        protected DecoderParameter(string shortName, string description, Array possibleValues, object defaultValue)
        {
            this.ShortName = shortName;
            this.Description = description;
            this.PossibleValues = possibleValues;
            this.DefaultValue = defaultValue;

            if (possibleValues.Length == 0)
                throw new Exception("Decoder contains parameter " + shortName + " with 0 possible values");
        }
    }

    public class DecoderParamaterStrings : DecoderParameter
    {
        public DecoderParamaterStrings(string shortName, string[] possibleValues, string defaultValue, string description)
            :base(shortName, description, possibleValues, defaultValue)
        {
            if (!possibleValues.Contains(defaultValue))
                throw new Exception(shortName+": DefaultValue " + defaultValue + " not member of PossibleValues");
        }
    }

    public class DecoderParamaterInts : DecoderParameter
    {
        public string Unit {get; protected set;}
        public DecoderParamaterInts(string shortName, int[] possibleValues, string unit, int defaultValue, string description)
            : base(shortName, description, possibleValues, defaultValue)
        {
            this.Unit = unit;

            if (!possibleValues.Contains(defaultValue))
                throw new Exception(shortName + ": DefaultValue " + defaultValue + " not member of PossibleValues");
        }
    }    

    public class DecoderParamaterIntRange : DecoderParamaterInts
    {
        public DecoderParamaterIntRange(string shortName, int minValue, int maxValue, string unit, int defaultValue, string description)
            :base(shortName, CreateIntList(minValue, maxValue), unit, defaultValue, description)
        {
        }

        private static int[] CreateIntList(int minValue, int maxValue)
        {
            List<int> intList = new List<int>();
            for (int i = minValue; i < maxValue+1; i++)
                intList.Add(i);
            
            return intList.ToArray();
        }
    }
}
