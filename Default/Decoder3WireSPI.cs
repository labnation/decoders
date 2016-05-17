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
    public class Decoder3WireSPI : IDecoder
    {
        public DecoderDescription Description { 
            get { 
                return new DecoderDescription()
                {
                    Name = "3-wire SPI Decoder",
                    ShortName = "3SPI",
                    Author = "LabNation",
                    VersionMajor = 0,
                    VersionMinor = 1,
                    Description = "3-wire SPI using a single wire for bidirectional communication. Converts nCS, clock and data signals into Adress and Value bytes.",
                    InputWaveformTypes = new Dictionary<string, Type>() 
                    {
                        { "nCS", typeof(bool)},
                        { "SCLK", typeof(bool)},
                        { "SDIO", typeof(bool)}
                    },
                    InputWaveformExpectedToggleRates = new Dictionary<string, ToggleRate>() 
                    {
                        { "nCS", ToggleRate.Low},
                        { "SCLK", ToggleRate.High},
                        { "SDIO", ToggleRate.Medium}
                    },
                    Parameters = new DecoderParameter[]
                    {
                        new DecoderParameterInts("RWPOS", new int[] { 0, 7 }, "", 0, "Position of the bit inside the Address byte which indicates whether a transaction is a Read or Write transaction"),
                    }
                };
            }
        }

        public DecoderOutput[] Process(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod)
        {
            //name input waveforms for easier usage
            bool[] nCS = (bool[])inputWaveforms["nCS"];
            bool[] SCLK = (bool[])inputWaveforms["SCLK"];
            bool[] SDIO = (bool[])inputWaveforms["SDIO"];

            //initialize output structure
            List<DecoderOutput> decoderOutputList = new List<DecoderOutput>();

            //init counters and flags
            bool spiSequenceStarted = false;
            bool startEventFired = false;
            bool addressDecoded = false;
            bool read = false;
            int bitCounter = 0;
            int startIndex = 0;
            byte decodedByte = 0;

            //brute-force decoding of incoming bits
            for (int i = 1; i < SCLK.Length; i++)
            {
                bool ncsRisingEdge = nCS[i] && !nCS[i - 1];
                bool ncsFallingEdge = !nCS[i] && nCS[i - 1];
                bool clockRisingEdge = SCLK[i] && !SCLK[i - 1];

                //Check for start sequence
                if (ncsFallingEdge)
                {   
                    spiSequenceStarted = true;
                    startEventFired = true;
                    addressDecoded = false;
                    bitCounter = 7;
                    startIndex = i;
                }
                //Check for stop sequence
                else if (ncsRisingEdge)
                {
                    spiSequenceStarted = false;
                    decoderOutputList.Add(new DecoderOutputEvent(startIndex, i, DecoderOutputColor.Orange, "P"));
                    startIndex = i;
                }                

                //Decode byte
                if (spiSequenceStarted && clockRisingEdge)
                {
                    //terminate start event
                    if (startEventFired)
                    {
                        startEventFired = false;
                        decoderOutputList.Add(new DecoderOutputEvent(startIndex, i, DecoderOutputColor.Green, "S"));
                        startIndex = i;
                    }

                    //reset each new byte
                    if (bitCounter == 7)
                    {
                        decodedByte = 0;
                        startIndex = i;
                    }

                    //for very first bit: check for R/W bit
                    if (!addressDecoded && (bitCounter == 7 - (int)parameters["RWPOS"]))
                        read = SDIO[i];
                    //for all other bits: accumulate value
                    else 
                        decodedByte = (byte)((decodedByte << 1) + (SDIO[i] ? 1 : 0));

                    //at end of byte
                    if (bitCounter == 0)
                    {
                        if (!addressDecoded)
                        {
                            decoderOutputList.Add(new DecoderOutputValueNumeric(startIndex, i, DecoderOutputColor.DarkBlue, decodedByte, "Address", 8));
                            addressDecoded = true;
                        }
                        else
                        {
                            if (read)
                                decoderOutputList.Add(new DecoderOutputValueNumeric(startIndex, i, DecoderOutputColor.Purple, decodedByte, "Read", 8));
                            else
                                decoderOutputList.Add(new DecoderOutputValueNumeric(startIndex, i, DecoderOutputColor.DarkPurple, decodedByte, "Write", 8));
                        }
                        startIndex = i;
                        bitCounter = 7;
                    }
                    else
                    {
                        bitCounter--;
                    }
                }
            }

            return decoderOutputList.ToArray();
        }
    }
}
