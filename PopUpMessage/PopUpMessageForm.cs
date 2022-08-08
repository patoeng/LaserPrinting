using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PopUpMessage
{
    public partial class PopUpMessageForm : Form
    {
        private Point _max = new Point();
        private Point _newPoint = new Point();

        private Rectangle _resolution;
        //Initialize random number generator
        Random Rnd = new Random();
        public PopUpMessageForm()
        {
            InitializeComponent();
            _resolution = Screen.PrimaryScreen.Bounds;
            //Set Max point to form size minus the label size so that the whole label will always fit on the form.
            _max = new Point(this._resolution.Size - this.Size);
            tmrRandomizer.Interval = 2000;
            //Reset Max whenever the form is resized.
            this.Resize += Form1_Resize;
           
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            _max = new Point(this._resolution.Size - this.Size);
        }

        private Point RandomPoint()
        {
            _newPoint.X = Rnd.Next(_max.X);
            _newPoint.Y = Rnd.Next(_max.Y);
            return _newPoint;
        }
        private void tmrRandomizer_Tick(object sender, EventArgs e)
        {
            Location = RandomPoint();
        }

        private void tmrBlink_Tick(object sender, EventArgs e)
        {
            label1.ForeColor = label1.ForeColor == Color.Yellow ? Color.Blue : Color.Yellow;
        }
    }
}
