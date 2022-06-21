using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ComponentFactory.Krypton.Toolkit;
using LaserPrinting.Model;
using LaserPrinting.Helpers;
using LaserPrinting.Services;
using OpcenterWikLibrary;
using System.Reflection;
using MesData;


namespace LaserPrinting
{
    public partial class MainAuto : KryptonForm
    {
        #region Private fields

        private Mes _mesData;
        private string _productWorkflow;
        private DatalogFileWatcher _watcher;

        private delegate void DgDataSourceDelegate(KryptonDataGridView kdg, BindingList<LaserPrintingProduct> list);

        #endregion
        public MainAuto()
        {
            try
            {
                InitializeComponent();
                InitLaserPrinting();
                InitFileWatcher();
                InitSetting();
            }
            catch (Exception ex)
            {
                File.WriteAllText(DateTime.Now.ToString("yyyyMMddHHmmss"),ex.Source +" "+ex.Message);
            }
        }
        public void InitLaserPrinting()
        {
            /* Create round Indicator*/
            Rectangle r = new Rectangle(0, 0, Pb_IndicatorPicture.Width, Pb_IndicatorPicture.Height);
            System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();
            int d = 28;
            gp.AddArc(r.X, r.Y, d, d, 180, 90);
            gp.AddArc(r.X + r.Width - d, r.Y, d, d, 270, 90);
            gp.AddArc(r.X + r.Width - d, r.Y + r.Height - d, d, d, 0, 90);
            gp.AddArc(r.X, r.Y + r.Height - d, d, d, 90, 90);
            Pb_IndicatorPicture.Region = new Region(gp);
            this.WindowState = FormWindowState.Normal;
            this.Size = new Size(1028,810);

#if MiniMe
            var  name = "Laser Marking Minime";
            Text = Mes.AddVersionNumber(Text + " MiniMe");
#elif Ariel
            var  name = "Laser Marking Ariel";
            Text = Mes.AddVersionNumber(Text + " Ariel");
#endif

            _mesData = new Mes(name, AppSettings.Resource,name);

            MyTitle.Text = $@"Laser Printing - {AppSettings.Resource}";
            ResourceGrouping.Values.Heading = $@"Resource Status: {AppSettings.Resource}";

        }
        private void InitFileWatcher()
        {
            var setting = new Properties.Settings();
            _watcher = new DatalogFileWatcher(setting.LaserDatalogLocation, setting.LaserDatalogPattern);
            _watcher.FileChangedDetected += DatalogParserMethod;
        }
        private void InitSetting()
        {
            var setting = new Properties.Settings();
            _productWorkflow = setting.WorkFlow;
        }

        private void SetDgDataSource(KryptonDataGridView kdg, BindingList<LaserPrintingProduct> list)
        {
            if (kdg.InvokeRequired)
            {
                kdg.Invoke(new DgDataSourceDelegate(SetDgDataSource), kdg, list);
                return;
            }

            kdg.DataSource = list;
        }
        private async Task<bool> DatalogParserMethod(string fileLocation)
        {
            var fileNewPath = fileLocation.Replace('\\', '/');
            ThreadHelper.ControlSetText(Tb_Message, "");
            if (_mesData.ManufacturingOrder == null)
            {
                ThreadHelper.ControlAppendFirstText(Tb_Message, $"{DateTime.Now:s} :Parsing --->[{fileLocation}] Canceled, Invalid Manufacturing Order.");
                return false;
            }
            if (_mesData.ResourceStatusDetails == null || _mesData.ResourceStatusDetails?.Availability != "Up")
            {
                ThreadHelper.ControlAppendFirstText(Tb_Message, $"{DateTime.Now:s} :Parsing --->[{fileLocation}] Canceled, Resource is not in \"Up\" condition.");
                return false;
            }
            ThreadHelper.ControlAppendFirstText(Tb_Message, $"{DateTime.Now:s} :Parsing ---> {fileLocation}");

            var result = await DatalogFile.GetDatalogFileByFileName(fileNewPath);
            if (!result.Result) return false;
            var dataLogFile = (DatalogFile) result.Data;
            var list = DatalogFile.FileParse(ref dataLogFile);

            SetDgDataSource(kryptonDataGridView1, new BindingList<LaserPrintingProduct>(list));

            // MoveStart, MoveIn, Move
            var save = await DatalogFile.SaveDatalogFileHistory(dataLogFile);
            if (!save.Result)
            {
                return false;
            }
            foreach(var sn in list)
            {
                var s = await StartMoveInMove(sn);
                ThreadHelper.ControlAppendFirstText(Tb_Message,$"{DateTime.Now:s} :[{sn.Barcode}] ---> {s}");
            }
            ThreadHelper.ControlAppendFirstText(Tb_Message, $"{DateTime.Now:s} :Parsing ---> {fileLocation}... Done");
            await ContainerOfMfgOrder();
            return true;
        }

