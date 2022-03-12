using System;
using System.Drawing;
using System.Windows.Forms;
namespace LaserPrinting.Helpers
{
    public class ThreadHelper
    {
        public delegate void ControlSetTextDelegate(Control control, string text);
        public delegate void ControlSetBgColorDelegate(Control control, Color color);
        public static void ControlSetText(Control control, string text)
        {
            if (control == null) return;
            if (control.InvokeRequired)
            {
                control.Invoke(new ControlSetTextDelegate(ControlSetText), control, text);
                return;
            }
            control.Text = text;

        }
        public static void ControlAppendFirstText(Control control, string text)
        {
            if (control == null) return;
            if (control.InvokeRequired)
            {
                control.Invoke(new ControlSetTextDelegate(ControlAppendFirstText), control, text);
                return;
            }
            control.Text = text+ Environment.NewLine+ control.Text;

        }
        public static void ControlSetBgColor(Control control, Color color)
        {
            if (control == null) return;
            if (control.InvokeRequired)
            {
                control.Invoke(new ControlSetBgColorDelegate(ControlSetBgColor), control, color);
                return;
            }
            control.BackColor = color;

        }
    }
}
