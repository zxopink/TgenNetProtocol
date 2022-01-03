using System;
using System.Collections.Generic;
using System.Text;
using TgenSerializer;

namespace TgenNetProtocol
{
    public class MTUPacket
    {
        public Bytes Data { get; private set; }
        private int fragment;
        public int Size { get; private set; }
        public bool IsComplete { get => fragment >= Size; }

        public MTUPacket(int size)
        {
            Data = new Bytes();
            Size = size;
        }

        public void Append(byte[] data)
        {
            Data.Append(data);
            fragment++;
        }
    }
}
