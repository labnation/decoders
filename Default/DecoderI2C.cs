using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using LabNation.Interfaces;

namespace LabNation.Decoders
{
    [Export(typeof(IDecoder))]
    public class DecoderI2C : IDecoder
    {
        public DecoderDescription Description
        {
            get
            {
                return new DecoderDescription()
                {
                    Name = "I2C Decoder",
                    ShortName = "I2C",
                    Author = "LabNation",
                    VersionMajor = 0,
                    VersionMinor = 1,
                    Description = "I2C decoder, converting clock and data signal into Adress and Value bytes",
                    InputWaveformTypes = new Dictionary<string, Type>() 
                    {
                        { "SCL", typeof(bool)},
                        { "SDA", typeof(bool)}
                    },
                    Parameters = null
                };
            }
        }

        public DecoderOutput[] Decode(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod)
        {
            //name input waveforms for easier usage
            bool[] SCLK = (bool[])inputWaveforms["SCL"];
            bool[] SDIO = (bool[])inputWaveforms["SDA"];

            //initialize output structure
            List<DecoderOutput> decoderOutputList = new List<DecoderOutput>();

            //start of brute-force decoding
            bool i2cSequenceStarted = false;
            bool addressDecoded = false;
            int bitCounter = 0;
            int startIndex = 0;
            byte decodedByte = 0;

            for (int i = 1; i < SCLK.Length; i++)
            {
                bool clockRisingEdge = SCLK[i] && !SCLK[i - 1];
                bool clockFallingEdge = !SCLK[i] && SCLK[i - 1];
                bool dataRisingEdge = SDIO[i] && !SDIO[i - 1];
                bool dataFallingEdge = !SDIO[i] && SDIO[i - 1];

                //Check for start/stop sequence
                if (dataRisingEdge && SCLK[i])
                {
                    i2cSequenceStarted = false;
                    decoderOutputList.Add(new DecoderOutputEvent(startIndex, i, DecoderOutputColor.Orange, "P"));
                    startIndex = i;
                }
                else if (dataFallingEdge && SCLK[i])
                {                    
                    //sadly this even has no 'length'... it's just an edge
                    //it will be correctly for at the very end of this decoder as otherwise it wouldnt be visible
                    decoderOutputList.Add(new DecoderOutputEvent(i, i, DecoderOutputColor.Green, "S"));
                    i2cSequenceStarted = true;
                    addressDecoded = false;
                    bitCounter = 8;
                    startIndex = i;
                }

                //Decode byte
                if (i2cSequenceStarted && clockRisingEdge)
                {
                    if (bitCounter == 8)
                    {
                        decodedByte = 0;
                        startIndex = i;
                    }
                    if (bitCounter >= 1) //don't take ACK in here
                        decodedByte = (byte)((decodedByte << 1) + (SDIO[i] ? 1 : 0));

                    if (bitCounter == 1)
                    {
                        if (!addressDecoded)
                        {
                            decoderOutputList.Add(new DecoderOutputValue<byte>(startIndex, i, DecoderOutputColor.DarkBlue, decodedByte, "Address"));
                            addressDecoded = true;
                        }
                        else
                        {
                            decoderOutputList.Add(new DecoderOutputValue<byte>(startIndex, i, DecoderOutputColor.DarkPurple, decodedByte, "Data"));
                        }
                        startIndex = i;
                    }

                    if (bitCounter == 0)
                    {
                        if (SDIO[i])
                            decoderOutputList.Add(new DecoderOutputEvent(startIndex, i, DecoderOutputColor.Orange, "NACK"));
                        else
                            decoderOutputList.Add(new DecoderOutputEvent(startIndex, i, DecoderOutputColor.Blue, "ACK"));
                        startIndex = i;
                        bitCounter = 8;
                    }
                    else
                        bitCounter--;
                }
            }

            //visual patch at the very end, as otherwise the Start events would be rendered only 1 sample wide
            //use the same width as a Stop event
            List<DecoderOutput> stopEvents = decoderOutputList.Where(x => x.Text == "P").ToList();
            if (stopEvents.Count > 0)
            {
                int eventSize = stopEvents[0].EndIndex - stopEvents[0].StartIndex;
                List<DecoderOutput> startEvents = decoderOutputList.Where(x => x.Text == "S").ToList();
                foreach (var item in startEvents)
                    item.StartIndex -= eventSize;
            }

            return decoderOutputList.ToArray();
        }
    }
}
