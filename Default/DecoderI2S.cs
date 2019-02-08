using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using LabNation.Interfaces;

namespace Iristick.Decoders
{
    [Export(typeof(IProcessor))]
    public class DecoderI2S : IDecoder
    {
        public DecoderDescription Description
        {
            get
            {
                return new DecoderDescription()
                {
                    Name = "I2S Decoder",
                    ShortName = "I2S",
                    Author = "LabNation",
                    VersionMajor = 1,
                    VersionMinor = 0,
                    Description = "I2S decoder",
                    InputWaveformTypes = new Dictionary<string, Type>()
                    {
                        { "BCLK", typeof(bool)},
                        { "LRCLK", typeof(bool)},
                        { "DATA", typeof(bool)},
                    },
                    InputWaveformExpectedToggleRates = new Dictionary<string, ToggleRate>()
                    {
                        { "BCLK", ToggleRate.High},
                        { "LRCLK", ToggleRate.Low},
                        { "DATA", ToggleRate.Medium}
                    },
                    Parameters = new DecoderParameter[]
                    {
                        new DecoderParameterInts("bits", new int[] { 8, 16, 24, 32 }, "", 16, "Word size"),
                        new DecoderParameterInts("delay", new int[] { 0, 1, 2 }, "", 1, "bits of delay between LRCLK edge and first bit of word"),
                        new DecoderParameterStrings("edge",  new string[] { "high", "low" }, "high", "edge of BCLK on which to sample DATA"),
                        new DecoderParameterStrings("first", new string[] { "MSB", "LSB" }, "MSB", "Which bit is first after LRCLK edge"),
                    }
                };
            }
        }

        public DecoderOutput[] Process(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod)
        {
            //name input waveforms for easier usage
            bool[] bclk = (bool[])inputWaveforms["BCLK"];
            bool[] lrclk = (bool[])inputWaveforms["LRCLK"];
            bool[] data = (bool[])inputWaveforms["DATA"];

            //initialize output structure
            List<DecoderOutput> decoderOutputList = new List<DecoderOutput>();

            bool msbFirst = (string)parameters["first"] == "MSB";
            int bitsPerWord = (int)parameters["bits"];
            int delay = (int)parameters["delay"];
            int[] bclkEdges = bclk.FindEdges((string)parameters["edge"] == "low" ? Helpers.Edge.Falling : Helpers.Edge.Rising);
            int[] lrclkEdges = lrclk.FindEdges();

            for (int i = 0; i < lrclkEdges.Length - 1; i++)
            {
                int lrIndex = lrclkEdges[i];
                int lrIndexNext = lrclkEdges[i + 1];

                //Scan each LRCLK word
                bool isLeft = lrclk[lrIndex];

                //Decode word
                Int32 word = 0;
                int[] bitEdges;

                try
                {
                    bitEdges = bclkEdges.Where(x => x >= lrIndex).Skip(delay).Take(bitsPerWord).ToArray();
                }
                catch (Exception)
                {
                    break;
                }

                for (int j = 0; j < bitEdges.Length && j < bitsPerWord; j++)
                {
                    int bitIndex = bitEdges[j];
                    if (data[bitIndex])
                        word += msbFirst ? (1 << (bitsPerWord - j - 1)) : (1 << j);
                }

                decoderOutputList.Add(new DecoderOutputValueNumeric(lrIndex, lrIndexNext, isLeft ? DecoderOutputColor.Black : DecoderOutputColor.Red, word, isLeft ? "L" : "R", bitsPerWord));
            }

            return decoderOutputList.ToArray();
        }

    }

    public static class Helpers
    {
        public enum Edge
        {
            Rising,
            Falling,
            Any
        }

        public static int[] FindEdges(this bool[] arr, Edge edgeType = Edge.Any)
        {
            //Find BCLK edges - the only places where events will occur
            bool[] arrShift = new bool[arr.Length];
            Array.Copy(arr, 0, arrShift, 1, arr.Length - 1);
            arrShift[0] = arrShift[1];

            int[] edges;
            switch (edgeType)
            {
                case Edge.Any:
                    edges = arr.Select((b, i) => b != arrShift[i] ? i : -1).Where(x => x > 0).ToArray();
                    break;
                case Edge.Rising:
                    edges = arr.Select((b, i) => (b != arrShift[i] && b) ? i : -1).Where(x => x > 0).ToArray();
                    break;
                case Edge.Falling:
                    edges = arr.Select((b, i) => (b != arrShift[i] && !b) ? i : -1).Where(x => x > 0).ToArray();
                    break;
                default:
                    edges = new int[] { };
                    break;
            }

            return edges;
        }
    }
}
