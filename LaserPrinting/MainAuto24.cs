using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Camstar.WCF.ObjectStack;
using ComponentFactory.Krypton.Toolkit;
using LaserPrinting.Helpers;
using LaserPrinting.Model;
using LaserPrinting.Properties;
using LaserPrinting.Services;
using MesData;
using MesData.Login;
using OpcenterWikLibrary;

namespace LaserPrinting
{
    public partial class MainAuto24 : KryptonForm
    {
        private DatalogFileWatcher _watcher;
        private Mes _mesData;
        private string _productWorkflow;
        private LocalProductionOrder _currentPo;
        private string _dataLocalPo;
        private int _indexMaintenanceState;

        public MainAuto24()
        {
            InitializeComponent();
            InitSetting();
            InitLaserPrinting();
            InitFileWatcher();
            ClearTextBox();
            SetProductionState(ProductionState.Idle);
        }
        public void InitLaserPrinting()
        {

#if MiniMe
            var name = "Laser Marking Minime";
             Text = Mes.AddVersionNumber(name);
#elif Ariel
            var name = "Laser Marking Ariel";
            Text = Mes.AddVersionNumber(name);
#endif

            _mesData = new Mes(name, AppSettings.Resource,name);

            MyTitle.Text = @"Laser Printing";
            lbTitle.Text = AppSettings.Resource;
            kryptonNavigator1.SelectedIndex = 0;
            EventLogUtil.LogEvent("Application Start");

            //Prepare Maintenance Grid
            var maintStrings = new[] { "Resource", "MaintenanceType", "MaintenanceReq", "NextDateDue", "NextThruputQtyDue", "MaintenanceState" };

            for (int i = 0; i < Dg_Maintenance.Columns.Count; i++)
            {
                if (!maintStrings.Contains(Dg_Maintenance.Columns[i].DataPropertyName))
                {
                    Dg_Maintenance.Columns[i].Visible = false;
                }
                else
                {
                    switch (Dg_Maintenance.Columns[i].HeaderText)
                    {

                        case "MaintenanceType":
                            Dg_Maintenance.Columns[i].HeaderText = @"Maintenance Type";
                            break;
                        case "MaintenanceReq":
                            Dg_Maintenance.Columns[i].HeaderText = @"Maintenance Requirement";
                            break;
                        case "NextDateDue":
                            Dg_Maintenance.Columns[i].HeaderText = @"Next Due Date";
                            break;
                        case "NextThruputQtyDue":
                            Dg_Maintenance.Columns[i].HeaderText = @"Next Thruput Quantity Due";
                            break;
                        case "MaintenanceState":
                            Dg_Maintenance.Columns[i].HeaderText = @"Maintenance State";
                            _indexMaintenanceState = Dg_Maintenance.Columns[i].Index;
                            break;
                    }

                }
            }
        }
        private void InitFileWatcher()
        {
            var setting = new Settings();
            if (!Directory.Exists(setting.LaserDatalogLocation))
            {
                KryptonMessageBox.Show($"Directory {setting.LaserDatalogLocation} does not exist!\r\nApplication will now exit!", "Init File Watcher",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.ExitThread();
                Application.Exit();
            }
            _watcher = new DatalogFileWatcher(setting.LaserDatalogLocation, setting.LaserDatalogPattern);
            _watcher.FileChangedDetected += DatalogParserMethod;
        }
        private void InitSetting()
        {
            var setting = new Settings();
            _productWorkflow = setting.WorkFlow;
            Tb_DummyQty.Value = setting.DummyQty;
            Tb_TimeOffset.Value =(decimal) setting.TimeOffset;
        }

     
        private async Task<bool> DatalogParserMethod(string fileLocation)
        {
            var fileNewPath = fileLocation.Replace('\\', '/');
           // ThreadHelper.ControlSetText(Tb_Message, "");
            if (_mesData.ManufacturingOrder == null)
            {
               return false;
            }
            if (_mesData.ResourceStatusDetails == null || _mesData.ResourceStatusDetails?.Availability != "Up")
            {
                return false;
            }

            var result = await DatalogFile.GetDatalogFileByFileName(fileNewPath);
            if (!result.Result) return false;
            var dataLogFile = (DatalogFile)result.Data;
            var list = DatalogFile.FileParse(ref dataLogFile);


            // MoveStart, MoveIn, Move
            var save = await DatalogFile.SaveDatalogFileHistory(dataLogFile);
            if (!save.Result)
            {
                return false;
            }
            foreach (var sn in list)
            {
                var s = await StartMoveInMove(sn);
            }
            return true;
        }

        private async Task<bool> SetMfgOrder()
        {
            if (_watcher.Busy)
            {
                MessageBox.Show("Datalog Parsing In Progress");
                return false;
            }
            if (_mesData.ResourceStatusDetails == null ||_mesData.ResourceStatusDetails?.Availability != "Up" && _mesData.ResourceStatusDetails?.Reason?.Name!="Setting")
            {
                MessageBox.Show("Canceled, Resource is not in \"Up\" condition.");
                return false;
            }
            var mfg = await Mes.GetMfgOrder(_mesData, Tb_MfgOrder.Text);
            _mesData.SetManufacturingOrder(mfg);

            if (_mesData.ManufacturingOrder != null)
            {
                ClearTextBox();
                var setting = new Settings();
                _dataLocalPo = $"{setting.DataLocation}\\{_mesData.ManufacturingOrder.Name}.json";
                _currentPo = LocalProductionOrder.Load(_dataLocalPo);
                _currentPo.ProductionOrder = _mesData.ManufacturingOrder.Name.ToString();
                _currentPo.DummyQty = setting.DummyQty;
                _currentPo.TimeOffset = setting.TimeOffset;
                _currentPo.Save();

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
                            return false;
                        }

                        _mesData.WorkFlow = productChanges.Workflow.Name;
                        Tb_Description.Text = Convert.ToString(productChanges.Description);
                    }
                }
                //if (_mesData.ManufacturingOrder.isWorkflow != null) Tb_MfgWorkflow.Text = _mesData.ManufacturingOrder.isWorkflow.Name;
                if (_mesData.ManufacturingOrder.Qty != null)
                    Tb_MfgQty.Text = _mesData.ManufacturingOrder.Qty.ToString();
                if (Convert.ToString(_mesData.ManufacturingOrder.PlannedStartDate) != "") Tb_MfgStartedDate.Text = Convert.ToString(_mesData.ManufacturingOrder.PlannedStartDate);
                if (Convert.ToString(_mesData.ManufacturingOrder.PlannedCompletionDate) != "") Tb_MfgEndDate.Text = Convert.ToString(_mesData.ManufacturingOrder.PlannedCompletionDate);
               // if (Convert.ToString(_mesData.ManufacturingOrder.Product?.Name) != "") Tb_ArticleNumber.Text = Convert.ToString(_mesData.ManufacturingOrder.Product?.Name);

