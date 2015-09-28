using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using LabNation.Interfaces;

namespace LabNation.Decoders
{
    [Export(typeof(IOperator))]
    public class OperatorMath : IOperator
    {
        public DecoderDescription Description
        {
            get
            {
                return new DecoderDescription()
                {
                    Name = "Math",
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
                    Parameters = null
                };
            }
        }

        public float[] Process(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod)
        {
            //name input waveforms for easier usage
            float[] i0 = (float[])inputWaveforms["I0"];
            float[] i1 = (float[])inputWaveforms["I1"];

            //allocate output buffer
            float[] output = new float[i0.Length];

            //add together
            for (int i = 0; i < i0.Length; i++)
                output[i] = i0[i] + i1[i];

            return output;
        }
    }
}
