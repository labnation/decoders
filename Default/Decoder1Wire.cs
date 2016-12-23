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
    public class Decoder1Wire : IDecoder
    {
        public DecoderDescription Description { 
            get { 
                return new DecoderDescription()
                {
                    Name = "1-wire Decoder",
                    ShortName = "1WIR",
                    Author = "LabNation",
                    VersionMajor = 0,
                    VersionMinor = 1,
                    Description = "Dallas/Maxim 1-wire protocol",
                    InputWaveformTypes = new Dictionary<string, Type>() 
                    {
                        { "Input", typeof(bool)}
                    }
                };
            }
        }

        enum OneWireState { Idle, ResetMPulse, ResetSWait, ResetSPulse, Active, ActiveFallen, One, MasterWritingZero, MasterReadingZero }

        const double resetMPulseMin = 480e-6;
        const double resetWaitMin = 15e-6;
        const double resetWaitMax = 60e-6;
        const double resetSPulseMin = 60e-6;
        const double resetSPulseMax = 240e-6;
        const double masterWriteOneMax = 15e-6;
        const double timeslot = 60e-6;


        public DecoderOutput[] Process(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod)
        {
            //name input waveforms for easier usage
            bool[] input = (bool[])inputWaveforms["Input"];

            //initialize output structure
            List<DecoderOutput> decoderOutputList = new List<DecoderOutput>();

            //init counters and flags
            OneWireState state = OneWireState.Idle;            

            int startIndex = 0;
            //brute-force decoding of incoming bits
            for (int i = 1; i < input.Length; i++)
            {
                bool risingEdge = input[i] && !input[i - 1];
                bool fallingEdge = !input[i] && input[i - 1];

                switch (state)
                {
                    case OneWireState.Idle:
                        {
                            if (fallingEdge)
                            {
                                state = OneWireState.ResetMPulse;
                                startIndex = i;
                            }
                        }
                        break;
                    case OneWireState.ResetMPulse:
                        {
                            if (risingEdge)
                            {
                                int elapsedSamples = i - startIndex;
                                double elapsedTime = (double)(elapsedSamples) * samplePeriod;
                                if (elapsedTime > resetMPulseMin)
                                {
                                    state = OneWireState.ResetSWait;
                                    decoderOutputList.Add(new DecoderOutputEvent(startIndex, i, DecoderOutputColor.Green, "Reset"));
                                }
                                else
                                {
                                    state = OneWireState.Idle;
                                    decoderOutputList.Add(new DecoderOutputEvent(startIndex, i, DecoderOutputColor.Red, "ResetNotLongEnough"));
                                }
                                startIndex = i;
                            }
                        }
                        break;
                    case OneWireState.ResetSWait:
                        {
                            if (fallingEdge)
                            {
                                int elapsedSamples = i - startIndex;
                                double elapsedTime = (double)(elapsedSamples) * samplePeriod;
                                if (elapsedTime > resetWaitMin && elapsedTime < resetWaitMax)
                                {
                                    state = OneWireState.ResetSPulse;
                                }
                                else
                                {
                                    state = OneWireState.Idle;
                                    decoderOutputList.Add(new DecoderOutputEvent(startIndex, i, DecoderOutputColor.Red, "UntimelyResponse"));
                                }
                                startIndex = i;
                            }
                        }
                        break;
                    case OneWireState.ResetSPulse:
                        {
                            if (risingEdge)
                            {
                                int elapsedSamples = i - startIndex;
                                double elapsedTime = (double)(elapsedSamples) * samplePeriod;
                                if (elapsedTime > resetSPulseMin && elapsedTime < resetSPulseMax)
                                {
                                    state = OneWireState.Active;
                                    decoderOutputList.Add(new DecoderOutputEvent(startIndex, i, DecoderOutputColor.Purple, "R_ACK"));
                                }
                                else
                                {
                                    state = OneWireState.Idle;
                                    decoderOutputList.Add(new DecoderOutputEvent(startIndex, i, DecoderOutputColor.Red, "WrongAckWidth"));
                                }
                                startIndex = i;
                            }
                        }
                        break;
                    case OneWireState.Active:
                        {
                            if (fallingEdge)
                            {
                                state = OneWireState.ActiveFallen;
                                startIndex = i;
                            }
                        }
                        break;
                    case OneWireState.ActiveFallen:
                        {
                            if (risingEdge)
                            {
                                int elapsedSamples = i - startIndex;
                                double elapsedTime = (double)(elapsedSamples) * samplePeriod;
                                if (elapsedTime < masterWriteOneMax)
                                {
                                    state = OneWireState.One;
                                }
                                else if (elapsedTime < timeslot)
                                {
                                    state = OneWireState.MasterReadingZero;
                                }
                                else if (elapsedTime > timeslot)
                                {
                                    state = OneWireState.MasterWritingZero;
                                }
                                // startIndex = i; don't reset here, as the output needs to span both falling and rising edge period
                            }
                        }
                        break;
                    case OneWireState.One:
                        {
                            if (fallingEdge)
                            {
                                state = OneWireState.ActiveFallen;
                                decoderOutputList.Add(new DecoderOutputValueNumeric(startIndex, i, DecoderOutputColor.Purple, 1, "W/R", 1));
                                startIndex = i;
                            }
                        }
                        break;
                    case OneWireState.MasterWritingZero:
                        {
                            if (fallingEdge)
                            {
                                state = OneWireState.ActiveFallen;
                                decoderOutputList.Add(new DecoderOutputValueNumeric(startIndex, i, DecoderOutputColor.Blue, 0, "W", 1));
                                startIndex = i;
                            }
                        }
                        break;
                    case OneWireState.MasterReadingZero:
                        {
                            if (fallingEdge)
                            {
                                state = OneWireState.ActiveFallen;
                                decoderOutputList.Add(new DecoderOutputValueNumeric(startIndex, i, DecoderOutputColor.Green, 0, "R", 1));
                                startIndex = i;
                            }
                        }
                        break;
                }

                //watchdog
                if ((state == OneWireState.ActiveFallen) && ((double)(i - startIndex) * samplePeriod > 100 * timeslot))
                {
                    state = OneWireState.Idle;
                    decoderOutputList.Add(new DecoderOutputEvent(startIndex, i, DecoderOutputColor.Red, "TimeOut"));
                }
            }

            return decoderOutputList.ToArray();
        }
    }
}
