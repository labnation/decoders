using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabNation.Interfaces
{
    public enum ToggleRate { High, Medium, Low }
    public class DecoderDescription
    {
        public string Name;
        public string ShortName;
        public string Author;
        public Dictionary<string, Type> InputWaveformTypes;
        public Dictionary<string, ToggleRate> InputWaveformExpectedToggleRates;
        public DecoderParameter[] Parameters;
        public int VersionMajor;
        public int VersionMinor;
        public string Description;
        public List<string> ContextMenuOrder;
    }

    public interface IProcessor
    {
        DecoderDescription Description { get; }
    }

    public interface IDecoder : IProcessor
    {
        DecoderOutput[] Process(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod);
    }

    public interface IOperatorAnalog : IProcessor
    {
        float[] Process(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod);
    }

    public interface IOperatorDigital : IProcessor
    {
        bool[] Process(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod);
    }
}
