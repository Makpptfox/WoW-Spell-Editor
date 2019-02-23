using SereniaBLPLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace SpellEditor.Sources.BLP
{
    public class BlpManager
    {
        private static BlpManager _Instance = new BlpManager();
        private static Dictionary<string, BitmapSource> _ImageSourceMap = new Dictionary<string, BitmapSource>();

        private BlpManager()
        {
            Console.WriteLine("Instantiated [BlpManager]");
        }
        
        public void LoadIcons(List<string> iconPaths)
        {
            Console.WriteLine($"[BlpManager] Loading {iconPaths.Count} icons...");
            var watch = new Stopwatch();
            watch.Start();
            foreach (var iconPath in iconPaths)
            {
                try
                {
                    using (var fileStream = new FileStream(iconPath, FileMode.Open))
                    {
                        using (var blpImage = new BlpFile(fileStream))
                        {
                            using (var bit = blpImage.getBitmap(0))
                            {
                                var source = Imaging.CreateBitmapSourceFromHBitmap(
                                    bit.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty,
                                    BitmapSizeOptions.FromWidthAndHeight(bit.Width, bit.Height));
                                if (!_ImageSourceMap.ContainsKey(iconPath))
                                    _ImageSourceMap.Add(iconPath, source);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[BlpManager] WARNING Unable to load image: {iconPath}\n{e.Message}");
                }
            }
            watch.Stop();
            Console.WriteLine($"[BlpManager] Loaded {iconPaths.Count} images in {watch.ElapsedMilliseconds}ms");
        }

        public BitmapSource GetSourceForImagePath(string path)
        {
            return _ImageSourceMap.ContainsKey(path) ? _ImageSourceMap[path] : null;
        }

        public static BlpManager GetInstance()
        {
            return _Instance;
        }
    }
}
