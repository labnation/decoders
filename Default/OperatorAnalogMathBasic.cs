using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using LabNation.Interfaces;

namespace LabNation.Decoders
{
    [Export(typeof(IProcessor))]
    public class OperatorAnalogMathBasic : IOperatorAnalog
    {
        public DecoderDescription Description
        {
            get
            {
                return new DecoderDescription()
                {
                    Name = "Math - Basic",
                    ShortName = "MAT",
                    Author = "LabNation",
                    VersionMajor = 0,
                    VersionMinor = 1,
                    Description = "Basic math operator",
                    InputWaveformTypes = new Dictionary<string, Type>() 
                    {
                        { "I0", typeof(float)},
                        { "I1", typeof(float)}
                    },
                    Parameters = new DecoderParameter[]
                    {
                        new DecoderParamaterStrings("Op", new string[] { "+", "-", "*", "/", "Mod" }, "+", "Operator")
                    },
                    ContextMenuOrder = new List<string>(new string[] {"I0", "Op", "I1"})
                };
            }
        }

        public float[] Process(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod)
        {
            //name input waveforms for easier usage
            float[] i0 = (float[])inputWaveforms["I0"];
            float[] i1 = (float[])inputWaveforms["I1"];

            //fetch operators
            string op = (string)parameters["Op"];

            //allocate output buffer
            float[] output = new float[i0.Length];

            //do math
            if (op == "+")
                for (int i = 0; i < i0.Length; i++)
                    output[i] = i0[i] + i1[i];
            else if (op == "-")
                for (int i = 0; i < i0.Length; i++)
                    output[i] = i0[i] - i1[i];
            else if (op == "*")
                for (int i = 0; i < i0.Length; i++)
                    output[i] = i0[i] * i1[i];
            else if (op == "/")
                for (int i = 0; i < i0.Length; i++)
                    output[i] = i0[i] / i1[i];
            else if (op == "Mod")
                for (int i = 0; i < i0.Length; i++)
                    output[i] = i0[i] % i1[i];

            return output;
        }
    }
}
