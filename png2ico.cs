using System;
using System.IO;
using System.Drawing;

class Program {
    static void Main(string[] args) {
        if(args.Length < 2) return;
        string inPath = args[0];
        string outPath = args[1];
        
        using (Bitmap bmp = new Bitmap(inPath)) {
            // Must resize it to exactly 256x256 for standard modern Windows icon
            using (Bitmap resized = new Bitmap(bmp, new Size(256, 256))) {
                using (MemoryStream msImg = new MemoryStream()) {
                    resized.Save(msImg, System.Drawing.Imaging.ImageFormat.Png);
                    byte[] pngBytes = msImg.ToArray();
                    
                    using (FileStream fs = new FileStream(outPath, FileMode.Create)) {
                        fs.WriteByte(0); fs.WriteByte(0); 
                        fs.WriteByte(1); fs.WriteByte(0); 
                        fs.WriteByte(1); fs.WriteByte(0); 
                        
                        fs.WriteByte(0); fs.WriteByte(0); 
                        fs.WriteByte(0); fs.WriteByte(0); 
                        fs.WriteByte(1); fs.WriteByte(0); 
                        fs.WriteByte(32); fs.WriteByte(0); 
                        
                        uint size = (uint)pngBytes.Length;
                        fs.WriteByte((byte)(size & 0xFF));
                        fs.WriteByte((byte)((size >> 8) & 0xFF));
                        fs.WriteByte((byte)((size >> 16) & 0xFF));
                        fs.WriteByte((byte)((size >> 24) & 0xFF));
                        
                        uint offset = 22; 
                        fs.WriteByte((byte)(offset & 0xFF));
                        fs.WriteByte((byte)((offset >> 8) & 0xFF));
                        fs.WriteByte((byte)((offset >> 16) & 0xFF));
                        fs.WriteByte((byte)((offset >> 24) & 0xFF));
                        
                        fs.Write(pngBytes, 0, pngBytes.Length);
                    }
                }
            }
        }
    }
}
