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
    public class DecoderFPGA : IDecoder
    {
        public DecoderDescription Description
        {
            get
            {
                return new DecoderDescription()
                {
                    Name = "SmartScope FGPA",
                    ShortName = "FPGA",
                    Author = "LabNation",
                    VersionMajor = 0,
                    VersionMinor = 1,
                    Description = "Decoder converting the I2C communication between the USB controller and FPGA on the SmartScope into Register names and values",
                    InputWaveformTypes = new Dictionary<string, Type>() 
                    {
                        { "I2C", typeof(DecoderOutput)},
                    },
                    Parameters = null
                };
            }
        }

        public DecoderOutput[] Process(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod)
        {
            DecoderOutput[] i2cStream = (DecoderOutput[])inputWaveforms["I2C"];

            List<DecoderOutput> decoderOutputList = new List<DecoderOutput>();

            int startIndex = 0;
            bool i2cSequenceStarted = false;
            bool fpgaAddressed = false;
            int targetRegister = -1;

            for (int i = 0; i < i2cStream.Length; i++)
            {
                DecoderOutput currentData = i2cStream[i];

                if ((!i2cSequenceStarted) && (currentData.Text == "S"))
                {
                    i2cSequenceStarted = true;
                    fpgaAddressed = false;
                    startIndex = currentData.StartIndex;
                    targetRegister = -1;
                    continue;
                }

                if (i2cSequenceStarted)
                {
                    if (currentData is DecoderOutputValueNumeric) //check for values only, not events
                    {
                        int rawValue = (currentData as DecoderOutputValueNumeric).Value;
                        if (!fpgaAddressed)
                        {
                            fpgaAddressed = true;
                        }
                        else if (targetRegister < 0)
                        {
                            targetRegister = (int)rawValue;
                        }
                        else //3rd byte: regsiter value
                        {
                            REG fpgaRegister = (REG)targetRegister;
                            string toPrint = "FPGA: Set register " + fpgaRegister.ToString() + " to " + rawValue.ToString() + " (0x" + rawValue.ToString("X").PadLeft(2, '0') + ")";
                            decoderOutputList.Add(new DecoderOutputEvent(startIndex, currentData.EndIndex, DecoderOutputColor.Blue, toPrint));
                            
                            //terminate this transmission
                            i2cSequenceStarted = false;
                        }
                    }
                    else
                    {
                        //if any event other than ACK is encountered during I2C transmission
                        if (currentData.Text != "ACK")
                            i2cSequenceStarted = false;
                    }
                }

            }
            
            return decoderOutputList.ToArray();
        }
    }

    enum REG
    {
        STROBE_UPDATE = 0,
        SPI_ADDRESS = 1,
        SPI_WRITE_VALUE = 2,
        DIVIDER_MULTIPLIER = 3,
        CHA_YOFFSET_VOLTAGE = 4,
        CHB_YOFFSET_VOLTAGE = 5,
        TRIGGER_PWM = 6,
        TRIGGER_LEVEL = 7,
        TRIGGER_THRESHOLD = 8,
        TRIGGER_MODE = 9,
        TRIGGER_WIDTH = 10,
        INPUT_DECIMATION = 11,
        ACQUISITION_DEPTH = 12,
        TRIGGERHOLDOFF_B0 = 13,
        TRIGGERHOLDOFF_B1 = 14,
        TRIGGERHOLDOFF_B2 = 15,
        TRIGGERHOLDOFF_B3 = 16,
        VIEW_DECIMATION = 17,
        VIEW_OFFSET_B0 = 18,
        VIEW_OFFSET_B1 = 19,
        VIEW_OFFSET_B2 = 20,
        VIEW_ACQUISITIONS = 21,
        VIEW_BURSTS = 22,
        VIEW_EXCESS_B0 = 23,
        VIEW_EXCESS_B1 = 24,
        DIGITAL_TRIGGER_RISING = 25,
        DIGITAL_TRIGGER_FALLING = 26,
        DIGITAL_TRIGGER_HIGH = 27,
        DIGITAL_TRIGGER_LOW = 28,
        DIGITAL_OUT = 29,
        AWG_DEBUG = 30,
        AWG_DECIMATION = 31,
        AWG_SAMPLES_B0 = 32,
        AWG_SAMPLES_B1 = 33,
    }
}
