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
        }
    }

    public class DecoderParameterStrings : DecoderParameter
    {
        public DecoderParameterStrings(string shortName, string[] possibleValues, string defaultValue, string description)
            :base(shortName, description, possibleValues, defaultValue)
        {
            if (!possibleValues.Contains(defaultValue))
                throw new Exception(shortName+": DefaultValue " + defaultValue + " not member of PossibleValues");
        }
    }

    public class DecoderParameterInts : DecoderParameter
    {
        public string Unit {get; protected set;}
        public DecoderParameterInts(string shortName, int[] possibleValues, string unit, int defaultValue, string description)
            : base(shortName, description, possibleValues, defaultValue)
        {
            this.Unit = unit;

            if (!possibleValues.Contains(defaultValue))
                throw new Exception(shortName + ": DefaultValue " + defaultValue + " not member of PossibleValues");
        }
    }    

    public class DecoderParameterIntRange : DecoderParameterInts
    {
        public DecoderParameterIntRange(string shortName, int minValue, int maxValue, string unit, int defaultValue, string description)
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

    public class DecoderParameterFloats : DecoderParameter
    {
        public string Unit { get; protected set; }
        public DecoderParameterFloats(string shortName, float[] possibleValues, string unit, float defaultValue, string description)
            : base(shortName, description, possibleValues, defaultValue)
        {
            this.Unit = unit;

            if (!possibleValues.Contains(defaultValue))
                throw new Exception(shortName + ": DefaultValue " + defaultValue + " not member of PossibleValues");
        }
    }

    public class DecoderParameterNumpad : DecoderParameter
    {
        public object MinValue { get; protected set; }
        public object MaxValue { get; protected set; }
        public string Unit { get; protected set; }
        public DecoderParameterNumpad(string shortName, object minValue, object maxValue, string unit, object defaultValue, string description)
            : base(shortName, description, null, defaultValue)
        {
            this.MinValue = minValue;
            this.MaxValue = maxValue;
            this.Unit = unit;
        }
    }

    public class DecoderParameterNumpadFloat : DecoderParameterNumpad
    {
        public DecoderParameterNumpadFloat(string shortName, float minValue, float maxValue, string unit, float defaultValue, string description)
            : base(shortName, minValue, maxValue, unit, defaultValue, description)
        {
        }
    }

    public class DecoderParameterNumpadInt : DecoderParameterNumpad
    {
        public DecoderParameterNumpadInt(string shortName, int minValue, int maxValue, string unit, int defaultValue, string description)
            : base(shortName, minValue, maxValue, unit, defaultValue, description)
        {
        }
    }
}
