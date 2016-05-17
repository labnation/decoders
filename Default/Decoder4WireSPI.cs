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
    public class Decoder4WireSPI : IDecoder
    {
        public DecoderDescription Description
        {
            get
            {
                return new DecoderDescription()
                {
                    Name = "4-wire SPI Decoder",
                    ShortName = "4SPI",
                    Author = "LabNation",
                    VersionMajor = 0,
                    VersionMinor = 1,
                    Description = "4-wire SPI using a 2 wires for bidirectional communication. Converts nCS, clock, MOSI and MISO signals into byte values.",
                    InputWaveformTypes = new Dictionary<string, Type>() 
                    {
                        { "nCS", typeof(bool)},
                        { "SCLK", typeof(bool)},
                        { "MOSI", typeof(bool)},
                        { "MISO", typeof(bool)}
                    },
                    InputWaveformExpectedToggleRates = new Dictionary<string, ToggleRate>() 
                    {
                        { "nCS", ToggleRate.Low},
                        { "SCLK", ToggleRate.High},
                        { "MOSI", ToggleRate.Medium},
                        { "MISO", ToggleRate.Medium}
                    },
                    Parameters = new DecoderParameter[]
                    {
                        new DecoderParameterInts("CPOL", new int[] { 0, 1 }, "", 0, "Clock polarity. Defines whether clock is high (1) or low (0) when idle (when no communication is ongoing)."),
                        new DecoderParameterInts("CPHA", new int[] { 0, 1 }, "", 0, "Clock phase. Defines whether data is sampled on rising edge (0) or falling edge (1).")
                    }
                };
            }
        }

        public DecoderOutput[] Process(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod)
        {
            //name input waveforms for easier usage
            bool[] nCS = (bool[])inputWaveforms["nCS"];
            bool[] SCLK = (bool[])inputWaveforms["SCLK"];
            bool[] MOSI = (bool[])inputWaveforms["MOSI"];
            bool[] MISO = (bool[])inputWaveforms["MISO"];

            //fetch parameters
            bool CPOL = ((int)parameters["CPOL"] == 1);
            bool CPHA = ((int)parameters["CPHA"] == 1);

            //initialize output structure
            List<DecoderOutput> decoderOutputList = new List<DecoderOutput>();

            //init counters and flags
            bool spiSequenceStarted = false;
            bool startEventFired = false;
            bool commandDecoded = false;
            int bitCounter = 0;
            int startIndex = 0;
            byte decodedMosiByte = 0;
            byte decodedMisoByte = 0;

            //brute-force decoding of incoming bits
            for (int i = 1; i < SCLK.Length; i++)
            {
                bool ncsRisingEdge = nCS[i] && !nCS[i - 1];
                bool ncsFallingEdge = !nCS[i] && nCS[i - 1];
                bool clockRisingEdge = SCLK[i] && !SCLK[i - 1];
                bool clockFallingEdge = !SCLK[i] && SCLK[i - 1];                

                //Decode byte
                bool samplingEdge = (clockRisingEdge && !CPHA) || (clockFallingEdge && CPHA);
                if (spiSequenceStarted && samplingEdge)
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
                        decodedMosiByte = 0;
                        decodedMisoByte = 0;
                        startIndex = i;
                    }                    

                    //accumulate byte values
                    decodedMosiByte = (byte)((decodedMosiByte << 1) + (MOSI[i] ? 1 : 0));
                    decodedMisoByte = (byte)((decodedMisoByte << 1) + (MISO[i] ? 1 : 0));

                    //at end of byte
                    if (bitCounter == 0)
                    {
                        if (!commandDecoded)
                        {
                            decoderOutputList.Add(new DecoderOutputValueNumeric(startIndex, i, DecoderOutputColor.DarkBlue, decodedMosiByte, "Command", 8));
                            commandDecoded = true;
                        }
                        else
                        {
                            //second byte and onward: typically miso byte unless mosi bytevalue non-0x00 and non-0xFFs
                            bool isMosiByte = false;
                            if ((decodedMosiByte != 0x00) && (decodedMosiByte != 0xFF))
                                isMosiByte = true;

                            if (isMosiByte)
                                decoderOutputList.Add(new DecoderOutputValueNumeric(startIndex, i, DecoderOutputColor.Purple, decodedMosiByte, "MOut", 8));
                            else
                                decoderOutputList.Add(new DecoderOutputValueNumeric(startIndex, i, DecoderOutputColor.Blue, decodedMisoByte, "SOut", 8));
                        }
                        startIndex = i;
                        bitCounter = 7;
                    }
                    else
                    {
                        bitCounter--;
                    }                    
                }

                //Check for start sequence. needs to be done at the end, so any edges appearing on other signals are not taken into account                
                if (ncsFallingEdge && (SCLK[i] == CPOL))
                {
                    spiSequenceStarted = true;
                    startEventFired = true;
                    commandDecoded = false;
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
            }

            return decoderOutputList.ToArray();
        }
    }
}
