namespace Serial
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
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
                                           new DecoderParamaterInts("Baudrate", new[] { 75, 110, 300, 1200, 2400, 4800, 9600, 14400, 19200, 28800, 38400, 57600, 115200 }, "bits per second", 1200, "Bits per second (baudrate)."),
                                           new DecoderParamaterInts("Databits", new[] { 7, 8 }, "Databits", 8, "Data bits."),
                                           new DecoderParamaterStrings("Parity", new[] { "Even", "Odd", "None", "Mark", "Space" }, "None", "Parity."),
                                           new DecoderParamaterInts("Stopbits", new[] { 1, 2 }, "Stopbits", 1, "stop bit setting.")
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
        public DecoderOutput[] Decode(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod)
        {
            //// Todo fix startbit, reposition startbit. Current version can not handle a lot of data.
            
            //// Get samples.
            var serialData = (float[])inputWaveforms["UART"];

            //// Fetch parameters.
            var selectedBaudrate = (int)parameters["Baudrate"];
            var selectedDatabits = (int)parameters["Databits"];
            var selectedParity = (string)parameters["Parity"];
            var selectedStopbits = (int)parameters["Stopbits"];

            //// Bit length in msec.
            var bitlength = 1000.0 / selectedBaudrate;
            var clockTime = bitlength / (samplePeriod * 1000);

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

            //// Calc Low High threshold, this way we can handle almost all signal levels.
            var th = (max - min) / 4;
            float maxth = max - th;
            float minth = min + th;
            var val = Bitvalue.Unknown;

            //// Find startbit.
            int startIndex = 0;
            for (int i = 0; i < serialData.Length; i++)
            {
                if (serialData[i] > maxth)
                {
                    if (val == Bitvalue.Low)
                    {
                        //// Start detected, start decoding.
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

            int pointer = startIndex + (int)(clockTime / 2.0);
            var decoderOutputList = new List<DecoderOutput>();
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
                        if (serialData[pointer] > maxth)
                        {
                            // Debug.WriteLine("index = {0}, 0", pointer);
                            // decoderOutputList.Add(new DecoderOutputEvent(lastPointer + (int)(clockTime / 2.0), pointer + (int)(clockTime / 2.0), DecoderOutputColor.Red, "0"));
                            // lastPointer = pointer;
                        }
                        else if (serialData[pointer] < minth)
                        {
                            // Debug.WriteLine("index = {0}, 1", pointer);
                            // decoderOutputList.Add(new DecoderOutputEvent(lastPointer + (int)(clockTime / 2.0), pointer + (int)(clockTime / 2.0), DecoderOutputColor.Blue, "1"));
                            // lastPointer = pointer;
                            data += add;
                        }
                    }

                    pointer += (int)clockTime;
                    add *= 2;
                }

                //// Skip stop bit.
                pointer += (int)clockTime;

                if (data > 33 && data < 255)
                {
                    var text = string.Format("0x{0:X} ({1})", data, Convert.ToChar(data));
                    decoderOutputList.Add(
                        colorToggle
                            ? new DecoderOutputEvent(lastPointer, pointer, DecoderOutputColor.Red, text)
                            : new DecoderOutputEvent(lastPointer, pointer, DecoderOutputColor.Blue, text));
                    lastPointer = pointer;
                    colorToggle = !colorToggle;
                }
            }

            //// Todo get parity.
            return decoderOutputList.ToArray();
        }
    }
}