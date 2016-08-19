﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NewEPL {
    class NinePatch {

        private ImageSource Source;

        public ImageSource Original {
            get {
                var src = (BitmapSource)Source;
                var cropped = new CroppedBitmap(src, new Int32Rect(1, 1, (int)src.Width - 2, (int)src.Height - 2));

                MemoryStream stream = new MemoryStream();
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(cropped));
                encoder.Save(stream);

                BitmapImage ret = new BitmapImage();
                ret.BeginInit();
                ret.StreamSource = stream;
                ret.EndInit();

                return ret;
            }
        }

        private Dictionary<string, ImageSource> Cache = new Dictionary<string, ImageSource>();

        private List<int> TopPatches, LeftPatches, BottomPatches, RightPatches;

        private const int BYTES_PER_PIXEL = 4;

        public NinePatch(ImageSource src) {
            Source = src;
            FindPatchRegion();
        }

        public void ClearCache() {
            Cache.Clear();
        }

        public ImageSource GetPatchedImage(int width, int height) {
            if (Cache.ContainsKey(String.Format("{0}x{1}", width, height))) {
                return Cache[String.Format("{0}x{1}", width, height)];
            }

            var src = (BitmapSource)Original;

            int srcWidth = src.PixelWidth;
            int srcHeight = src.PixelHeight;
            int targetWidth = width;
            int targetHeight = height;

            targetWidth = Math.Max(srcWidth, targetWidth);
            targetHeight = Math.Max(srcHeight, targetHeight);
            if(srcWidth == targetWidth && srcHeight == targetHeight) {
                return src;
            }

            WriteableBitmap ret = new WriteableBitmap(targetWidth, targetHeight, 96, 96, PixelFormats.Bgra32, null);
            byte[] srcPixels = new byte[targetHeight * targetWidth * BYTES_PER_PIXEL];
            byte[] pixelsId = new byte[targetHeight * targetWidth * BYTES_PER_PIXEL];

            var xMapping = XMapping(targetWidth - srcWidth, targetWidth);
            var yMapping = YMapping(targetHeight - srcHeight, targetHeight);

            int index = 0;
            src.CopyPixels(srcPixels, targetWidth * BYTES_PER_PIXEL, 0);
            for (int row = 0; row < targetHeight; row++) {
                int sourceY = yMapping[row];
                for (int col = 0; col < targetWidth; col++) {
                    int sourceX = xMapping[col];
                    for (int i = 0; i < 4; i++)
                        pixelsId[index++] = srcPixels[sourceY * targetWidth * 4 + sourceX * 4 + i];
                }
            }   

            int stride = BYTES_PER_PIXEL * targetWidth;
            ret.WritePixels(new Int32Rect(0, 0, targetWidth, targetHeight), pixelsId, stride, 0);

            Cache.Add(String.Format("{0}x{1}", targetWidth, targetHeight), ret);

            return ret;
        }

        private void FindPatchRegion() {
            TopPatches = new List<int>();
            LeftPatches = new List<int>();
            BottomPatches = new List<int>();
            RightPatches = new List<int>();

            BitmapSource src = Source as BitmapSource;
            byte[] srcPixels = new byte[src.PixelWidth *src.PixelHeight * BYTES_PER_PIXEL];
            src.CopyPixels(srcPixels, (src.PixelWidth * src.Format.BitsPerPixel + 7) / 8, 0);

            // Top
            for (int x = 1; x < src.PixelWidth - 1; x++) {
                if(srcPixels[0 * src.PixelWidth * BYTES_PER_PIXEL + x * BYTES_PER_PIXEL + 0] == 0 &&
                    srcPixels[0 * src.PixelWidth * BYTES_PER_PIXEL + x * BYTES_PER_PIXEL + 1] == 0 &&
                    srcPixels[0 * src.PixelWidth * BYTES_PER_PIXEL + x * BYTES_PER_PIXEL + 2] == 0 &&
                    srcPixels[0 * src.PixelWidth * BYTES_PER_PIXEL + x * BYTES_PER_PIXEL + 3] == 255) { 
                    TopPatches.Add(x - 1);
                }
            }
            // Left
            for (int y = 1; y < src.PixelHeight - 1; y++) {
                if (srcPixels[y * src.PixelWidth * BYTES_PER_PIXEL + 0 * BYTES_PER_PIXEL + 0] == 0 &&
                    srcPixels[y * src.PixelWidth * BYTES_PER_PIXEL + 0 * BYTES_PER_PIXEL + 1] == 0 &&
                    srcPixels[y * src.PixelWidth * BYTES_PER_PIXEL + 0 * BYTES_PER_PIXEL + 2] == 0 &&
                    srcPixels[y * src.PixelWidth * BYTES_PER_PIXEL + 0 * BYTES_PER_PIXEL + 3] == 255) {
                    LeftPatches.Add(y - 1);
                }
            }
            // Bottom
            for (int x = 1; x < src.PixelWidth - 1; x++) {
                if (srcPixels[(src.PixelHeight - 1) * src.PixelWidth * BYTES_PER_PIXEL + x * BYTES_PER_PIXEL + 0] == 0 &&
                    srcPixels[(src.PixelHeight - 1) * src.PixelWidth * BYTES_PER_PIXEL + x * BYTES_PER_PIXEL + 1] == 0 &&
                    srcPixels[(src.PixelHeight - 1) * src.PixelWidth * BYTES_PER_PIXEL + x * BYTES_PER_PIXEL + 2] == 0 &&
                    srcPixels[(src.PixelHeight - 1) * src.PixelWidth * BYTES_PER_PIXEL + x * BYTES_PER_PIXEL + 3] == 255) {
                    BottomPatches.Add(x - 1);
                }
            }
            // Right
            for (int y = 1; y < src.PixelHeight - 1; y++) {
                if (srcPixels[y * src.PixelWidth * BYTES_PER_PIXEL + (src.PixelWidth - 1) * BYTES_PER_PIXEL + 0] == 0 &&
                    srcPixels[y * src.PixelWidth * BYTES_PER_PIXEL + (src.PixelWidth - 1) * BYTES_PER_PIXEL + 1] == 0 &&
                    srcPixels[y * src.PixelWidth * BYTES_PER_PIXEL + (src.PixelWidth - 1) * BYTES_PER_PIXEL + 2] == 0 &&
                    srcPixels[y * src.PixelWidth * BYTES_PER_PIXEL + (src.PixelWidth - 1) * BYTES_PER_PIXEL + 3] == 255) {
                    RightPatches.Add(y - 1);
                }
            }
        }

        private List<int> XMapping(int diff, int target) {
            var ret = new List<int>(target);

            int src = 0;
            int dst = 0;
            while (dst < target) {
                int foundIndex = TopPatches.IndexOf(src);
                if (foundIndex != -1) {
                    int repeatCount = (diff / TopPatches.Count) + 1;
                    if (foundIndex < diff % TopPatches.Count) {
                        repeatCount++;
                    }
                    for (int j = 0; j < repeatCount; j++) {
                        ret.Insert(dst++, src);
                    }
                } else {
                    ret.Insert(dst++, src);
                }
                src++;
            }

            return ret;
        }

        private List<int> YMapping(int diff, int target) {
            var ret = new List<int>(target);

            int src = 0;
            int dst = 0;
            while (dst < target) {
                int foundIndex = LeftPatches.IndexOf(src);
                if (foundIndex != -1) {
                    int repeatCount = (diff / LeftPatches.Count) + 1;
                    if (foundIndex < diff % LeftPatches.Count) {
                        repeatCount++;
                    }
                    for (int j = 0; j < repeatCount; j++) {
                        ret.Insert(dst++, src);
                    }
                } else {
                    ret.Insert(dst++, src);
                }
                src++;
            }

            return ret;
        }
    }
}