using System;
using System.Text;

namespace SRF.Messages
{

    public class SRFMessage
    {

        protected byte[] message;

        public SRFMessage() { this.message = new byte[512]; }

        public byte[] Message
        {
            get
            {
                return message;
            }
        }

        public Commands Command
        {
            get
            {
                return (Commands)message[0];
            }
            set
            {
                message[0] = (byte)value;
            }
        }

        public uint ID
        {
            get
            {
                return BitConverter.ToUInt32(message, 1);
            }
            set
            {
                Array.Copy(BitConverter.GetBytes(value), 0, message, 1, 4);
            }
        }

        public int CurrentFileCount
        {
            get
            {
                return BitConverter.ToInt32(message, 5);
            }
            set
            {
                Array.Copy(BitConverter.GetBytes(value), 0, message, 5, 4);
            }
        }

        public long CurrentFullSize
        {
            get
            {
                return BitConverter.ToInt64(message, 9);
            }
            set
            {
                Array.Copy(BitConverter.GetBytes(value), 0, message, 9, 8);
            }
        }

        public string FileName
        {
            get
            {
                string temp = Encoding.UTF8.GetString(message, 256, 256);
                return temp.Substring(0, temp.IndexOf((char)0x00));
            }
            set
            {
                byte[] temp = Encoding.UTF8.GetBytes(value);
                Array.Copy(temp, 0, message, 256, temp.Length);
            }
        }

    }

}