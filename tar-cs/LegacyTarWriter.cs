using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using tar_cs;

namespace tar_cs
{
    public class LegacyTarWriter : IDisposable
    {
        private readonly Stream outStream;
        protected byte[] buffer = new byte[1024];
        private bool isClosed;
        public bool ReadOnZero = true;

        /// <summary>
        /// Writes tar (see GNU tar) archive to a stream
        /// </summary>
        /// <param name="writeStream">stream to write archive to</param>
        public LegacyTarWriter(Stream writeStream)
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


        public async Task WriteDirectoryEntryAsync(string path, int userId, int groupId, int mode)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");
            if (path[path.Length - 1] != '/')
            {
                path += '/';
            }
            DateTime lastWriteTime;
            if (Directory.Exists(path))
            {
                lastWriteTime = Directory.GetLastWriteTime(path);
            }
            else
            {
                lastWriteTime = DateTime.Now;
            }
            await WriteHeaderAsync(path, lastWriteTime, 0, userId, groupId, mode, EntryType.Directory);
        }

        public async Task WriteDirectoryEntryAsync(string path)
        {
            await WriteDirectoryEntryAsync(path, 101, 101, 0777);
        }

        public async Task WriteDirectoryAsync(string directory, bool doRecursive)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentNullException("directory");

            await WriteDirectoryEntryAsync(directory);

            string[] files = Directory.GetFiles(directory);
            foreach (var fileName in files)
            {
                await WriteAsync(fileName);
            }

            string[] directories = Directory.GetDirectories(directory);
            foreach (var dirName in directories)
            {
                await WriteDirectoryEntryAsync(dirName);
                if (doRecursive)
                {
                    await WriteDirectoryAsync(dirName, true);
                }
            }
        }


        public async Task WriteAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");
            using (FileStream file = File.OpenRead(fileName))
            {
                await WriteAsync(file, file.Length, fileName, 61, 61, 511, File.GetLastWriteTime(file.Name));
            }
        }

        public async Task WriteAsync(FileStream file)
        {
            string path = Path.GetFullPath(file.Name).Replace(Path.GetPathRoot(file.Name), string.Empty);
            path = path.Replace(Path.DirectorySeparatorChar, '/');
            await WriteAsync(file, file.Length, path, 61, 61, 511, File.GetLastWriteTime(file.Name));
        }

        public Task WriteAsync(Stream data, long dataSizeInBytes, string name)
        {
            return WriteAsync(data, dataSizeInBytes, name, 61, 61, 511, DateTime.Now);
        }

        public virtual async Task WriteAsync(string name, long dataSizeInBytes, int userId, int groupId, int mode, DateTime lastModificationTime, WriteDataAsyncCallback callback)
        {
            IArchiveDataWriter writer = new DataWriter(OutStream, dataSizeInBytes);
            await WriteHeaderAsync(name, lastModificationTime, dataSizeInBytes, userId, groupId, mode, EntryType.File);
            while (writer.CanWrite)
            {
                await callback(writer);
            }
            await AlignTo512Async(dataSizeInBytes, false);
        }

        public virtual async Task WriteAsync(Stream data, long dataSizeInBytes, string name, int userId, int groupId, int mode,
                                  DateTime lastModificationTime)
        {
            if (isClosed)
                throw new TarException("Can not write to the closed writer");
            await WriteHeaderAsync(name, lastModificationTime, dataSizeInBytes, userId, groupId, mode, EntryType.File);
            await WriteContentAsync(dataSizeInBytes, data);
            await AlignTo512Async(dataSizeInBytes, false);
        }

        protected async Task WriteContentAsync(long count, Stream data)
        {
            while (count > 0 && count > buffer.Length)
            {
                int bytesRead = data.Read(buffer, 0, buffer.Length);
                if (bytesRead < 0)
                    throw new IOException("LegacyTarWriter unable to read from provided stream");
                if (bytesRead == 0)
                {
                    if (ReadOnZero)
                        Thread.Sleep(100);
                    else
                        break;
                }
                await OutStream.WriteAsync(buffer, 0, bytesRead);
                count -= bytesRead;
            }
            if (count > 0)
            {
                int bytesRead = await data.ReadAsync(buffer, 0, (int)count);
                if (bytesRead < 0)
                    throw new IOException("LegacyTarWriter unable to read from provided stream");
                if (bytesRead == 0)
                {
                    while (count > 0)
                    {
                        OutStream.WriteByte(0);
                        --count;
                    }
                }
                else
                {
                    await OutStream.WriteAsync(buffer, 0, bytesRead);
                }
            }
        }

        protected virtual async Task WriteHeaderAsync(string name, DateTime lastModificationTime,
            long count, int userId, int groupId, int mode, EntryType entryType)
        {
            var header = new TarHeader
            {
                FileName = name,
                LastModification = lastModificationTime,
                SizeInBytes = count,
                UserId = userId,
                GroupId = groupId,
                Mode = mode,
                EntryType = entryType
            };
            await OutStream.WriteAsync(header.GetHeaderValue(), 0, header.HeaderSize);
        }

        public async Task AlignTo512Async(long size, bool acceptZero)
        {
            size = size % 512;
            if (size == 0 && !acceptZero) return;
            while (size < 512)
            {
                OutStream.WriteByte(0);
                size++;
            }
        }

        public virtual void Close()
        {
            CloseAsync().Wait();
        }

        private async Task CloseAsync()
        {
            if (isClosed) return;
            await AlignTo512Async(0, true);
            await AlignTo512Async(0, true);
            isClosed = true;
        }
    }
}