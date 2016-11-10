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
    public class DecoderEdgeIntervals : IDecoder
    {
        public DecoderDescription Description
        {
            get
            {
                return new DecoderDescription()
                {
                    Name = "Edge intervals",
                    ShortName = "INT",
                    Author = "LabNation",
                    VersionMajor = 0,
                    VersionMinor = 1,
                    Description = "Reports the time between edges",
                    InputWaveformTypes = new Dictionary<string, Type>()
                    {
                        { "Input", typeof(bool?)}                        
                    },
                    Parameters = new DecoderParameter[]
                    {
                        new DecoderParameterStrings("Edge", new[] { "Rising", "Falling", "Both"}, "Both", "Specify which type of edges to increment on."),
                    }
                };
            }
        }

        public DecoderOutput[] Process(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod)
        {
            //name input waveforms for easier usage
            bool[] input = (bool[])inputWaveforms["Input"];
            string edgeType = (string)parameters["Edge"];

            //initialize output structure
            List<DecoderOutput> decoderOutputList = new List<DecoderOutput>();

            int startIndex = 0;
            bool toggle = false;
            for (int i = 1; i < input.Length; i++)
            {
                if ((edgeType == "Both") && (input[i] != input[i - 1])
                    || ((edgeType == "Rising") && (input[i] != input[i - 1]) && input[i])
                    || ((edgeType == "Falling") && (input[i] != input[i - 1]) && !input[i]) )
                {
                    DecoderOutputColor color = toggle ? DecoderOutputColor.Green : DecoderOutputColor.Purple;
                    double timeInS = ((double)(i - startIndex)) * samplePeriod;
                    long timeInNs = (long)(timeInS * 1000000000.0);
                    decoderOutputList.Add(new DecoderOutputValue<string>(startIndex, i, color, timeInNs.ToString("N0") + " ns", ""));
                    startIndex = i;
                    toggle = !toggle;
                }
            }

            return decoderOutputList.ToArray();
        }
    }
}
