namespace Serial
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
    [Export(typeof(IDecoder))]
    public class Serial : IDecoder
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
                               Name = "Serial decoder",
                               ShortName = "UART",
                               Author = "robert44",
                               VersionMajor = 0,
                               VersionMinor = 1,
                               Description = "Serial decoder",
                               InputWaveformTypes = new Dictionary<string, Type> { { "UART", typeof(float) } },
                               Parameters = new DecoderParameter[]
                                       {
                                           new DecoderParamaterInts("Baudrate", new[] { 0, 75, 110, 300, 1200, 2400, 4800, 9600, 14400, 19200, 28800, 38400, 57600, 115200 }, "bits per second", 0, "Bits per second (baudrate)."),
                                           new DecoderParamaterInts("Databits", new[] { 7, 8 }, "Databits", 8, "Data bits."),
                                           new DecoderParamaterStrings("Parity", new[] { "Even", "Odd", "None", "Mark", "Space" }, "None", "Parity."),
                                           new DecoderParamaterInts("Stopbits", new[] { 1, 2 }, "Stopbits", 1, "stop bit setting."),
                                           new DecoderParamaterStrings("Mode", new[] { "UART", "RS232" }, "RS232", "Select if the signal needs to be inverted.")
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
        public DecoderOutput[] Decode(
            Dictionary<string, Array> inputWaveforms,
            Dictionary<string, object> parameters,
            double samplePeriod)
        {
            //// Todo fix startbit, reposition startbit. Current version can not handle a lot of data.
            var decoderOutputList = new List<DecoderOutput>();

            //// Get samples.
            var serialData = (float[])inputWaveforms["UART"];

            //// Fetch parameters.
            var selectedBaudrate = (int)parameters["Baudrate"];
            var selectedDatabits = (int)parameters["Databits"];
            var selectedParity = (string)parameters["Parity"];
            var selectedStopbits = (int)parameters["Stopbits"];
            var selectedMode = (string)parameters["Mode"];
            bool inverted = selectedMode == "UART";

            //// Sort values.
            var dic = new Dictionary<float, int>();
            foreach (var f in serialData)
            {
                if (dic.ContainsKey(f))
                {
                    dic[f]++;
                }
                else
                {
                    dic.Add(f, 1);
                }
            }

            float min = float.MaxValue;
            float max = float.MinValue;

            //// Take Min Max value.
            foreach (var i in dic.Where(i => i.Value > 50))
            {
                if (i.Key < min)
                {
                    min = i.Key;
                }

                if (i.Key > max)
                {
                    max = i.Key;
                }
            }

            if (Math.Abs(min - float.MaxValue) < 0.01 || Math.Abs(max - float.MinValue) < 0.01)
            {
                return decoderOutputList.ToArray();
            }

            //// Calc Low High threshold, this way we can handle almost all signal levels.
            var th = (max - min) / 4;
            float maxth = max - th;
            float minth = min + th;

            int indexSignalUp = -1;
            int indexSignalDown = -1;
            var bits = new List<Bit>();

            //// Get bit length.
            for (int i = 0; i < serialData.Length - 1; i++)
            {
                if (serialData[i] > maxth)
                {
                    if (indexSignalDown == -1)
                    {
                        indexSignalDown = i;
                    }

                    if (indexSignalUp != -1)
                    {
                        double bitlength = Math.Abs(indexSignalDown - indexSignalUp) * samplePeriod * 1000;
                        indexSignalUp = -1;
                        bits.Add(inverted ? new Bit(i, bitlength, 0) : new Bit(i, bitlength, 1));
                        // Debug.WriteLine("bitlength L-H = {0} , index = {1}", bitlength, i);
                    }
                }

                if (serialData[i] < minth)
                {
                    if (indexSignalUp == -1)
                    {
                        indexSignalUp = i;
                    }

                    if (indexSignalDown != -1)
                    {
                        double bitlength = Math.Abs(indexSignalDown - indexSignalUp) * samplePeriod * 1000;
                        indexSignalDown = -1;
                        bits.Add(inverted ? new Bit(i, bitlength, 1) : new Bit(i, bitlength, 0));
                        // Debug.WriteLine("bitlength H-L = {0}, index = {1}", bitlength,  i);
                    }
                }
            }

            //// Minimum bit length in msec.
            double minimumBitlength = bits.Select(bit => bit.Length).Concat(new[] { double.MaxValue }).Min();
            Debug.WriteLine("Minimum bit length = {0} msec", minimumBitlength);

            if (Math.Abs(minimumBitlength - double.MaxValue) < 0.1)
            {
                // possible show error in detecting baudrate.
                return decoderOutputList.ToArray();
            }

            var resultBits = new List<Bit>();
            int indexstep = (int)(minimumBitlength / (1000.0 * samplePeriod));
            
            var bitstring = new StringBuilder();

            for (int idx = 0; idx < bits.Count; idx++)
            {
                var count = (int)(bits[idx].Length / minimumBitlength);
                if (count < (1 + selectedDatabits + 1))
                {
                    for (var i = 0; i < count; i++)
                    {
                        resultBits.Add(new Bit(bits[idx].Index + (i * indexstep), indexstep, bits[idx].Value));
                        bitstring.Append(bits[idx].Value == 1 ? "1" : "0");
                    }
                }
            }

            resultBits.Add(new Bit(bits[bits.Count - 1].Index + indexstep, indexstep, 1));

    //Debug.WriteLine("Result bits.");
    //foreach (var resultBit in resultBits)
    //{
    //    Debug.WriteLine("Index= {0}, Length= {1}, Value= {2}", resultBit.Index, resultBit.Length, resultBit.Value);
    //}

            var bitstr = bitstring.ToString();
            var bitstream = bitstr.Substring(bitstr.IndexOf('0')) + "1";    // Add end bit
            
            // Debug.WriteLine(bitstream);

            for (int i = 0; i < bitstream.Length; i += 1 + selectedDatabits + 1)
            {
                if (i + selectedDatabits + 1 < bitstream.Length)
                {
                    if (bitstream[i] == '0' && bitstream[i + selectedDatabits + 1] == '1')
                    {
                        // Start and stop bit found.
                        var databits = bitstream.Substring(i + 1, selectedDatabits).ToCharArray();
                        Array.Reverse(databits);
                        byte data = Convert.ToByte(new string(databits), 2);
                        decoderOutputList.Add(
                            new DecoderOutputValue<byte>(
                                resultBits[i].Index,
                                resultBits[i + selectedDatabits + 1].Index,
                                DecoderOutputColor.Red,
                                data,
                                string.Empty));
                    }
                }
            }

            double baudrate = 1.0 / (minimumBitlength / 1000.0);
            Debug.WriteLine("Detected: {0} baud.", (int)baudrate);

            ////// Todo get parity.
            return decoderOutputList.ToArray();
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
            public Bit(int index, double length, int val)
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
            public int Value { get; private set; }

            /// <summary>
            /// Gets the length.
            /// </summary>
            public double Length { get; private set; }
        }
    }
}