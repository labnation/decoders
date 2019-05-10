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
    public class DecoderBus : IDecoder
    {
        public DecoderDescription Description
        {
            get
            {
                return new DecoderDescription()
                {
                    Name = "Digital Bus",
                    ShortName = "BUS",
                    Author = "LabNation",
                    VersionMajor = 0,
                    VersionMinor = 1,
                    Description = "Bus decoder, converting 8 bits into a readable value",
                    InputWaveformTypes = new Dictionary<string, Type>() 
                    {
                        { "B7", typeof(bool?)},
                        { "B6", typeof(bool?)},
                        { "B5", typeof(bool?)},
                        { "B4", typeof(bool?)},
                        { "B3", typeof(bool?)},
                        { "B2", typeof(bool?)},
                        { "B1", typeof(bool?)},
                        { "B0", typeof(bool?)}
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

            int startIndex = 0;
            bool toggle = false;
            for (int i = 1; i < convertedValues.Length-1; i++)
            {
                if (convertedValues[i] != convertedValues[i - 1])
                {
                    DecoderOutputColor color = toggle ? DecoderOutputColor.Green : DecoderOutputColor.Purple;
                    decoderOutputList.Add(new DecoderOutputValueNumeric(startIndex, i, color, convertedValues[i - 1], "", 8));
                    startIndex = i;
                    toggle = !toggle;
                }
            }

            //last element separately, in case there was not a single transition on the bus
            if (convertedValues.Length > 0)
            {
                DecoderOutputColor finalColor = toggle ? DecoderOutputColor.Green : DecoderOutputColor.Purple;
                decoderOutputList.Add(new DecoderOutputValueNumeric(startIndex, convertedValues.Length - 1, finalColor, convertedValues[convertedValues.Length - 1], "", 8));
            }

            return decoderOutputList.ToArray();
        }
    }
}
