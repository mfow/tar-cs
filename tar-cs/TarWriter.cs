using System;
using System.IO;
using System.Threading.Tasks;

namespace tar_cs
{
    public class TarWriter : LegacyTarWriter
    {

        public TarWriter(Stream writeStream) : base(writeStream)
        {
        }

        protected override async Task WriteHeaderAsync(string name, DateTime lastModificationTime,
            long count, int userId, int groupId, int mode, EntryType entryType)
        {
            var tarHeader = new UsTarHeader()
            {
                FileName = name,
                LastModification = lastModificationTime,
                SizeInBytes = count,
                UserId = userId,
                UserName = Convert.ToString(userId,8),
                GroupId = groupId,
                GroupName = Convert.ToString(groupId,8),
                Mode = mode,
                EntryType = entryType
            };

            await OutStream.WriteAsync(tarHeader.GetHeaderValue(), 0, tarHeader.HeaderSize);
        }

        protected virtual async Task WriteHeaderAsync(string name, DateTime lastModificationTime, long count,
            string userName, string groupName, int mode)
        {
            var tarHeader = new UsTarHeader()
            {
                FileName = name,
                LastModification = lastModificationTime,
                SizeInBytes = count,
                UserId = userName.GetHashCode(),
                UserName = userName,
                GroupId = groupName.GetHashCode(),
                GroupName = groupName,
                Mode = mode
            };

            await OutStream.WriteAsync(tarHeader.GetHeaderValue(), 0, tarHeader.HeaderSize);
        }


        public virtual async Task WriteAsync(string name, long dataSizeInBytes, string userName, string groupName,
            int mode, DateTime lastModificationTime, WriteDataAsyncCallback callback)
        {
            var writer = new DataWriter(OutStream,dataSizeInBytes);
            await WriteHeaderAsync(name, lastModificationTime, dataSizeInBytes, userName, groupName, mode);

            while(writer.CanWrite)
            {
                await callback(writer);
            }
            await AlignTo512Async(dataSizeInBytes, false);
        }


        public async Task WriteAsync(Stream data, long dataSizeInBytes, string fileName, string userId, string groupId, int mode,
                          DateTime lastModificationTime)
        {
            await WriteHeaderAsync(fileName,lastModificationTime,dataSizeInBytes,userId, groupId, mode);
            await WriteContentAsync(dataSizeInBytes,data);
            await AlignTo512Async(dataSizeInBytes,false);
        }
    }
}