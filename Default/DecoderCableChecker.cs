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
    public class DecoderCableChecker : IDecoder
    {
        public DecoderDescription Description
        {
            get
            {
                return new DecoderDescription()
                {
                    Name = "Cable checker",
                    ShortName = "CAB",
                    Author = "LabNation",
                    VersionMajor = 0,
                    VersionMinor = 1,
                    Description = "Cable checker",
                    InputWaveformTypes = new Dictionary<string, Type>() 
                    {
                        { "B0", typeof(bool?)},
                        { "B1", typeof(bool?)},
                        { "B2", typeof(bool?)},
                        { "B3", typeof(bool?)},
                        { "B4", typeof(bool?)},
                        { "B5", typeof(bool?)},
                        { "B6", typeof(bool?)},
                        { "B7", typeof(bool?)}
                    }
                };
            }
        }

        public DecoderOutput[] Process(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod)
        {
            //name input waveforms for easier usage
            bool[] b0 = (bool[])inputWaveforms["B0"];
            bool[] b1 = (bool[])inputWaveforms["B1"];
            bool[] b2 = (bool[])inputWaveforms["B2"];
            bool[] b3 = (bool[])inputWaveforms["B3"];
            bool[] b4 = (bool[])inputWaveforms["B4"];
            bool[] b5 = (bool[])inputWaveforms["B5"];
            bool[] b6 = (bool[])inputWaveforms["B6"];
            bool[] b7 = (bool[])inputWaveforms["B7"];

            //first convert all bool values into ushorts
            int[] convertedValues = new int[b0.Length];
            for (int i = 0; i < convertedValues.Length; i++)
                convertedValues[i] = (b0[i] ? 1 : 0) + (b1[i] ? 2 : 0) + (b2[i] ? 4 : 0) + (b3[i] ? 8 : 0) + (b4[i] ? 16 : 0) + (b5[i] ? 32 : 0) + (b6[i] ? 64 : 0) + (b7[i] ? 128 : 0);

            //initialize output structure
            List<DecoderOutput> decoderOutputList = new List<DecoderOutput>();

            string result = "OK";
            for (int i = 2; i < convertedValues.Length-1; i++)
            {
                if (!(convertedValues[i] == convertedValues[i - 1]))
                    if (!(convertedValues[i+1] == convertedValues[i - 2] + 1))
                        if (convertedValues[i+1] != 0)
                            result = "Not incrementing!";
            }

            if (result == "OK")            
                decoderOutputList.Add(new DecoderOutputEvent(0, convertedValues.Length - 1, DecoderOutputColor.Green, result));
            else
                decoderOutputList.Add(new DecoderOutputEvent(0, convertedValues.Length - 1, DecoderOutputColor.Red, result));

            return decoderOutputList.ToArray();
        }
    }
}
