using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using LabNation.Interfaces;

namespace LabNation.Decoders
{
    [Export(typeof(IProcessor))]
    public class DecoderSynchronousSerial : IDecoder
    {
        public DecoderDescription Description
        {
            get
            {
                return new DecoderDescription()
                {
                    Name = "Synchronous Serial Decoder",
                    ShortName = "SSI",
                    Author = "bernhardbreuss",
                    VersionMajor = 0,
                    VersionMinor = 1,
                    Description = "Synchronous serial communication using a one data and one clock wire. Converts clock and data signals into byte values.",
                    InputWaveformTypes = new Dictionary<string, Type>() 
                    {
                        { "CLOCK", typeof(bool)},
                        { "DATA", typeof(bool)},
                    },
                    InputWaveformExpectedToggleRates = new Dictionary<string, ToggleRate>() 
                    {
                        { "CLOCK", ToggleRate.High},
                        { "DATA", ToggleRate.Medium},
                    },
                    Parameters = new DecoderParameter[]
                    {
                        new DecoderParameterInts("CPHA", new int[] { 0, 1 }, "", 0, "Clock phase. Defines whether data is sampled on rising edge (0) or falling edge (1).")
                    }
                };
            }
        }

        public DecoderOutput[] Process(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod)
        {
            //name input waveforms for easier usage
            bool[] CLOCK = (bool[])inputWaveforms["CLOCK"];
            bool[] DATA = (bool[])inputWaveforms["DATA"];

            //fetch parameters
            bool CPHA = ((int)parameters["CPHA"] == 1);

            //initialize output structure
            List<DecoderOutput> decoderOutputList = new List<DecoderOutput>();

            //init counters and flags
            int bitCounter = 7;
            int startIndex = 0;
            byte decodedDataByte = 0;

            //brute-force decoding of incoming bits
            for (int i = 1; i < CLOCK.Length; i++)
            {
                bool clockRisingEdge = CLOCK[i] && !CLOCK[i - 1];
                bool clockFallingEdge = !CLOCK[i] && CLOCK[i - 1];                

                //Decode byte
                bool samplingEdge = (clockRisingEdge && !CPHA) || (clockFallingEdge && CPHA);
                if (samplingEdge)
                {
                    //reset each new byte
                    if (bitCounter == 7)
                    {
                        decodedDataByte = 0;
                        startIndex = i;
                    }

                    //accumulate byte values
                    decodedDataByte = (byte)((decodedDataByte << 1) + (DATA[i] ? 1 : 0));

                    //at end of byte
                    if (bitCounter == 0)
                    {
                        decoderOutputList.Add(new DecoderOutputValueNumeric(startIndex, i, DecoderOutputColor.Purple, decodedDataByte, "", 8));
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