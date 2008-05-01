using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using TachyonLabs.SharpSpell.UI;

namespace CS514_HW2
{
    public class CustomPaintTextBox : NativeWindow
    {
        private TextBox parentTextBox;
        private Bitmap bitmap;
        private Graphics textBoxGraphics;
        private Graphics bufferGraphics;
        // this is where we intercept the Paint event for the TextBox at the OS level  
        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            switch (m.Msg)
            {
                case 15: // this is the WM_PAINT message  
                    // invalidate the TextBox so that it gets refreshed properly  
                    parentTextBox.Invalidate();
                    // call the default win32 Paint method for the TextBox first  
                    base.WndProc(ref m);
                    // now use our code to draw extra stuff over the TextBox  
                    this.CustomPaint();
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }
        public CustomPaintTextBox(TextBox textBox)
        {
            this.parentTextBox = textBox;
            this.bitmap = new Bitmap(textBox.Width, textBox.Height);
            this.bufferGraphics = Graphics.FromImage(this.bitmap);
            this.bufferGraphics.Clip = new Region(textBox.ClientRectangle);
            this.textBoxGraphics = Graphics.FromHwnd(textBox.Handle);
            // Start receiving messages (make sure you call ReleaseHandle on Dispose):  
            this.AssignHandle(textBox.Handle);
        }
        private void CustomPaint()
        {
            // get the index of the first visible line in the TextBox  
            int curPos = TextBoxAPIHelper.GetFirstVisibleLine(parentTextBox);
            curPos = TextBoxAPIHelper.GetLineIndex(parentTextBox, curPos);
            // clear the graphics buffer  
            bufferGraphics.Clear(Color.Transparent);
            // * Here’s where the magic happens  
            // For simplicity we’ll just draw a line underneath a character range.  
            // This will be from character 1 to character 5 in this sample.  
            // You can of course modify this to underline specific words.  
            Point start = TextBoxAPIHelper.PosFromChar(parentTextBox, 1);
            Point end = TextBoxAPIHelper.PosFromChar(parentTextBox, 5);
            // The position above now points to the top left corner of the character.  
            // We need to account for the character height so the underlines go  
            // to the right place.  
            end.X += 1;
            start.Y += TextBoxAPIHelper.GetBaselineOffsetAtCharIndex(parentTextBox, 1);
            end.Y += TextBoxAPIHelper.GetBaselineOffsetAtCharIndex(parentTextBox, 5);
            // Draw the wavy underline.  
            DrawWave(start, end);
            // Now we just draw our internal buffer on top of the TextBox.  
            // Everything should be at the right place.  
            textBoxGraphics.DrawImageUnscaled(bitmap, 0, 0);
        }

        private void DrawWave(Point start, Point end)   {
            Pen pen = Pens.Red;  
            if ((end.X-start.X) > 4)  
            { 
                List<Point> pl = new List<Point>();  
                for (int i = start.X; i <= (end.X-2); i += 4)  
                {
                    pl.Add(new Point(i, start.Y));  
                    pl.Add(new Point(i + 2, start.Y + 2));  
                }  
                Point [] p = pl.ToArray();  
                bufferGraphics.DrawLines(pen, p);  
            }  
            else   
            {  
                bufferGraphics.DrawLine(pen, start, end);  
            }
        }
    }
}
