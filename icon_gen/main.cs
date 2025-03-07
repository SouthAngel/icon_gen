using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace icon_gen
{

    public class IconReader
    {
        public static void ReadIcon(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                BinaryReader br = new BinaryReader(fs);

                // 读取文件头
                ushort reserved = br.ReadUInt16();
                ushort type = br.ReadUInt16();
                ushort count = br.ReadUInt16();

                // 读取目录项
                for (int i = 0; i < count; i++)
                {
                    byte width = br.ReadByte();
                    byte height = br.ReadByte();
                    byte colorCount = br.ReadByte();
                    byte reserved1 = br.ReadByte();
                    ushort planes = br.ReadUInt16();
                    ushort bitCount = br.ReadUInt16();
                    uint bytesInRes = br.ReadUInt32();
                    uint imageOffset = br.ReadUInt32();

                    // 读取图像数据
                    fs.Seek(imageOffset, SeekOrigin.Begin);
                    byte[] imageData = br.ReadBytes((int)bytesInRes);

                    // 将图像数据转换为Bitmap对象
                    using (MemoryStream ms = new MemoryStream(imageData))
                    {
                        Bitmap bitmap = new Bitmap(ms);
                        bitmap.Save($"icon_{width}x{height}.png", System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
            }
        }
    }

    public class IconWriter
    {
        public static void WriteIcon(string filePath, List<Bitmap> bitmaps)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                BinaryWriter bw = new BinaryWriter(fs);

                // 写入文件头
                bw.Write((ushort)0); // 保留字
                bw.Write((ushort)1); // 类型：ICO
                bw.Write((ushort)bitmaps.Count); // 图像数量

                // 写入目录项
                uint currentOffset = 6 + (uint)(bitmaps.Count * 16);
                foreach (Bitmap bitmap in bitmaps)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        byte[] imageData = ms.ToArray();

                        // 写入目录项
                        bw.Write((byte)bitmap.Width);
                        bw.Write((byte)bitmap.Height);
                        bw.Write((byte)0); // 调色板数量
                        bw.Write((byte)0); // 保留
                        bw.Write((ushort)1); // 颜色平面数
                        bw.Write((ushort)32); // 每像素位数
                        bw.Write((uint)imageData.Length);
                        bw.Write(currentOffset);

                        currentOffset += (uint)imageData.Length;
                    }
                }

                // 写入图像数据
                foreach (Bitmap bitmap in bitmaps)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        byte[] imageData = ms.ToArray();
                        bw.Write(imageData);
                    }
                }
            }
        }
    }

    public class ImageResizer
    {
        public static Bitmap GetResizeBitmap(Image sourceImage, int targetWidth, int targetHeight)
        {
            // 创建目标图片
            Bitmap targetBitmap = new Bitmap(targetWidth, targetHeight);
            using (Graphics graphics = Graphics.FromImage(targetBitmap))
            {
                // 设置高质量插值法
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                // 设置高质量,低速度呈现平滑程度
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                // 清除整个绘图面并以透明背景色填充
                graphics.Clear(Color.Transparent);
                // 在指定位置并且按指定大小绘制原图片的指定部分
                graphics.DrawImage(sourceImage, new Rectangle(0, 0, targetWidth, targetHeight),
                                   new Rectangle(0, 0, sourceImage.Width, sourceImage.Height), GraphicsUnit.Pixel);
            }
            return targetBitmap;
        }
    }

    internal class main
    {
        static void Main(string[] args) {
            Action outusage =  ()=>{ Console.WriteLine("usage: icon_gen.exe <input-image> <16> <32> .. <output-ico>\nExample: icon_gen.exe test_in.png 16 64 128 test_out.ico"); };
            var btmaps = new List<Bitmap>();
            if (args.Length < 2) { outusage(); return; }
            string inf, outf;
            inf = args[0];
            outf = args[args.Length-1];
            var inimg = Image.FromFile(inf);
            for (int i = 1; i < args.Length - 1; ++i)
            {
                int wh = 16;
                if (!int.TryParse(args[i], out wh)) { outusage();return; };
                btmaps.Add(ImageResizer.GetResizeBitmap(inimg,wh,wh));
            }
            IconWriter.WriteIcon(outf,btmaps);
            foreach (Bitmap mf in btmaps)
            {
                mf.Dispose();
            }
        }
    }

}
