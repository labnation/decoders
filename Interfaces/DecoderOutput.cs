using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabNation.Interfaces
{
    public enum DecoderOutputColor { Green, Red, DarkRed, Orange, Yellow, Black, DarkPurple, Purple, Blue, DarkBlue}

    public abstract class DecoderOutput
    {
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public string Text { get; private set; }
        public DecoderOutputColor Color { get; private set; }

        public DecoderOutput(int startIndex, int endIndex, DecoderOutputColor color, string text)
        {
            this.StartIndex = startIndex;
            this.EndIndex = endIndex;
            this.Text = text;
            this.Color = color;
        }
    }

    public class DecoderOutputEvent : DecoderOutput
    {
        //this constructor for Event outputs
        public DecoderOutputEvent(int startLocation, int endLocation, DecoderOutputColor color, string eventName)
            : base(startLocation, endLocation, color, eventName) { }        
    }

    public class DecoderOutputValue<T> : DecoderOutput
    {
        public T Value { get; private set; }

        //these constructors for Value outputs        
        public DecoderOutputValue(int startLocation, int endLocation, DecoderOutputColor color, T value, string label)
            : base(startLocation, endLocation, color, label) 
        {
            this.Value = value;
        }
    }
}
