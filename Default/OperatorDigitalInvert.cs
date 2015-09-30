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
    public class OperatorDigitalInvert : IOperatorDigital
    {
        public DecoderDescription Description
        {
            get
            {
                return new DecoderDescription()
                {
                    Name = "Invert",
                    ShortName = "INV",
                    Author = "LabNation",
                    VersionMajor = 0,
                    VersionMinor = 1,
                    Description = "Inverts a digital signal.",
                    InputWaveformTypes = new Dictionary<string, Type>() 
                    {
                        { "In", typeof(bool)}
                    }
                };
            }
        }

        public bool[] Process(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod)
        {
            //name input waveforms for easier usage
            bool[] i0 = (bool[])inputWaveforms["In"];

            //allocate output buffer
            bool[] output = new bool[i0.Length];

            //do operation
            for (int i = 0; i < i0.Length; i++)
                output[i] = !i0[i];

            return output;
        }
    }
}
