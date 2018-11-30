using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;

namespace URLServerManagerModern.Data.DataTypes
{
    public class Program
    {
        private string filePath;
        public string FilePath
        {
            get { return filePath; }
            set
            {
                icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(Icon.ExtractAssociatedIcon(value).ToBitmap().GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                filePath = value;
            }
        }
        public BitmapSource icon { get; set; }
        public List<ProtocolArgumentAssociation> associations { get; set; }

        public Program(string filePath)
        {
            FilePath = filePath;
            associations = new List<ProtocolArgumentAssociation>();
        }
    }
}
