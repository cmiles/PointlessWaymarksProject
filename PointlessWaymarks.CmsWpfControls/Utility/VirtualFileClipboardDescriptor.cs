using System.IO;
using System.Text;
using System.Windows;

namespace PointlessWaymarks.CmsWpfControls.Utility;

//TODO: Refactor to WPF Common

public sealed class VirtualFileClipboardDescriptor
{
    /// <summary>
    ///     Specifies which fields are valid in a VirtualFileClipboardDescriptor Structure
    /// </summary>
    [Flags]
    public enum VirtualFileDescriptorFlags : uint
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
        Unicode = 0x80000000
    }

    public VirtualFileClipboardDescriptor(BinaryReader reader)
    {
        //Flags
        Flags = (VirtualFileDescriptorFlags)reader.ReadUInt32();
        //ClassID
        ClassId = new Guid(reader.ReadBytes(16));

        try
        {
            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            Size = new Size(width, height);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading Size: {ex.Message}");
            Size = new Size(0, 0);
        }

        try
        {
            var x = reader.ReadInt32();
            var y = reader.ReadInt32();
            Point = new Point(x, y);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading Point: {ex.Message}");
            Point = new Point(0, 0);
        }

        //FileAttributes
        FileAttributes = (FileAttributes)reader.ReadUInt32();
        try
        {
            // CreationTime
            CreationTime = new DateTime(1601, 1, 1).AddTicks(reader.ReadInt64());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading CreationTime: {ex.Message}");
            CreationTime = DateTime.MinValue;
        }

        try
        {
            // LastAccessTime
            LastAccessTime = new DateTime(1601, 1, 1).AddTicks(reader.ReadInt64());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading LastAccessTime: {ex.Message}");
            LastAccessTime = DateTime.MinValue;
        }

        try
        {
            // LastWriteTime
            LastWriteTime = new DateTime(1601, 1, 1).AddTicks(reader.ReadInt64());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading LastWriteTime: {ex.Message}");
            LastWriteTime = DateTime.MinValue;
        }

        //FileSize
        var fileSizeHigh = reader.ReadUInt32();
        var fileSizeLow = reader.ReadUInt32();
        FileSize = ((long)fileSizeHigh << 32) + fileSizeLow;
        //FileName
        var nameBytes = reader.ReadBytes(520);
        var i = 0;
        while (i < nameBytes.Length)
        {
            if (nameBytes[i] == 0 && nameBytes[i + 1] == 0)
                break;
            i++;
            i++;
        }

        FileName = Encoding.Unicode.GetString(nameBytes, 0, i);
    }

    public Guid ClassId { get; set; }
    public DateTime CreationTime { get; set; }
    public FileAttributes FileAttributes { get; set; }
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public VirtualFileDescriptorFlags Flags { get; set; }
    public DateTime LastAccessTime { get; set; }
    public DateTime LastWriteTime { get; set; }
    public Point Point { get; set; }
    public Size Size { get; set; }
}