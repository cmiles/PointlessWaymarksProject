using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;
using IDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace PointlessWaymarks.CmsWpfControls.Utility;

/// <summary>
///     https://stackoverflow.com/questions/24985239/dropped-zip-file-causes-e-data-getdatafilecontents-to-throw-an-exception
/// </summary>
public static class VirtualFileClipboardHelper
{
    public static MemoryStream? GetFileContents(System.Windows.IDataObject dataObject, int index)
    {
        //cast the default IDataObject to a com IDataObject
        var comDataObject = (IDataObject)dataObject;

        var format = DataFormats.GetDataFormat("FileContents");
        if (format == null)
            return null;

        var formatetc = new FORMATETC
        {
            cfFormat = (short)format.Id,
            dwAspect = DVASPECT.DVASPECT_CONTENT,
            lindex = index,
            tymed = TYMED.TYMED_ISTREAM | TYMED.TYMED_HGLOBAL
        };


        //create STGMEDIUM to output request results into

        //using the com IDataObject interface get the data using the defined FORMATETC
        comDataObject.GetData(ref formatetc, out var medium);

        switch (medium.tymed)
        {
            case TYMED.TYMED_ISTREAM: return GetIStream(medium);
            default: throw new NotSupportedException();
        }
    }

    private static MemoryStream GetIStream(STGMEDIUM medium)
    {
        //marshal the returned pointer to a IStream object
        var iStream = (IStream)Marshal.GetObjectForIUnknown(medium.unionmember);
        Marshal.Release(medium.unionmember);

        //get the STATSTG of the IStream to determine how many bytes are in it
        iStream.Stat(out var iStreamStat, 0);
        var iStreamSize = (int)iStreamStat.cbSize;

        //read the data from the IStream into a managed byte array
        var iStreamContent = new byte[iStreamSize];
        iStream.Read(iStreamContent, iStreamContent.Length, IntPtr.Zero);

        //wrapped the managed byte array into a memory stream
        return new MemoryStream(iStreamContent);
    }

    public static IEnumerable<VirtualFileClipboardDescriptor> ReadFileDescriptor(Stream fileDescriptorStream)
    {
        var reader = new BinaryReader(fileDescriptorStream);
        var count = reader.ReadUInt32();
        while (count > 0)
        {
            var descriptor = new VirtualFileClipboardDescriptor(reader);

            yield return descriptor;

            count--;
        }
    }

    public static IEnumerable<string> ReadFileNames(Stream fileDescriptorStream)
    {
        var reader = new BinaryReader(fileDescriptorStream);
        var count = reader.ReadUInt32();
        while (count > 0)
        {
            var descriptor = new VirtualFileClipboardDescriptor(reader);

            yield return descriptor.FileName;

            count--;
        }
    }
}