        private async Task SetMfgOrder()
        {
            if (_watcher.Busy)
            {
                MessageBox.Show("Datalog Parsing In Progress");
                return;
            }
            if (_mesData.ResourceStatusDetails == null || _mesData.ResourceStatusDetails?.Availability != "Up")
            {
                MessageBox.Show("Canceled, Resource is not in \"Up\" condition."); 
                return;
            }
            var mfg = await Mes.GetMfgOrder(_mesData, Tb_MfgOrder.Text);
            _mesData.SetManufacturingOrder(mfg);

            if (_mesData.ManufacturingOrder != null)
            {
                ClearTextBox();
                if (_mesData.ManufacturingOrder.Name.ToString() != "") Tb_MfgOrder.Text = _mesData.ManufacturingOrder.Name.ToString();
                if (_mesData.ManufacturingOrder.Product != null)
                {
                    Tb_MfgProduct.Text = _mesData.ManufacturingOrder.Product.Name;
                    var productChanges = await Mes.GetProduct(_mesData, _mesData.ManufacturingOrder.Product.Name);

                    if (productChanges != null)
                    {
                        //Make Sure correct WorkFlow
                        if (productChanges.Workflow.Name != _productWorkflow)
                        {
                            _mesData.SetManufacturingOrder(mfg);
                            ClearTextBox();
                            MessageBox.Show(@"Incorrect Product Work Flow!");
                            return;
                        }

                        Tb_MfgWorkflow.Text = productChanges.Workflow.Name;
                        Tb_MfgProductDescription.Text = Convert.ToString(productChanges.Description);
                    }
                }
                //if (_mesData.ManufacturingOrder.isWorkflow != null) Tb_MfgWorkflow.Text = _mesData.ManufacturingOrder.isWorkflow.Name;
                if (_mesData.ManufacturingOrder.UOM != null) Tb_MfgUOM.Text = _mesData.ManufacturingOrder.UOM.Name;
                if (_mesData.ManufacturingOrder.sswQtyStarted != null) Tb_MfgInProcess.Text = Convert.ToString(_mesData.ManufacturingOrder.sswQtyStarted);
                if (_mesData.ManufacturingOrder.Qty != null) Tb_MfgQty.Text = Convert.ToString(_mesData.ManufacturingOrder.Qty);
                if (Convert.ToString(_mesData.ManufacturingOrder.PlannedStartDate) != "") Tb_MfgStartedDate.Text = Convert.ToString(_mesData.ManufacturingOrder.PlannedStartDate);
                if (Convert.ToString(_mesData.ManufacturingOrder.PlannedCompletionDate) != "") Tb_MfgEndDate.Text = Convert.ToString(_mesData.ManufacturingOrder.PlannedCompletionDate);
                await ContainerOfMfgOrder();
            }
            else
            {
                MessageBox.Show("Manufacturing Order is not found!");
            }
        }
        private async Task ContainerOfMfgOrder()
        {
            var mfg = await Mes.GetMfgOrder(_mesData, Tb_MfgOrder.Text);
            _mesData.SetManufacturingOrder(mfg);

            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => Lb_ContainerList.Items.Clear()));
            }
            else
            {
                Lb_ContainerList.Items.Clear();
            }
           
            if (_mesData.ManufacturingOrder != null)
            {
                if (_mesData.ManufacturingOrder.Name != null) ThreadHelper.ControlSetText(MfgContainerLabel, $"List Container of {_mesData.ManufacturingOrder.Name}");
                if (_mesData.ManufacturingOrder.Containers != null)
                {
                    if (_mesData.ManufacturingOrder.Containers.Length > 0)
                    {
                        //var oListOfContainer = oMfgOrderChanges.Containers.OrderBy(x => x.Value.ToString()).ToList();
                        foreach (var container in _mesData.ManufacturingOrder.Containers)
                        {
                            if (InvokeRequired)
                            {
                                Invoke(new MethodInvoker(() => Lb_ContainerList.Items.Add(container.Value)));
                            }
                            else
                            {
                                Lb_ContainerList.Items.Add(container.Value);
                            }
                        }
                    }

                    ThreadHelper.ControlSetText(Tb_MfgInProcess, Lb_ContainerList.Items.Count.ToString());
                }
            }
           
        }
       
        private async Task<string> StartMoveInMove(LaserPrintingProduct product)
        {
            try
            {
                if (_mesData.ManufacturingOrder.Name == "" || product.Barcode == "")
                    return "Manufacturing Order And Container Name is Needed!";

                var resultStart = await Mes.ExecuteStart(_mesData, product.Barcode,(string) _mesData.ManufacturingOrder.Name, _mesData.ManufacturingOrder.Product.Name, Tb_MfgWorkflow.Text, Tb_MfgUOM.Text, product.PrintedDateTime);
                if (!resultStart.Result) return $"Container Start failed. {resultStart.Message}";

                var transaction = await Mes.ExecuteMoveIn(_mesData, product.Barcode, product.PrintedDateTime);
                var resultMoveIn = transaction.Result || transaction.Message == "Move-in has already been performed for this operation.";
                if (!resultMoveIn && transaction.Message.Contains("TimeOut"))
                {
                    transaction = await Mes.ExecuteMoveIn(_mesData, product.Barcode, product.PrintedDateTime);
                    resultMoveIn = transaction.Result || transaction.Message == "Move-in has already been performed for this operation.";
                    if (!resultMoveIn && transaction.Message.Contains("TimeOut"))
                    {
                        transaction = await Mes.ExecuteMoveIn(_mesData, product.Barcode, product.PrintedDateTime);
                        resultMoveIn = transaction.Result || transaction.Message == "Move-in has already been performed for this operation.";
                    }
                }
                if (!resultMoveIn) return $"Container failed Move In. {transaction.Result}";


                var cDataPoint = product.LaserMarkingData.ToDataPointDetailsList().ToArray();

                
                var resultMoveStd = await Mes.ExecuteMoveStandard(_mesData, product.Barcode, DateTime.Now, cDataPoint);
                if (!resultMoveStd.Result && resultMoveStd.Message.Contains("TimeOut"))
                {
                    resultMoveStd = await Mes.ExecuteMoveStandard(_mesData, product.Barcode, DateTime.Now, cDataPoint);
                    if (!resultMoveStd.Result && resultMoveStd.Message.Contains("TimeOut"))
                    {
                        resultMoveStd = await Mes.ExecuteMoveStandard(_mesData, product.Barcode, DateTime.Now, cDataPoint);
                    }
                }
                return resultMoveStd.Result ? "Container success to Start, MoveIn and Moved Std" : $"Container success to Start and MoveIn but failed to Moved Std. {resultMoveStd.Message}";
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
                return "Exception";
            }
        }
     

        private void RealtimeMfg_Click(object sender, EventArgs e)
        {
            if (RealtimeMfg.Checked)
            {
                Tb_MfgOrder.Enabled = false;
                Tb_MfgOrder.StateCommon.Content.Color1 = Color.Gray;
                Tb_MfgOrder.StateCommon.Border.Color1 = Color.Gray;
                Tb_MfgOrder.StateCommon.Border.Color2 = Color.Gray;
                Bt_SetMfgOrder.Enabled = false;
                Bt_SetMfgOrder.StateCommon.Back.Color1 = Color.Gray;
                Bt_SetMfgOrder.StateCommon.Back.Color2 = Color.Gray;
                Bt_SetMfgOrder.StateCommon.Border.Color1 = Color.Gray;
                Bt_SetMfgOrder.StateCommon.Border.Color2 = Color.Gray;
            }
            else
            {
                Tb_MfgOrder.Enabled = true;
                Tb_MfgOrder.StateCommon.Content.Color1 = Color.Black;
                Tb_MfgOrder.StateCommon.Border.Color1 = Color.Black;
                Tb_MfgOrder.StateCommon.Border.Color2 = Color.Black;
                Bt_SetMfgOrder.Enabled = true;
                Bt_SetMfgOrder.StateCommon.Back.Color1 = Color.FromArgb(6, 174, 244);
                Bt_SetMfgOrder.StateCommon.Back.Color2 = Color.FromArgb(8, 142, 254);
                Bt_SetMfgOrder.StateCommon.Border.Color1 = Color.FromArgb(6, 174, 244);
                Bt_SetMfgOrder.StateCommon.Border.Color2 = Color.FromArgb(8, 142, 254);
            }
        }

        private async void TimerRealtime_Tick(object sender, EventArgs e)
        {
            TimerRealtime.Stop();

           
                await GetStatusOfResource();
                await GetStatusMaintenanceDetails();
                if (RealtimeMfg.Checked == true)
                {
                    //  GetMfgOrderMustBeDoing();
                }

                TimerRealtime.Start();
        }

        private async Task GetStatusMaintenanceDetails()
        {
            try
            {
                var maintenanceStatusDetails = await Mes.GetMaintenanceStatusDetails(_mesData);
                if (maintenanceStatusDetails != null)
                {
                    Dg_Maintenance.DataSource = maintenanceStatusDetails;
                    Dg_Maintenance.Columns["Due"].Visible = false;
                    Dg_Maintenance.Columns["Warning"].Visible = false;
                    Dg_Maintenance.Columns["PastDue"].Visible = false;
                    Dg_Maintenance.Columns["MaintenanceReqName"].Visible = false;
                    Dg_Maintenance.Columns["MaintenanceReqDisplayName"].Visible = false;
                    Dg_Maintenance.Columns["ResourceStatusCodeName"].Visible = false;
                    Dg_Maintenance.Columns["UOMName"].Visible = false;
                    Dg_Maintenance.Columns["ResourceName"].Visible = false;
                    Dg_Maintenance.Columns["UOM2Name"].Visible = false;
                    Dg_Maintenance.Columns["MaintenanceReqRev"].Visible = false;
                    Dg_Maintenance.Columns["NextThruputQty2Warning"].Visible = false;
                    Dg_Maintenance.Columns["NextThruputQty2Limit"].Visible = false;
                    Dg_Maintenance.Columns["UOM2"].Visible = false;
                    Dg_Maintenance.Columns["ThruputQty2"].Visible = false;
                    Dg_Maintenance.Columns["Resource"].Visible = false;
                    Dg_Maintenance.Columns["ResourceStatusCode"].Visible = false;
                    Dg_Maintenance.Columns["NextThruputQty2Due"].Visible = false;
                    Dg_Maintenance.Columns["MaintenanceClassName"].Visible = false;
                    Dg_Maintenance.Columns["MaintenanceStatus"].Visible = false;
                    Dg_Maintenance.Columns["ExportImportKey"].Visible = false;
                    Dg_Maintenance.Columns["DisplayName"].Visible = false;
                    Dg_Maintenance.Columns["Self"].Visible = false;
                    Dg_Maintenance.Columns["IsEmpty"].Visible = false;
                    Dg_Maintenance.Columns["FieldAction"].Visible = false;
                    Dg_Maintenance.Columns["IgnoreTypeDifference"].Visible = false;
                    Dg_Maintenance.Columns["ListItemAction"].Visible = false;
                    Dg_Maintenance.Columns["ListItemIndex"].Visible = false;
                    Dg_Maintenance.Columns["CDOTypeName"].Visible = false;
                    Dg_Maintenance.Columns["key"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }
        private async Task GetStatusOfResource()
        {
            try
            {
                var resourceStatus = await Mes.GetResourceStatusDetails(_mesData);
                _mesData.SetResourceStatusDetails(resourceStatus);

                if (resourceStatus != null)
                {
                    if (resourceStatus.Status != null) Tb_StatusCode.Text = resourceStatus.Status.Name;
                    if (resourceStatus.Reason != null) Tb_StatusReason.Text = resourceStatus.Reason.Name;
                    if (resourceStatus.Availability != null)
                    {
                        Tb_Availability.Text = resourceStatus.Availability.Value;
                        if (resourceStatus.Availability.Value == "Up")
                        {
                            Pb_IndicatorPicture.BackColor = Color.Green;
                        }
                        else if (resourceStatus.Availability.Value == "Down")
                        {
                            Pb_IndicatorPicture.BackColor = Color.Red;
                        }
                    }
                    else
                    {
                        Pb_IndicatorPicture.BackColor = Color.Orange;
                    }

                    if (resourceStatus.TimeAtStatus != null)
                        Tb_TimeAtStatus.Text =
                            $@"{DateTime.FromOADate(resourceStatus.TimeAtStatus.Value) - Mes.ZeroEpoch():G}";
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }

        private async void Bt_SetMfgOrder_Click(object sender, EventArgs e)
        {
            Bt_SetMfgOrder.Enabled = false;
            Tb_MfgOrder.Enabled = false;
            RealtimeMfg.Enabled = false;
            await SetMfgOrder();
            Bt_SetMfgOrder.Enabled = true;
            Tb_MfgOrder.Enabled = true;
            RealtimeMfg.Enabled = true;
        }
        private void ClearTextBox()
        {
            Tb_MfgOrder.Clear();
            Tb_MfgProduct.Clear();
            Tb_MfgProductDescription.Clear();
            Tb_MfgWorkflow.Clear();
            Tb_MfgQty.Clear();
            Tb_MfgStartedDate.Clear();
            Tb_MfgEndDate.Clear();
            Tb_MfgUOM.Clear();
        }

        private async void Tb_MfgOrder_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (string.IsNullOrEmpty(Tb_MfgOrder.Text))return;
                Bt_SetMfgOrder.Enabled = false;
                Tb_MfgOrder.Enabled = false;
                RealtimeMfg.Enabled = false;
                await SetMfgOrder();
                Bt_SetMfgOrder.Enabled = true;
                Tb_MfgOrder.Enabled = true;
                RealtimeMfg.Enabled = true;
            }
        }

        private async void MainAuto_Load(object sender, EventArgs e)
        {
            await GetStatusOfResource();
            await GetStatusMaintenanceDetails();
            ActiveControl = Tb_MfgOrder;
        }

        private async void btnResourceSetup_Click(object sender, EventArgs e)
        {
            Mes.ResourceSetupForm(this, _mesData, MyTitle.Text);
            await GetStatusOfResource();
        }
    }
}
