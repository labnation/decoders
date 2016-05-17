using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabNation.Interfaces
{
    public enum DecoderOutputColor { Green, Red, DarkRed, Orange, Yellow, Black, DarkPurple, Purple, Blue, DarkBlue}

    [SerializableAttribute]
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

    [SerializableAttribute]
    public class DecoderOutputEvent : DecoderOutput
    {
        //this constructor for Event outputs
        public DecoderOutputEvent(int startLocation, int endLocation, DecoderOutputColor color, string eventName)
            : base(startLocation, endLocation, color, eventName) { }
    }

    [SerializableAttribute]
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

    [SerializableAttribute]
    public class DecoderOutputValueNumeric : DecoderOutputValue<int>
    {
        /// <summary>
        /// The size of the value in bytes, used for displaying the value
        /// </summary>
        public int ValueBitSize
        {
            get;
            private set;
        }


        //these constructors for Value outputs        
        public DecoderOutputValueNumeric(int startLocation, int endLocation, DecoderOutputColor color, int value, string label, int bits = 0)
            : base(startLocation, endLocation, color, value, label) 
        {
            this.ValueBitSize = bits;
            if (this.ValueBitSize == 0)
                this.ValueBitSize = sizeof(int) * 8;
        }
    }    
}
