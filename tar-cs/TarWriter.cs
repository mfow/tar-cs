﻿using System;
using System.IO;
using System.Threading;
using tar_cs;

namespace tar_cs
{
    public class TarWriter : IDisposable
    {
        private readonly Stream outStream;
        protected byte[] buffer = new byte[1024];
        private bool isClosed;
        public bool ReadOnZero = true;

        /// <summary>
        /// Writes tar (see GNU tar) archive to a stream
        /// </summary>
        /// <param name="writeStream">stream to write archive to</param>
        public TarWriter(Stream writeStream)
        {
            outStream = writeStream;
        }

        protected virtual Stream OutStream
        {
            get { return outStream; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Close();
        }

        #endregion

        public void Write(string fileName)
        {
            using (FileStream file = File.OpenRead(fileName))
            {
                Write(file, file.Length, fileName, 61, 61, 511, File.GetLastWriteTime(file.Name));
            }
        }

        public void Write(FileStream file)
        {
            string path = Path.GetFullPath(file.Name).Replace(Path.GetPathRoot(file.Name),string.Empty);
            path = path.Replace(Path.DirectorySeparatorChar, '/');
            Write(file, file.Length, path, 61, 61, 511, File.GetLastWriteTime(file.Name));
        }

        public void Write(Stream data, long dataSizeInBytes, string name)
        {
            Write(data, dataSizeInBytes, name, 61, 61, 511, DateTime.Now);
        }

        protected virtual ITarHeader GetTarHeader()
        {
            return new TarHeader();
        }

        public virtual void Write(Stream data, long dataSizeInBytes, string name, int userId, int groupId, int mode,
                                  DateTime lastModificationTime)
        {
            if(isClosed)
                throw new TarException("Can not write to the closed writer");
            long count = dataSizeInBytes;
            ITarHeader header = GetTarHeader();
            header.Name = name;
            header.LastModification = lastModificationTime;
            header.SizeInBytes = count;
            header.UserId = userId;
            header.GroupId = groupId;
            header.Mode = mode;
            OutStream.Write(header.GetHeaderValue(), 0, header.HeaderSize);
            while (count > 0 && count > buffer.Length)
            {
                int bytesRead = data.Read(buffer, 0, buffer.Length);
                if (bytesRead < 0)
                    throw new IOException("TarWriter unable to read from provided stream");
                if (bytesRead == 0)
                {
                    if (ReadOnZero)
                        Thread.Sleep(100);
                    else
                        break;
                }
                OutStream.Write(buffer, 0, bytesRead);
                count -= bytesRead;
            }
            if (count > 0)
            {
                int bytesRead = data.Read(buffer, 0, (int) count);
                if (bytesRead < 0)
                    throw new IOException("TarWriter unable to read from provided stream");
                if (bytesRead == 0)
                {
                    while (count > 0)
                    {
                        OutStream.WriteByte(0);
                        --count;
                    }
                }
                else
                    OutStream.Write(buffer, 0, bytesRead);
            }
            AlignTo512(dataSizeInBytes,false);
        }


        public void AlignTo512(long size,bool acceptZero)
        {
            size = size%512;
            if (size == 0 && !acceptZero) return;
            while (size < 512)
            {
                OutStream.WriteByte(0);
                size++;
            }
        }

        public virtual void Close()
        {
            if (isClosed) return;
            AlignTo512(0,true);
            AlignTo512(0,true);
            isClosed = true;
        }
    }
}