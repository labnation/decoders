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
    public class OperatorAnalogAverage : IOperatorAnalog
    {
        public DecoderDescription Description
        {
            get
            {
                return new DecoderDescription()
                {
                    Name = "Average over time",
                    ShortName = "AVG",
                    Author = "LabNation",
                    VersionMajor = 0,
                    VersionMinor = 1,
                    Description = "Averages multiple acquisitions of a single channel over time",
                    InputWaveformTypes = new Dictionary<string, Type>() 
                    {
                        { "In", typeof(float)}
                    },
                    Parameters = new DecoderParameter[]
                    {
                        new DecoderParameterInts("AVGS", new int[] { 2,4,8,16,32,64,128}, "", 4, "Number of acquisitions to average")
                    }
                };
            }
        }

        private Queue<float[]> queue = new Queue<float[]>();
        double queueSamplePeriod = 0;
        public float[] Process(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod)
        {
            //name input waveforms for easier usage
            float[] input0 = (float[])inputWaveforms["In"];
            int queueLength = (int)parameters["AVGS"];

            //allocate output buffer
            float[] output = new float[input0.Length];

            /* Queue management */
            //check whether sizes are still same
            if (queue.Count > 0)
                if (input0.Length != queue.ElementAt(0).Length)
                    queue.Clear();

            //check whether samplerate has changed
            if (samplePeriod != queueSamplePeriod)
                queue.Clear();
            queueSamplePeriod = samplePeriod;

            //add new data to queue
            queue.Enqueue(input0);

            //remove data which is to much
            int toRemove = queue.Count - queueLength;
            for (int i = 0; i < toRemove; i++)
                queue.Dequeue();

            //do operation
            for (int i = 0; i < queue.Count; i++)
            {
                float[] curArray = queue.ElementAt(i);
                for (int j = 0; j < curArray.Length; j++)
                {
                    output[j] += curArray[j];
                }
            }
            for (int i = 0; i < output.Length; i++)
                output[i] /= (float)queue.Count;

            return output;
        }
    }
}
