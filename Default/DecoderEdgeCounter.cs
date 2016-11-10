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
    public class DecoderEdgeCounter : IDecoder
    {
        public DecoderDescription Description
        {
            get
            {
                return new DecoderDescription()
                {
                    Name = "Edge counter",
                    ShortName = "CTR",
                    Author = "LabNation",
                    VersionMajor = 0,
                    VersionMinor = 1,
                    Description = "Starts at 0 and increments at each edge encountered",
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
            int counter = 0;
            for (int i = 1; i < input.Length; i++)
            {
                if ((edgeType == "Both") && (input[i] != input[i - 1])
                    || ((edgeType == "Rising") && (input[i] != input[i - 1]) && input[i])
                    || ((edgeType == "Falling") && (input[i] != input[i - 1]) && !input[i]) )
                {
                    DecoderOutputColor color = toggle ? DecoderOutputColor.Green : DecoderOutputColor.Purple;
                    decoderOutputList.Add(new DecoderOutputValueNumeric(startIndex, i, color, counter, "", 32));
                    startIndex = i;
                    toggle = !toggle;
                    counter++;
                }
            }

            return decoderOutputList.ToArray();
        }
    }
}
