using System;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using EmulatePreTrialLaser.Properties;

namespace EmulatePreTrialLaser
{
    public partial class Form1 : Form
    {
        private int _waitCounter;
        private int _generateTargetQty;
        private int _generatedQty;
        private int _index;
        private bool _abortRequest;
        private string _filaname;
        private string _article;
        private string _prefix;
        private readonly string _fileNamePrefix;
        private readonly string _suffix;

        public Form1()
        {
            InitializeComponent();
           
            var setting = new Settings();
            _prefix = setting.Prefix;
            _fileNamePrefix = setting.FilePrefix;
            _suffix = setting.Sufix;

            var dt = DateTime.Now;
            var day = dt.Day + 31;
            _filaname = $"{_fileNamePrefix}_{dt:yyyyMM}{day}.TXT";
            tbFolder.Text = _filaname;
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            timer1.Start();
            _waitCounter = Convert.ToInt32(nmInterval.Value);
            _generateTargetQty = Convert.ToInt32(nmTarget.Value);
            _generatedQty = 0;
            btnGenerate.Enabled = false;
            btnAbort.Enabled = true;
            _abortRequest = false;
            nmTarget.Enabled = false;
            _index = Convert.ToInt32(nmIndex.Value);
            nmIndex.Enabled = false;
            _article = tbArticle.Text;
            tbArticle.Enabled = false;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            _waitCounter--;
            if (_waitCounter < 0)
            {
                //generate
                var textNoCheckSum = $"{_prefix}{_index+1:0000}{_suffix}";
                var cs = Barcode.CheckSumCalc(textNoCheckSum);
                var text = textNoCheckSum + cs;
                 var format = $"yyyy-MM-dd HH:mm:ss{CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator}fff";
                var textLine1 = DateTime.Now.ToString(format) +$" replace barcode with the same date";
                var textLine2 = DateTime.Now.ToString(format) +$" the MarkCode:{text},the Marked Count:{_index},Marking status:finish,DocName:{_article}.hs";
                using (var writer = new StreamWriter(tbFolder.Text, true))
                {
                    writer.WriteLine(textLine1);
                    writer.WriteLine(textLine2);
                }
                _waitCounter = Convert.ToInt32(nmInterval.Value); 
                _generatedQty++;
                _index++;
                nmIndex.Text = _index.ToString();
            }

            btnGenerate.Text = $@"Wait ({_waitCounter}), Remaining ({_generateTargetQty-_generatedQty})";
            if (_generatedQty < _generateTargetQty && !_abortRequest)
            {
                timer1.Start();
            }
            else
            {
                btnGenerate.Enabled = true;
                btnAbort.Enabled = false;
                btnGenerate.Text = "Generate";
                nmTarget.Enabled = true;
                nmIndex.Enabled = true;
                tbArticle.Enabled = true;
            }
           
        }

        private void btnAbort_Click(object sender, EventArgs e)
        {
            _abortRequest = true;
            timer1.Stop();
            btnGenerate.Enabled = true;
            btnAbort.Enabled = false;
            btnGenerate.Text = "Generate"; 
            nmTarget.Enabled = true;
            nmIndex.Enabled = true;
            tbArticle.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                var dt = DateTime.Now;
                var day = dt.Day + 31;
                _filaname = $"{_fileNamePrefix}_{dt:yyyyMM}{day}.TXT";
                var path = Path.Combine(fbd.SelectedPath, _filaname);
                tbFolder.Text = path;
            }
        }
    }
}
