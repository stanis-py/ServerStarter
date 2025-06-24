using System;
using System.Drawing;
using System.IO;

namespace ServerLauncher
{
    public static class IconGenerator
    {
        public static void CreateDefaultIcon(string outputPath)
        {
            // Create a simple icon with a blue background and "SL" text
            using (Bitmap bitmap = new Bitmap(32, 32))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    // Draw background
                    graphics.Clear(Color.RoyalBlue);
                    
                    // Draw text
                    using (Font font = new Font("Arial", 14, FontStyle.Bold))
                    {
                        using (StringFormat format = new StringFormat())
                        {
                            format.Alignment = StringAlignment.Center;
                            format.LineAlignment = StringAlignment.Center;
                            
                            using (Brush brush = new SolidBrush(Color.White))
                            {
                                graphics.DrawString("SL", font, brush, new RectangleF(0, 0, 32, 32), format);
                            }
                        }
                    }
                }
                
                // Save the bitmap as an icon
                using (Icon icon = Icon.FromHandle(bitmap.GetHicon()))
                {
                    using (FileStream stream = new FileStream(outputPath, FileMode.Create))
                    {
                        icon.Save(stream);
                    }
                }
            }
        }
    }
} 