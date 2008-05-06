using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Xml.Serialization;
using System.ComponentModel;
using System.IO;

namespace VideoMonitor_Proj3
{
    [QS.Fx.Reflection.ValueClass("10`1", "VMImage")]
    public class VMImage
    {
        public VMImage(Bitmap img)
        {
            picture = img;
        }

        public VMImage()
        {
        }

        private Bitmap picture;

        // the 'Picture' Bitmap as an array of bytes.
        [XmlIgnoreAttribute()]
        public Bitmap Picture
        {
            get { return picture; }
            set { picture = value; }
        }

        // Serializes the 'Picture' Bitmap to XML.
        [XmlElementAttribute("Picture")]
        public byte[] PictureByteArray
        {
            get
            {
                if (picture != null)
                {
                    TypeConverter BitmapConverter =
                         TypeDescriptor.GetConverter(picture.GetType());
                    return (byte[])
                         BitmapConverter.ConvertTo(picture, typeof(byte[]));
                }
                else
                    return null;
            }

            set
            {
                if (value != null)
                    picture = new Bitmap(new MemoryStream(value));
                else
                    picture = null;
            }
        }
    }
}
