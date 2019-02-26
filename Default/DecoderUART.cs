///////////////////
//
//  UART/RS232 decoder by Robert44
//  https://github.com/robert44/decoders
//

namespace LabNation.Decoders
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    using LabNation.Interfaces;

    /// <summary>
    /// The serial.
    /// </summary>
    [Export(typeof(IProcessor))]
    public class DecoderUART : IDecoder
    {
        /// <summary>
        /// Gets the description.
        /// </summary>
        public DecoderDescription Description
        {
            get
            {
                return new DecoderDescription
                {
                    Name = "UART/RS232 decoder",
                    ShortName = "UART",
                    Author = "robert44, sismoke",
                    VersionMajor = 0,
                    VersionMinor = 1,
                    Description = "Serial decoder for UART and RS232 protocols",
                    InputWaveformTypes = new Dictionary<string, Type> { { "Input", typeof(bool) } },
                    Parameters = new DecoderParameter[]
                    {
                        new DecoderParameterStrings(
                            "Baud",
                            new[] {
                                "Auto",
                                "75", "110", "300", "1200", "2400", "4800", "9600",
                                "14400", "19200", "28800", "38400", "57600", "115200"
                            },
                            "Auto",
                            "Bits per second (baudrate)."
                            ),
                        new DecoderParameterInts("Bits", new[] { 5, 6, 7, 8, 9 }, "Databits", 8, "Data bits."),
                        new DecoderParameterStrings("Par", new[] { "None", "Odd", "Even", "Mark", "Space" }, "None", "Parity."),
                        new DecoderParameterInts("Stopbits", new[] { 1, 2 }, "Stopbits", 1, "stop bit setting."),
                        new DecoderParameterStrings("Mode", new[] { "UART", "RS232" }, "UART", "Select if the signal needs to be inverted.")
                    }
                };
            }
        }

        /// <summary>
        /// The decoding method.
        /// </summary>
        /// <param name="inputWaveforms"> The input waveforms. </param>
        /// <param name="parameters"> The parameters. </param>
        /// <param name="samplePeriod"> The sample period. </param>
        /// <returns> The output returned to the scope. </returns>
        public DecoderOutput[] Process(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod)
        {
            var decoderOutputList = new List<DecoderOutput>();

            try
            {
                //// Get samples.
                var serialData = (bool[])inputWaveforms["Input"];

                //// Fetch parameters.
                int selectedBaudrate = 0;
                var selectedBaudrateStr = (string)parameters["Baud"];
                if (selectedBaudrateStr != "Auto")
                {
                    int.TryParse(selectedBaudrateStr, out selectedBaudrate);
                }

                var selectedDatabits = (int)parameters["Bits"];
                var selectedStopbits = (int)parameters["Stopbits"];
                var selectedMode = (string)parameters["Mode"];
                bool inverted = selectedMode == "UART";

                int parityLength;
                Parity parity;
                switch ((string)parameters["Par"])
                {
                    case "Odd":
                        parity = Parity.Odd;
                        parityLength = 1;
                        break;
                    case "Even":
                        parity = Parity.Even;
                        parityLength = 1;
                        break;
                    case "Mark":
                        parity = Parity.Mark;
                        parityLength = 1;
                        break;
                    case "Space":
                        parity = Parity.Space;
                        parityLength = 1;
                        break;
                    default:
                        parity = Parity.None;
                        parityLength = 0;
                        break;
                }

                int frameLength = 1 + selectedDatabits + parityLength + selectedStopbits;

                var bits = new List<Bit>();
                int lastIndex = 0;

                for (int i = 1; i < serialData.Length; i++)
                {
                    if (serialData[i] != serialData[i - 1])
                    {
                        bits.Add(new Bit(lastIndex, i - lastIndex, serialData[i - 1] == inverted));
                        lastIndex = i;
                    }
                }
                bits.Add(new Bit(lastIndex, serialData.Length - lastIndex - 1, serialData.Last() == inverted));

                //// Get bit length from the smallest bit.
                int minimumBitlength;
                if (selectedBaudrate == 0)
                {
                    var bitlengths = from bit in bits group bit by bit.Length into g where g.Count() > 1 select new { Length = g.Key, Count = g.Count() };
                    minimumBitlength = int.MaxValue;
                    foreach (var bitLength in bitlengths)
                    {
                        if (minimumBitlength > bitLength.Length)
                        {
                            minimumBitlength = bitLength.Length;
                        }
                    }

                    if ((int.MaxValue - minimumBitlength) < 0.1)
                    {
                        // possible show error in detecting baudrate.
                        return decoderOutputList.ToArray();
                    }
                }
                else
                {
                    minimumBitlength = (int)(1 / (samplePeriod * selectedBaudrate));
                }

                var resultBits = new List<Bit>();
                int indexstep = minimumBitlength;
                var bitstring = new StringBuilder();

                foreach (Bit bit in bits)
                {
                    var count = (bit.Length + (minimumBitlength / 2))/ minimumBitlength;
                    int indexOffset = bit.Index;
                    for (var i = 0; i < count; i++)
                    {
                        resultBits.Add(new Bit(indexOffset, indexstep, bit.Value));
                        bitstring.Append(bit.Value ? "1" : "0");
                        indexOffset += indexstep;
                    }
                }

                if (bitstring.Length < frameLength)
                {
                    // possible show error no data.
                    return decoderOutputList.ToArray();
                }

                var bitstream = bitstring.ToString();

                int bestOffset = 0;
                int maxCount = 0;

                // Find the offset that gives the most frames.
                for (int offset = 1; offset < bitstream.Length / 4; offset++)
                {
                    int cntFrames = 0;
                    for (int i = offset; i < bitstream.Length; i += frameLength)
                    {
                        if (i + selectedDatabits + parityLength + selectedStopbits < bitstream.Length)
                        {
                            // Find start and stop bit.
                            if (bitstream[i - 1] == '1' && bitstream[i] == '0' && bitstream[i + selectedDatabits + parityLength + selectedStopbits] == '1')
                            {
                                cntFrames++;
                            }
                        }
                    }

                    if (cntFrames > maxCount)
                    {
                        maxCount = cntFrames;
                        bestOffset = offset;
                    }
                }

                int stepSize = frameLength;
                for (int i = bestOffset; i < bitstream.Length - frameLength; i += stepSize)
                {
                    // Find start and stop bit.
                    if (bitstream[i - 1] == '1' && bitstream[i] == '0' && bitstream[i + selectedDatabits + parityLength + selectedStopbits] == '1')
                    {
                        if ((selectedStopbits == 1) | ((selectedStopbits == 2) && bitstream[i + selectedDatabits + parityLength + 1] == '1'))
                        {
                            stepSize = frameLength;
                            var databitsStr = bitstream.Substring(i + 1, selectedDatabits);

                            bool parityOk = true;
                            char parityBit = ' ';
                            if (parity != Parity.None)
                            {
                                int oneCount;
                                parityBit = bitstream[i + selectedDatabits + 1];
                                switch (parity)
                                {
                                    case Parity.Odd:
                                        oneCount = databitsStr.Count(x => x == '1');
                                        if (parityBit == '1')
                                        {
                                            oneCount++;
                                        }

                                        if (oneCount % 2 == 0)
                                        {
                                            parityOk = false;
                                        }

                                        break;
                                    case Parity.Even:
                                        oneCount = databitsStr.Count(x => x == '1');
                                        if (parityBit == '1')
                                        {
                                            oneCount++;
                                        }

                                        if (oneCount % 2 == 1)
                                        {
                                            parityOk = false;
                                        }

                                        break;
                                    case Parity.Mark:
                                        if (parityBit != '1')
                                        {
                                            parityOk = false;
                                        }

                                        break;
                                    case Parity.Space:
                                        if (parityBit != '0')
                                        {
                                            parityOk = false;
                                        }

                                        break;
                                }
                            }

                            var databits = databitsStr.ToCharArray();
                            Array.Reverse(databits);
                            Int16 data = Convert.ToInt16(new string(databits), 2);
                            decoderOutputList.Add(
                                new DecoderOutputEvent(
                                    resultBits[i].Index,
                                    resultBits[i].Index + indexstep,
                                    DecoderOutputColor.Orange,
                                    "START"
                                    ));
                            decoderOutputList.Add(
                                new DecoderOutputValueNumeric(
                                    resultBits[i].Index + indexstep,
                                    resultBits[i].Index + (indexstep * (selectedDatabits + 1)),
                                    DecoderOutputColor.Green,
                                    data,
                                    string.Empty,
                                    selectedDatabits
                                    ));
                            int offset = 1;
                            if (parity != Parity.None)
                            {
                                string par = string.Format("P:{0}", parityBit);
                                decoderOutputList.Add(
                                    new DecoderOutputEvent(
                                        resultBits[i].Index + (indexstep * (selectedDatabits + 1)),
                                        resultBits[i].Index + (indexstep * (selectedDatabits + 2)),
                                        parityOk ? DecoderOutputColor.DarkBlue : DecoderOutputColor.Red,
                                        par
                                        ));
                                offset++;
                            }

                            decoderOutputList.Add(
                                new DecoderOutputEvent(
                                    resultBits[i].Index + (indexstep * (selectedDatabits + offset)),
                                    resultBits[i].Index + (indexstep * (selectedDatabits + 1 + offset)),
                                    DecoderOutputColor.Blue,
                                    "STOP"
                                    ));
                        }
                    }
                    else
                    {
                        stepSize = 1;
                    }
                }

                double baudrate = 1.0 / (minimumBitlength / 1000.0);
                Debug.WriteLine("Detected: {0} baud.", (int)baudrate);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return decoderOutputList.ToArray();
        }

        /// <summary>
        /// The parity.
        /// </summary>
        private enum Parity
        {
            None,
            Odd,
            Even,
            Mark,
            Space
        }
        
        /// <summary>
        /// The bit.
        /// </summary>
        private class Bit
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Bit"/> class.
            /// </summary>
            /// <param name="index"> The index. </param>
            /// <param name="length"> The length. </param>
            /// <param name="val"> The val. </param>
            public Bit(int index, int length, bool val)
            {
                this.Index = index;
                this.Value = val;
                this.Length = length;
            }

            /// <summary>
            /// Gets the index.
            /// </summary>
            public int Index { get; private set; }

            /// <summary>
            /// Gets the value.
            /// </summary>
            public bool Value { get; private set; }

            /// <summary>
            /// Gets the length.
            /// </summary>
            public int Length { get; private set; }

            /// <summary>
            /// The to string.
            /// </summary>
            /// <returns>
            /// The <see cref="string"/>.
            /// </returns>
            public override string ToString()
            {
                return string.Format("{0},{1},{2}", this.Index, this.Length, this.Value);
            }
        }
    }
}