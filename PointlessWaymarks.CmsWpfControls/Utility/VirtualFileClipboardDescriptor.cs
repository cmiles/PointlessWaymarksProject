using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using IDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace PointlessWaymarks.CmsWpfControls.Utility
{
    /// <summary>
    /// Specifies which fields are valid in a FileDescriptor Structure
    /// </summary>    
    [Flags]
    enum FileDescriptorFlags : uint
    {
        ClsId = 0x00000001,
        SizePoint = 0x00000002,
        Attributes = 0x00000004,
        CreateTime = 0x00000008,
        AccessTime = 0x00000010,
        WritesTime = 0x00000020,
        FileSize = 0x00000040,
        ProgressUI = 0x00004000,
        LinkUI = 0x00008000,
        Unicode = 0x80000000,
    }

    /// <summary>
    /// https://stackoverflow.com/questions/24985239/dropped-zip-file-causes-e-data-getdatafilecontents-to-throw-an-exception
    /// </summary>
    static class ClipboardHelper
    {
        internal static MemoryStream GetFileContents(System.Windows.IDataObject dataObject, int index)
        {
            //cast the default IDataObject to a com IDataObject
            var comDataObject = (IDataObject)dataObject;

            System.Windows.DataFormat Format = System.Windows.DataFormats.GetDataFormat("FileContents");
            if (Format == null)
                return null;

            FORMATETC formatetc = new FORMATETC();
            formatetc.cfFormat = (short)Format.Id;
            formatetc.dwAspect = DVASPECT.DVASPECT_CONTENT;
            formatetc.lindex = index;
            formatetc.tymed = TYMED.TYMED_ISTREAM | TYMED.TYMED_HGLOBAL;


            //create STGMEDIUM to output request results into
            STGMEDIUM medium = new STGMEDIUM();

            //using the com IDataObject interface get the data using the defined FORMATETC
            comDataObject.GetData(ref formatetc, out medium);

            switch (medium.tymed)
            {
                case TYMED.TYMED_ISTREAM: return GetIStream(medium);
                default: throw new NotSupportedException();
            }
        }

        private static MemoryStream GetIStream(STGMEDIUM medium)
        {
            //marshal the returned pointer to a IStream object
            IStream iStream = (IStream)Marshal.GetObjectForIUnknown(medium.unionmember);
            Marshal.Release(medium.unionmember);

            //get the STATSTG of the IStream to determine how many bytes are in it
            var iStreamStat = new System.Runtime.InteropServices.ComTypes.STATSTG();
            iStream.Stat(out iStreamStat, 0);
            int iStreamSize = (int)iStreamStat.cbSize;

            //read the data from the IStream into a managed byte array
            byte[] iStreamContent = new byte[iStreamSize];
            iStream.Read(iStreamContent, iStreamContent.Length, IntPtr.Zero);

            //wrapped the managed byte array into a memory stream
            return new MemoryStream(iStreamContent);
        }
    }

    internal static class FileDescriptorReader
    {
        internal sealed class FileDescriptor
        {
            public FileDescriptorFlags Flags { get; set; }
            public Guid ClassId { get; set; }
            public Size Size { get; set; }
            public Point Point { get; set; }
            public FileAttributes FileAttributes { get; set; }
            public DateTime CreationTime { get; set; }
            public DateTime LastAccessTime { get; set; }
            public DateTime LastWriteTime { get; set; }
            public Int64 FileSize { get; set; }
            public string FileName { get; set; }

            public FileDescriptor(BinaryReader reader)
            {
                //Flags
                Flags = (FileDescriptorFlags)reader.ReadUInt32();
                //ClassID
                ClassId = new Guid(reader.ReadBytes(16));
                //Size
                Size = new Size(reader.ReadInt32(), reader.ReadInt32());
                //Point
                Point = new Point(reader.ReadInt32(), reader.ReadInt32());
                //FileAttributes
                FileAttributes = (FileAttributes)reader.ReadUInt32();
                //CreationTime
                CreationTime = new DateTime(1601, 1, 1).AddTicks(reader.ReadInt64());
                //LastAccessTime
                LastAccessTime = new DateTime(1601, 1, 1).AddTicks(reader.ReadInt64());
                //LastWriteTime
                LastWriteTime = new DateTime(1601, 1, 1).AddTicks(reader.ReadInt64());
                //FileSize
                var fileSizeHigh = reader.ReadUInt32();
                var fileSizeLow = reader.ReadUInt32();
                FileSize = ((long)fileSizeHigh << 32) + fileSizeLow;
                //FileName
                byte[] nameBytes = reader.ReadBytes(520);
                int i = 0;
                while (i < nameBytes.Length)
                {
                    if (nameBytes[i] == 0 && nameBytes[i + 1] == 0)
                        break;
                    i++;
                    i++;
                }
                FileName = UnicodeEncoding.Unicode.GetString(nameBytes, 0, i);
            }
        }

        public static IEnumerable<FileDescriptor> Read(Stream fileDescriptorStream)
        {
            BinaryReader reader = new BinaryReader(fileDescriptorStream);
            var count = reader.ReadUInt32();
            while (count > 0)
            {
                FileDescriptor descriptor = new FileDescriptor(reader);

                yield return descriptor;

                count--;
            }
        }

        public static IEnumerable<string> ReadFileNames(Stream fileDescriptorStream)
        {
            BinaryReader reader = new BinaryReader(fileDescriptorStream);
            var count = reader.ReadUInt32();
            while (count > 0)
            {
                FileDescriptor descriptor = new FileDescriptor(reader);

                yield return descriptor.FileName;

                count--;
            }
        }
    }
}
