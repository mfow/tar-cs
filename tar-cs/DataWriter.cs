using System;
using System.IO;
using System.Threading.Tasks;

namespace tar_cs
{
    internal class DataWriter : IArchiveDataWriter
    {
        private readonly long size;
        private long remainingBytes;
        private readonly Stream stream;
        public bool CanWrite { get; private set; } = true;

        public DataWriter(Stream data, long dataSizeInBytes)
        {
            size = dataSizeInBytes;
            remainingBytes = size;
            stream = data ?? throw new ArgumentNullException(nameof(data));
        }

        public async Task<int> WriteAsync(byte[] buffer, int count)
        {
            if(remainingBytes == 0)
            {
                CanWrite = false;
                return -1;
            }
            int bytesToWrite;
            if(remainingBytes - count < 0)
            {
                bytesToWrite = (int)remainingBytes;
            }
            else
            {
                bytesToWrite = count;
            }
            await stream.WriteAsync(buffer,0,bytesToWrite);
            remainingBytes -= bytesToWrite;
            return bytesToWrite;
        }
    }
}