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
    public class OperatorDigitalManchester : IOperatorDigital
    {
        public DecoderDescription Description
        {
            get
            {
                return new DecoderDescription()
                {
                    Name = "Manchester",
                    ShortName = "MAN",
                    Author = "LabNation",
                    VersionMajor = 0,
                    VersionMinor = 1,
                    Description = "Decodes manchester encoded signal",
                    InputWaveformTypes = new Dictionary<string, Type>()
                    {
                        { "In", typeof(bool)}
                    },
                };
            }
        }

        public bool[] Process(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod)
        {
            //name input waveforms for easier usage
            bool[] In = (bool[])inputWaveforms["In"];

            //allocate output buffer
            bool[] output = new bool[In.Length];

            int indexLast = 0;
            List<int> intervals = new List<int>() { };
            for (int i = 1; i < In.Length; i++)
            {
                var interval = i - indexLast;
                if (In[i] != In[i - 1])
                {
                    intervals.Add(interval);
                    indexLast = i;
                }
            }

            if (intervals.Count == 0)
                return output;

            int minimalInterval = (int)(intervals.Min() * 1.5); // Alow 25% variation

            indexLast = 0;
            foreach (var k in intervals)
            {
                if (k <= minimalInterval)
                    for (int i = indexLast; i < indexLast + k; i++)
                        output[i] = true;
                indexLast += k;
            }

            return output;
        }
    }
}
