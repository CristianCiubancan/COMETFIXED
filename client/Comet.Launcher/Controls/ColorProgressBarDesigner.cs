using System.Collections;
using System.Windows.Forms.Design;

namespace Comet.Launcher.Controls
{
    internal class ColorProgressBarDesigner : ControlDesigner
    {
        public ColorProgressBarDesigner()
        { }

        // clean up some unnecessary properties
        protected override void PostFilterProperties(IDictionary properties)
        {
            properties.Remove("AllowDrop");
            properties.Remove("BackgroundImage");
            properties.Remove("ContextMenu");
            properties.Remove("FlatStyle");
            properties.Remove("Image");
            properties.Remove("ImageAlign");
            properties.Remove("ImageIndex");
            properties.Remove("ImageList");
            properties.Remove("Text");
            properties.Remove("TextAlign");
        }
	}
}