                if (_mesData.ManufacturingOrder.Product != null)
                {
                    var opecImage = await Mes.GetImage(_mesData, _mesData.ManufacturingOrder.Product.Name);
                    if (opecImage != null)
                    {
                        pbProduct.ImageLocation = opecImage.Identifier.ToString();
                    }
                    var count = await Mes.GetCounterFromMfgOrder(_mesData);
                    Tb_LaserQty.Text = count.ToString();
                    if (_currentPo.ContainerList.Count >= _currentPo.DummyQty && !_currentPo.PreparationFinished)
                    {
                        await Mes.SetResourceStatus(_mesData, "LS - Productive Time", "Quality Inspection");
                        await GetStatusOfResource();
                    }
                }

                return true;
            }
            else
            {
                MessageBox.Show(@"Manufacturing Order is not found!");
            }

            return false;
        }
     

        private async Task<string> StartMoveInMove(LaserPrintingProduct product)
        {
            try
            {
                if (_mesData.ManufacturingOrder.Name == "" || product.Barcode == "")
                    return "Manufacturing Order And Container Name is Needed!";

                if (product.PrintedDateTime < _mesData.ProductionDateStart) return "Container date is older than production date!";
                if (product.PrintedDateTime > DateTime.Now) return "Container date is in the future!";

                var dMoveIn = product.PrintedDateTime.AddHours(_currentPo.TimeOffset);
                ThreadHelper.ControlSetText(Tb_ArticleNumber,product.ArticleNumber);
                ThreadHelper.ControlSetText(Tb_SerialNumber, product.Barcode);
                ThreadHelper.ControlSetText(lbMoveIn, dMoveIn.ToString(Mes.DateTimeStringFormat));
                ThreadHelper.ControlSetText(lbMoveOut, "");

                var resultStart = await Mes.ExecuteStart(_mesData, product.Barcode, (string)_mesData.ManufacturingOrder.Name, _mesData.ManufacturingOrder.Product.Name,_mesData.WorkFlow, Tb_MfgQty.Text, dMoveIn);
                if (!resultStart.Result) return $"Container Start failed. {resultStart.Message}";

                var transaction = await Mes.ExecuteMoveIn(_mesData, product.Barcode, dMoveIn);
                var resultMoveIn = transaction.Result || transaction.Message == "Move-in has already been performed for this operation.";
                if (!resultMoveIn && transaction.Message.Contains("TimeOut"))
                {
                    transaction = await Mes.ExecuteMoveIn(_mesData, product.Barcode, dMoveIn);
                    resultMoveIn = transaction.Result || transaction.Message == "Move-in has already been performed for this operation.";
                    if (!resultMoveIn && transaction.Message.Contains("TimeOut"))
                    {
                        transaction = await Mes.ExecuteMoveIn(_mesData, product.Barcode, dMoveIn);
                        resultMoveIn = transaction.Result || transaction.Message == "Move-in has already been performed for this operation.";
                    }
                }

                if (!resultMoveIn)
                {// check if fail by maintenance Past Due
                    var transPastDue = Mes.GetMaintenancePastDue(_mesData.MaintenanceStatusDetails);
                    if (transPastDue.Result && transPastDue.Data!=null)
                    {
                        KryptonMessageBox.Show(this, "This resource under maintenance, need to complete!", "Move In",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    return $"Container failed Move In. {transaction.Result}";
                }

                var dMoveOut = DateTime.Now.AddHours(_currentPo.TimeOffset);
                var cDataPoint = product.LaserMarkingData.ToDataPointDetailsList().ToArray();

                var resultMoveStd = await Mes.ExecuteMoveStandard(_mesData, product.Barcode, dMoveOut, cDataPoint);
                if (!resultMoveStd.Result && resultMoveStd.Message.Contains("TimeOut"))
                {
                    resultMoveStd = await Mes.ExecuteMoveStandard(_mesData, product.Barcode, dMoveOut, cDataPoint);
                    if (!resultMoveStd.Result && resultMoveStd.Message.Contains("TimeOut"))
                    {
                        resultMoveStd = await Mes.ExecuteMoveStandard(_mesData, product.Barcode, dMoveOut, cDataPoint);
                    }
                }

                if (resultMoveStd.Result)
                {
                    ThreadHelper.ControlSetText(lbMoveOut, dMoveOut.ToString(Mes.DateTimeStringFormat));
                    if (!_currentPo.PreparationFinished || _mesData.ResourceStatusDetails?.Reason?.Name == "Quality Inspection")
                    {
                        _currentPo.ContainerList.Add(product.Barcode);
                        _currentPo.Save(_dataLocalPo);
                        var cAttributes = new ContainerAttrDetail[1];
                        cAttributes[0] = new ContainerAttrDetail { Name = MesContainerAttribute.LaserQualityMarkerInspect, DataType = TrivialTypeEnum.Integer, AttributeValue = $"{_currentPo.ContainerList.Count}", IsExpression = false };
                        var t = await Mes.ExecuteContainerAttrMaint(_mesData, product.Barcode, cAttributes);
                        if (_currentPo.ContainerList.Count >= _currentPo.DummyQty && _mesData?.ResourceStatusDetails?.Reason?.Name!= "Quality Inspection")
                        {
                            await Mes.SetResourceStatus(_mesData, "LS - Productive Time", "Quality Inspection");
                            await GetStatusOfResource();
                        }
                    }


                    await Mes.UpdateCounter(_mesData, 1);
                    var mfg = await Mes.GetMfgOrder(_mesData, _mesData.ManufacturingOrder.Name.ToString());
                    _mesData.SetManufacturingOrder(mfg);
                    var count = await Mes.GetCounterFromMfgOrder(_mesData);
                    ThreadHelper.ControlSetText(Tb_LaserQty, count.ToString());
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
        private async Task GetStatusOfResource()
        {
            try
            {
                var resourceStatus = await Mes.GetResourceStatusDetails(_mesData);
                _mesData.SetResourceStatusDetails(resourceStatus);

                if (resourceStatus != null)
                {
                    if (resourceStatus.Status != null) Tb_StatusCode.Text = resourceStatus.Reason?.Name;
                    if (resourceStatus.Availability != null)
                    {
                        if (resourceStatus.Availability.Value == "Up")
                        {
                            Tb_StatusCode.StateCommon.Content.Color1 = resourceStatus.Reason?.Name == "Quality Inspection" ? Color.Orange : Color.Green;
                        }
                        else if (resourceStatus.Availability.Value == "Down")
                        {
                            Tb_StatusCode.StateCommon.Content.Color1 = Color.Red;
                        }
                    }
                    else
                    {
                        Tb_StatusCode.StateCommon.Content.Color1 = Color.Orange;
                    }

                    if (resourceStatus.TimeAtStatus != null)
                        Tb_TimeAtStatus.Text = $@"{Mes.OaTimeSpanToString(resourceStatus.TimeAtStatus.Value)}";
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }
        private async Task GetStatusOfResourceDetail()
        {
            try
            {
                var resourceStatus = await Mes.GetResourceStatusDetails(_mesData);
                _mesData.SetResourceStatusDetails(resourceStatus);

                if (resourceStatus != null)
                {
                    if (_mesData.ResourceStatusDetails?.Reason?.Name != "Quality Inspection")
                    {
                        btnFinishPreparation.Enabled = true;
                        btnStartPreparation.Enabled = false;
                    }
                    if (resourceStatus.Status != null) Cb_StatusCode.Text = resourceStatus.Status.Name;
                    await Task.Delay(1000);
                    if (resourceStatus.Reason != null) Cb_StatusReason.Text = resourceStatus.Reason.Name;
                    if (resourceStatus.Availability != null)
                    {
                        Tb_StatusCodeM.Text = resourceStatus.Availability.Value;
                        if (resourceStatus.Availability.Value == "Up")
                        {
                            Tb_StatusCodeM.StateCommon.Content.Color1 = Color.Green;
                        }
                        else if (resourceStatus.Availability.Value == "Down")
                        {
                            Tb_StatusCodeM.StateCommon.Content.Color1 = Color.Red;
                        }
                    }
                    else
                    {
                        Tb_StatusCodeM.StateCommon.Content.Color1 = Color.Orange;
                    }

                    if (resourceStatus.TimeAtStatus != null)
                        Tb_TimeAtStatus.Text = $@"{Mes.OaTimeSpanToString(resourceStatus.TimeAtStatus.Value)}";
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }
        private async Task GetStatusMaintenanceDetails()
        {
            try
            {
                var maintenanceStatusDetails = await Mes.GetMaintenanceStatusDetails(_mesData);
                _mesData.SetMaintenanceStatusDetails(maintenanceStatusDetails);
                if (maintenanceStatusDetails != null)
                {
                    getMaintenanceStatusDetailsBindingSource.DataSource =
                        new BindingList<GetMaintenanceStatusDetails>(maintenanceStatusDetails);
                    Dg_Maintenance.DataSource = getMaintenanceStatusDetailsBindingSource;
                    return;
                }
                getMaintenanceStatusDetailsBindingSource.Clear();
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }
        private async Task GetResourceStatusCodeList()
        {
            try
            {
                var oStatusCodeList = await Mes.GetListResourceStatusCode(_mesData);
                if (oStatusCodeList != null)
                {
                    Cb_StatusCode.DataSource = oStatusCodeList.Where(x=>x.Name.IndexOf("LS", StringComparison.Ordinal)==0).ToList();
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }
        private void ClearTextBox()
        {
            Tb_MfgOrder.Clear();
            Tb_MfgProduct.Clear();
            Tb_Description.Clear();
            Tb_MfgQty.Clear();
            Tb_MfgStartedDate.Clear();
            Tb_MfgEndDate.Clear();
            Tb_MfgQty.Clear();
            Tb_ArticleNumber.Clear();
            Tb_SerialNumber.Clear();
            lbMoveIn.Text = "";
            lbMoveOut.Text = "";
            Tb_LaserQty.Clear();
            Tb_FinishedGoodCounter.Clear();
            kryptonDataGridView1.Rows.Clear();
            bindingSource1.DataSource = null;
        }

        private async void SetProductionState(ProductionState currentProductionState)
        {
            switch (currentProductionState)
            {
                case ProductionState.Idle:
                    ClearTextBox();
                    btnStartPreparation.Enabled = true;
                    btnEndProduction.Enabled = false;
                    btnFinishPreparation.Enabled = false;
                    Bt_SetMfgOrder.Enabled = false;
                    Tb_MfgOrder.Enabled = false;
                    break;
                case ProductionState.PreparationStarted:
                    ClearTextBox();
                    _mesData.SetManufacturingOrder(null);
                    btnStartPreparation.Enabled = false;
                    btnEndProduction.Enabled = false;
                    btnFinishPreparation.Enabled = false;
                    Bt_SetMfgOrder.Enabled = false;
                    Tb_MfgOrder.Enabled = true;
                    ActiveControl = Tb_MfgOrder;
                    break;
                case ProductionState.ManufacturingOrderSet:
                    btnEndProduction.Enabled = false;
                    if (_currentPo.PreparationFinished)
                    {
                        btnStartPreparation.Enabled = true;
                        btnFinishPreparation.Enabled = false;
                    }
                    else
                    {
                        btnStartPreparation.Enabled = false;
                        btnFinishPreparation.Enabled = true;
                    }
                    Bt_SetMfgOrder.Enabled = false;
                    Tb_MfgOrder.Enabled = false;
                    break;
                case ProductionState.PreparationFinished:
                    btnStartPreparation.Enabled = true;
                    btnEndProduction.Enabled = false;
                    btnFinishPreparation.Enabled = false;
                    Bt_SetMfgOrder.Enabled = false;
                    Tb_MfgOrder.Enabled = false;
                    break;
                case ProductionState.ProductionEnd:
                    ClearTextBox();
                    await Mes.SetResourceStatus(_mesData, "", "");
                    btnStartPreparation.Enabled = true;
                    btnEndProduction.Enabled = false;
                    btnFinishPreparation.Enabled = false;
                    Bt_SetMfgOrder.Enabled = false;
                    Tb_MfgOrder.Enabled = false;
                    break;
            }
        }
        private async void TimerRealtime_Tick(object sender, EventArgs e)
        {
            TimerRealtime.Stop();


            await GetStatusOfResource();
            await GetStatusMaintenanceDetails();
            TimerRealtime.Start();
        }

        private void kryptonTextBox5_TextChanged(object sender, EventArgs e)
        {

        }

        private async void MainAuto24_Load(object sender, EventArgs e)
        {
            await GetStatusOfResource();
            await GetResourceStatusCodeList();
            await GetStatusMaintenanceDetails();
          
        }

        private async void btnStartPreparation_Click(object sender, EventArgs e)
        {
            if (_mesData.ResourceStatusDetails == null)
            {
                KryptonMessageBox.Show("Resource Status not yet retrieved", "Start Preparation", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (_mesData.ResourceStatusDetails?.Reason?.Name == "Maintenance")
            {
                KryptonMessageBox.Show("Resource Status under Maintenance", "Start Preparation", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            SetProductionState(ProductionState.PreparationStarted);
            await Mes.SetResourceStatus(_mesData, "LS - Planned Downtime", "Setting");
            await GetStatusOfResource();
        }

        private async void Bt_SetMfgOrder_Click(object sender, EventArgs e)
        {
            var data = Tb_MfgOrder.Text.Trim();
            if (data.Length<7) return;
            if (_mesData.ResourceStatusDetails?.Reason?.Name=="Maintenance")return;
            var result = await SetMfgOrder();
            if (!result) return;
            _mesData.SetProductionDateStart(DateTime.Now);
            if (_currentPo.ContainerList.Count < _currentPo.DummyQty )
            {
                await Mes.SetResourceStatus(_mesData, "LS - Standby Time", "Ready");
            }
            else
            {
                if (_currentPo.PreparationFinished)
                {
                    await Mes.SetResourceStatus(_mesData, "LS - Productive Time", "Pass");
                }
                else
                {
                    await Mes.SetResourceStatus(_mesData, "LS - Productive Time", "Quality Inspection");
                }
            }
            
            await GetStatusOfResource();

           
            SetProductionState(ProductionState.ManufacturingOrderSet);
        }

        private async void btnFinishPreparation_Click(object sender, EventArgs e)
        {
            if (_currentPo.ContainerList.Count < _currentPo.DummyQty)
            {
                KryptonMessageBox.Show("Dummy quantity failed!");
                return;
            }

            if (_mesData.ResourceStatusDetails?.Reason?.Name == "Maintenance")
            {
                KryptonMessageBox.Show("CP under maintenance");
                return;
            }
            using (var ss = new LoginForm24("Quality"))
            {
                var dlg = ss.ShowDialog(this);
                if (dlg == DialogResult.Abort)
                {
                    KryptonMessageBox.Show("Login Failed");
                   return;
                }
                if (dlg == DialogResult.Cancel)
                {
                    return;
                }
                if (ss.UserDetails.UserRole != UserRole.Quality)return;

            }

            _currentPo.PreparationFinished = true;
            _currentPo.Save(_dataLocalPo);

            await Mes.SetResourceStatus(_mesData, "LS - Productive Time", "Pass");
            await GetStatusOfResource();
            SetProductionState(ProductionState.PreparationFinished);
        }

        private void btnEndProduction_Click(object sender, EventArgs e)
        {
            SetProductionState(ProductionState.ProductionEnd);
        }

        private void Tb_MfgOrder_KeyUp(object sender, KeyEventArgs e)
        {
            Bt_SetMfgOrder.Enabled = Tb_MfgOrder.Text.Length >= 7;
           
        }

        private async void btnCallMaintenance_Click(object sender, EventArgs e)
        {
            await Mes.SetResourceStatus(_mesData, "LS - Internal Downtime", "Maintenance");
            await GetStatusOfResource();
        }

        private async void Cb_StatusCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                var oStatusCode = await Mes.GetResourceStatusCode(_mesData, Cb_StatusCode.SelectedValue != null ? Cb_StatusCode.SelectedValue.ToString() : "");
                if (oStatusCode != null)
                {
                    Tb_StatusCodeM.Text = oStatusCode.Availability.ToString();
                    if (oStatusCode.ResourceStatusReasons != null)
                    {
                        var oStatusReason = await Mes.GetResourceStatusReasonGroup(_mesData, oStatusCode.ResourceStatusReasons.Name);
                        Cb_StatusReason.DataSource = oStatusReason.Entries;
                    }
                    else
                    {
                        Cb_StatusReason.Items.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }

        private async void btnSetMachineStatus_Click(object sender, EventArgs e)
        {
            try
            {
                var result = false;
                if (Cb_StatusCode.Text != "" && Cb_StatusReason.Text != "")
                {
                    result = await Mes.SetResourceStatus(_mesData, Cb_StatusCode.Text, Cb_StatusReason.Text);
                }
                else if (Cb_StatusCode.Text != "")
                {
                    result = await Mes.SetResourceStatus(_mesData, Cb_StatusCode.Text, "");
                }

                await GetStatusOfResourceDetail();
                KryptonMessageBox.Show(result ? "Setup status successful" : "Setup status failed");

            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }

        private async void kryptonNavigator1_SelectedPageChanged(object sender, EventArgs e)
        {
            if (kryptonNavigator1.SelectedIndex == 1)
            {
                await GetStatusOfResourceDetail();
            }
            //Serial Number of PO:
            if (kryptonNavigator1.SelectedIndex == 3)
            {
                lblPo.Text = $@"Serial Number of PO: {_mesData.ManufacturingOrder?.Name}";
                lblLoading.Visible = true;
                await GetFinishedGoodRecord();
                lblLoading.Visible = false;
            }
            if (kryptonNavigator1.SelectedIndex == 2)
            {
                if (_currentPo==null)return;

                kryptonDataGridView1.Rows.Clear();
                foreach (var container in _currentPo.ContainerList)
                {
                    kryptonDataGridView1.Rows.Add(container);
                }
            }
        }

        private async Task GetFinishedGoodRecord()
        {
            var data = await Mes.GetFinishGoodRecord(_mesData, _mesData.ManufacturingOrder?.Name.ToString(),120000);
            if (data != null)
            {
                var list = await Mes.ContainerStatusesToFinishedGood(data);
                bindingSource1.DataSource = new BindingList<FinishedGood>(list);
                kryptonDataGridView2.DataSource = bindingSource1;
                Tb_FinishedGoodCounter.Text = list.Length.ToString();
            }
        }

        private void Tb_MfgOrder_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) Bt_SetMfgOrder_Click(Bt_SetMfgOrder, null);
        }

        private void kryptonNavigator1_Selecting(object sender, ComponentFactory.Krypton.Navigator.KryptonPageCancelEventArgs e)
        {
            if (e.Index != 1 && e.Index != 2) return;

            using (var ss = new LoginForm24(e.Index==1? "Maintenance":"Quality"))
            {
                var dlg = ss.ShowDialog(this);
                if (dlg == DialogResult.Abort)
                {
                    KryptonMessageBox.Show("Login Failed");
                    e.Cancel = true;
                    return;
                }
                if (dlg == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                if (ss.UserDetails.UserRole == UserRole.Maintenance && e.Index != 1) e.Cancel = true;
                if (ss.UserDetails.UserRole == UserRole.Quality && e.Index != 2) e.Cancel = true;
            }
        }

        private void btnSetDummyQty_Click(object sender, EventArgs e)
        {
            var setting = new Settings();
            if (_currentPo == null)
            {
                KryptonMessageBox.Show(this, "Please Set PO number before Set Dummy Quantity", "Dummy Qty Set", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                Tb_DummyQty.Value = setting.DummyQty;
                return;
            }

            setting.DummyQty = (int) Tb_DummyQty.Value;
                setting.Save();
            _currentPo.DummyQty = setting.DummyQty;
            _currentPo?.Save(_dataLocalPo);
            KryptonMessageBox.Show(this, "Dummy Quantity Set Successfully", "Dummy Qty Set", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

     

        private void btnSetLaserTime_Click(object sender, EventArgs e)
        {
            var setting = new Settings
            {
                TimeOffset = (double)Tb_TimeOffset.Value
            };
            setting.Save();
            KryptonMessageBox.Show(this, "Time Offset Set Successfully", "Time Offset Qty Set", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            if (_currentPo == null) return;
            _currentPo.TimeOffset = setting.TimeOffset;
            _currentPo?.Save(_dataLocalPo);
        }

        private void Dg_Maintenance_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            try
            {
                foreach (DataGridViewRow row in Dg_Maintenance.Rows)
                {
                    switch (Convert.ToString(row.Cells[_indexMaintenanceState].Value))
                    {
                        //Console.WriteLine(Convert.ToString(row.Cells["MaintenanceState"].Value));
                        case "Pending":
                            row.DefaultCellStyle.BackColor = Color.Yellow;
                            break;
                        case "Due":
                            row.DefaultCellStyle.BackColor = Color.Orange;
                            break;
                        case "Past Due":
                            row.DefaultCellStyle.BackColor = Color.Red;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }
    }
}
