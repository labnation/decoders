namespace Serial
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Linq;

    using LabNation.Interfaces;

    /// <summary>
    /// The serial.
    /// </summary>
    [Export(typeof(IDecoder))]
    public class Serial : IDecoder
    {
        /// <summary>
        /// The bit value.
        /// </summary>
        private enum Bitvalue
        {
            /// <summary>
            /// The unknown.
            /// </summary>
            Unknown,

            /// <summary>
            /// The high value.
            /// </summary>
            High,

            /// <summary>
            /// The low value.
            /// </summary>
            Low
        }

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
                               Author = "R.Dubber",
                               VersionMajor = 0,
                               VersionMinor = 1,
                               Description = "Serial decoder",
                               InputWaveformTypes = new Dictionary<string, Type> { { "UART", typeof(float) } },
                               Parameters = new DecoderParameter[]
                                       {
                                           new DecoderParamaterInts("Baudrate", new[] { 75, 110, 300, 1200, 2400, 4800, 9600, 14400, 19200, 28800, 38400, 57600, 115200 }, "bits per second", 9600, "Bits per second (baudrate)."),
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

            var val = Bitvalue.Unknown;

            //// Bit length in msec.
            double bitlength;

            if (selectedBaudrate != 0)
            {
                bitlength = 1000.0 / selectedBaudrate;
            }
            else
            {
                int indexSignalUp = -1;
                int indexSignalDown = -1;
                int minimumDelta = int.MaxValue;

                //// Get bit length.
                for (int i = 0; i < serialData.Length - 1; i++)
                {
                    if (serialData[i] > maxth && serialData[i + 1] < minth)
                    {
                        indexSignalDown = i;
                    }

                    if (serialData[i] < minth && serialData[i + 1] > maxth)
                    {
                        indexSignalUp = i;
                    }

                    if (indexSignalDown != indexSignalUp)
                    {
                        var d = Math.Abs(indexSignalDown - indexSignalUp);
                        if (d < minimumDelta)
                        {
                            minimumDelta = d;
                        }
                    }
                }

                bitlength = minimumDelta * samplePeriod * 1000;
                Debug.WriteLine("Minimum delta = {0} msec", bitlength);
            }

            var clockTime = bitlength / (samplePeriod * 1000);
            if (clockTime < 1)
            {
                clockTime = 1;
            }

            //// Find startbit.
            int startIndex = 0;
            for (int i = 0; i < serialData.Length; i++)
            {
                if (inverted)
                {
                    if (serialData[i] < minth)
                    {
                        if (val == Bitvalue.High)
                        {
                            //// Start detected, start decoding. (High -> Low)
                            startIndex = i;
                            break;
                        }

                        val = Bitvalue.Low;
                    }
                    else if (serialData[i] > maxth)
                    {
                        val = Bitvalue.High;
                    }
                }
                else
                {
                    if (serialData[i] > maxth)
                    {
                        if (val == Bitvalue.Low)
                        {
                            //// Start detected, start decoding. (Low -> High)
                            startIndex = i;
                            break;
                        }

                        val = Bitvalue.High;
                    }
                    else if (serialData[i] < minth)
                    {
                        val = Bitvalue.Low;
                    }
                }
            }

            int pointer = startIndex + (int)(clockTime / 2.0);
            int lastPointer = pointer;
            bool colorToggle = false;

            //// Get data bits.
            while ((pointer + (int)clockTime) < serialData.Length)
            {
                // Start bit
                pointer += (int)clockTime;

                var data = 0;
                var add = 1;
                for (int i = 0; i < selectedDatabits; i++)
                {
                    if (pointer < serialData.Length)
                    {
                        if (inverted)
                        {
                            if (serialData[pointer] > maxth)
                            {
                                data += add;
                            }
                        }
                        else
                        {
                            if (serialData[pointer] < minth)
                            {
                                data += add;
                            }
                        }
                    }

                    pointer += (int)clockTime;
                    add *= 2;
                }

                //// Skip stop bit.
                pointer += (int)clockTime;

                if (pointer < serialData.Length)
                {
                    decoderOutputList.Add(
                        colorToggle
                            ? new DecoderOutputValue<byte>(lastPointer, pointer, DecoderOutputColor.Red, (byte)data, string.Empty)
                            : new DecoderOutputValue<byte>(lastPointer, pointer, DecoderOutputColor.Blue, (byte)data, string.Empty));
                    lastPointer = pointer;
                    colorToggle = !colorToggle;
                }
            }

            //// Todo get parity.
            return decoderOutputList.ToArray();
        }
    }
}