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
    public class OperatorAnalogSubtract : IOperatorAnalog
    {
        public DecoderDescription Description
        {
            get
            {
                return new DecoderDescription()
                {
                    Name = " Ch1 - Ch2 (Subtract)",
                    ShortName = "SUB",
                    Author = "LabNation",
                    VersionMajor = 0,
                    VersionMinor = 1,
                    Description = "Subtracts two waveforms from each other",
                    InputWaveformTypes = new Dictionary<string, Type>() 
                    {
                        { "In1", typeof(float)},
                        { "In2", typeof(float)}
                    },
                    Parameters = new DecoderParameter[]
                    {
                        new DecoderParameterStrings("Dum", new string[] { "-" }, "-", "Dummy"),
                    },
                    ContextMenuOrder = new List<string>(new string[] { "In1","Dum", "In2"})
                };
            }
        }

        public float[] Process(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod)
        {
            //name input waveforms for easier usage
            float[] i0 = (float[])inputWaveforms["In1"];
            float[] i1 = (float[])inputWaveforms["In2"];

            //allocate output buffer
            float[] output = new float[i0.Length];

            //do operation
            for (int i = 0; i < i0.Length; i++)
                output[i] = i0[i] - i1[i];

            return output;
        }
    }
}
