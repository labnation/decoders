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
    public class OperatorAnalogAbs : IOperatorAnalog
    {
        public DecoderDescription Description
        {
            get
            {
                return new DecoderDescription()
                {
                    Name = "Absolute value",
                    ShortName = "ABS",
                    Author = "LabNation",
                    VersionMajor = 0,
                    VersionMinor = 1,
                    Description = "Removes the sign of each value",
                    InputWaveformTypes = new Dictionary<string, Type>() 
                    {
                        { "In", typeof(float)}
                    }
                };
            }
        }

        public float[] Process(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod)
        {
            //name input waveforms for easier usage
            float[] i0 = (float[])inputWaveforms["In"];

            //allocate output buffer
            float[] output = new float[i0.Length];

            //do operation
            for (int i = 0; i < i0.Length; i++)
                output[i] = (float)Math.Abs(i0[i]);

            return output;
        }
    }
}
