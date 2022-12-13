using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using MesData.UnitCounter;
using OpcenterWikLibrary;
using PopUpMessage;
using System.Linq.Dynamic;
using System.Threading;

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
        private MesUnitCounter _mesUnitCounter;
        private bool _allowClose;
        private bool _sortAscending;
        private BindingList<FinishedGoodLaser> _bindingList;
        private BackgroundWorker _syncWorker;
        private AbortableBackgroundWorker _moveWorker;

        public MainAuto24()
        {
            InitializeComponent();
            InitSetting();
            InitLaserPrinting();
            InitFileWatcher();
            ClearTextBox();
            SetProductionState(ProductionState.Idle);
            tmrFirstLoad.Start();
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

            _syncWorker = new BackgroundWorker();
            _syncWorker.WorkerReportsProgress = true;
            _syncWorker.RunWorkerCompleted += SyncWorkerCompleted;
            _syncWorker.ProgressChanged += SyncWorkerProgress;
            _syncWorker.DoWork += SyncDoWork;

            //_getContainerStatusWorker = new AbortableBackgroundWorker();
            //_getContainerStatusWorker.WorkerReportsProgress =true;
            //_getContainerStatusWorker.RunWorkerCompleted += GetContainerCompleted;
            //_getContainerStatusWorker.ProgressChanged += GetContainerProgress;
            //_getContainerStatusWorker.DoWork += GetContainerDoWork();

            _moveWorker = new AbortableBackgroundWorker();
            _moveWorker.WorkerReportsProgress = true;
            _moveWorker.RunWorkerCompleted += MoveWorkerCompleted;
            _moveWorker.ProgressChanged += MoveWorkerProgress;
            _moveWorker.DoWork += MoveWorkerDoWork;

        }

        private void MoveWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            var list = (List<LaserPrintingProduct>) e.Argument;
            foreach (var sn in list)
            {
                if (sn.ArticleNumber != _mesData.ManufacturingOrder.Product.Name)
                {
                    PopUpMessageHelper.Show();
                    _moveWorker.CancelAsync();
                }
                PopUpMessageHelper.CloseAll();
                var task = StartMoveInMove(sn);
                _moveWorker.ReportProgress(1,task);

            }
        }


        private void MoveWorkerProgress(object sender, ProgressChangedEventArgs e)
        {
            
        }

        private void MoveWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
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

     
        private   bool DatalogParserMethod(string fileLocation)
        {
            if (_moveWorker.IsBusy) return false;

            var fileNewPath = fileLocation.Replace('\\', '/');
           // ThreadHelper.ControlSetText(Tb_Message, "");
            if (_mesData.ManufacturingOrder == null)
            {
                EventLogUtil.LogEvent("Manufacturing Order not loaded!",EventLogEntryType.Warning);
                KryptonMessageBox.Show($"Manufacturing Order not loaded!", "File Watcher",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (_mesData.ResourceStatusDetails == null || (_mesData.ResourceStatusDetails?.Availability != "Up" && _mesData.ResourceStatusDetails?.ReasonCodeName?.Value!="Maintenance"))
            {
                EventLogUtil.LogEvent("Resource status is not in UP condition!", EventLogEntryType.Warning);
                KryptonMessageBox.Show($"Resource status is not in UP condition!", "File Watcher",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            // check if fail by maintenance Past Due
            var transPastDue = Mes.GetMaintenancePastDue(_mesData.MaintenanceStatusDetails);
            if (transPastDue.Result && transPastDue.Data != null)
            {
                EventLogUtil.LogEvent("This resource under maintenance, need to complete!", EventLogEntryType.Warning);
                KryptonMessageBox.Show(this, "This resource under maintenance, need to complete!", "Move In",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            var result =   DatalogFile.GetDatalogFileByFileName(fileNewPath);
            if (!result.Result) return false;
            var dataLogFile = (DatalogFile)result.Data;
            List<LaserPrintingProduct> list;
            try
            {
                list = DatalogFile.FileParse(ref dataLogFile);
            }
            catch
            {
                return false;
            }


            // MoveStart, MoveIn, Move
            var save =   DatalogFile.SaveDatalogFileHistory(dataLogFile);
            if (!save.Result)
            {
                return false;
            }

            if (_mesData.ResourceStatusDetails?.ReasonCodeName?.Value == "Maintenance") return true;

            _moveWorker.RunWorkerAsync(list);
            return true;
        }

        private   bool SetMfgOrder()
        {
            if (_watcher.Busy)
            {
                MessageBox.Show("Datalog Parsing In Progress");
                Bt_SetMfgOrder.Enabled = true;
                return false;
            }
            if (_mesData.ResourceStatusDetails == null ||_mesData.ResourceStatusDetails?.Availability != "Up" && _mesData.ResourceStatusDetails?.Reason?.Name!="Setting")
            {
                MessageBox.Show("Canceled, Resource is not in \"Up\" condition.");
                Bt_SetMfgOrder.Enabled = true;
                 return false;
            }
            var mfg =   Mes.GetMfgOrder(_mesData, Tb_MfgOrder.Text);
           

            if (mfg != null)
            {
                _mesData.SetManufacturingOrder(mfg);
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
                    var productChanges =   Mes.GetProduct(_mesData, _mesData.ManufacturingOrder.Product.Name);

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
                    var opecImage =   Mes.GetImage(_mesData, _mesData.ManufacturingOrder.Product.Name);
                    if (opecImage != null)
                    {
                        pbProduct.ImageLocation = opecImage.Identifier.ToString();
                    }
                }

                if (_mesUnitCounter != null)
                {
                      _mesUnitCounter.StopPoll();
                }
                _mesUnitCounter = MesUnitCounter.Load(MesUnitCounter.GetFileName(mfg.Name.Value));

             
                _mesUnitCounter.SetActiveMfgOrder(mfg.Name.Value);
              
                _mesUnitCounter.InitPoll(_mesData);
                _mesUnitCounter.StartPoll();
                MesUnitCounter.Save(_mesUnitCounter);

                Tb_LaserQty.Text = _mesUnitCounter.Counter.ToString();

                if (_currentPo.ContainerList.Count >= _currentPo.DummyQty && !_currentPo.PreparationFinished)
                {
                      Mes.SetResourceStatus(_mesData, "LS - Productive Time", "Quality Inspection");
                      GetStatusOfResource();
                }

                //var inRedis =   Mes.GetFinishGoodRecordFromCached(_mesData, _mesData.ManufacturingOrder?.Name.ToString());
                //if (inRedis == null)
                //{
                //    MessageBox.Show(@"Local Cache server error!");
                //    return true;
                //}

                //if (mfg.Containers == null)
                //{
                //    return true;
                //}
                //if (inRedis.Count != mfg.Containers.Length) 
                //{
                    ThreadHelper.Execute(async ()=> await Mes.GetFinishGoodRecordSyncWithServer(_mesData, _mesData.ManufacturingOrder?.Name.ToString()));
                //}

                return true;
            }

            MessageBox.Show(@"Manufacturing Order is not found!");

            return false;
        }
     

        private   string StartMoveInMove(LaserPrintingProduct product)
        {
            try
            {
                if (_mesData.ManufacturingOrder.Name == "" || product.Barcode == "")
                    return "Manufacturing Order And Container Name is Needed!";

                if (product.PrintedDateTime < _mesData.ProductionDateStart) return "Container date is older than production date!";
                if (product.PrintedDateTime > DateTime.Now) return "Container date is in the future!";

               
                ThreadHelper.ControlSetText(Tb_ArticleNumber,product.ArticleNumber);
                ThreadHelper.ControlSetText(Tb_SerialNumber, product.Barcode);
               
                ThreadHelper.ControlSetText(lbMoveOut, "");

                var resultStart =   Mes.ExecuteStart(_mesData, product.Barcode, (string)_mesData.ManufacturingOrder.Name, _mesData.ManufacturingOrder.Product.Name,_mesData.WorkFlow, Tb_MfgQty.Text);
                if (!resultStart.Result)
                {
                    var posAfterStart = Mes.GetCurrentContainerStep(_mesData, product.Barcode);
                    if (!posAfterStart.Contains("Laser"))
                    {
                        resultStart = Mes.ExecuteStart(_mesData, product.Barcode, (string)_mesData.ManufacturingOrder.Name, _mesData.ManufacturingOrder.Product.Name, _mesData.WorkFlow, Tb_MfgQty.Text);
                        if (!resultStart.Result)
                        { 
                            posAfterStart = Mes.GetCurrentContainerStep(_mesData, product.Barcode);
                            if (!posAfterStart.Contains("Laser"))
                            {
                                resultStart = Mes.ExecuteStart(_mesData, product.Barcode, (string)_mesData.ManufacturingOrder.Name, _mesData.ManufacturingOrder.Product.Name, _mesData.WorkFlow, Tb_MfgQty.Text);
                                if (!resultStart.Result)
                                {
                                    posAfterStart = Mes.GetCurrentContainerStep(_mesData, product.Barcode);
                                    if (!posAfterStart.Contains("Laser"))
                                    {
                                        return $"Container Start failed. {resultStart.Message}";
                                    }
                                }
                            }
                        }
                    }
                }

                var oContainerStatus =   Mes.GetCurrentContainerStep(_mesData, product.Barcode);
                  Mes.UpdateOrCreateFinishGoodRecordToCached(_mesData, _mesData.ManufacturingOrder.Name?.Value, product.Barcode, oContainerStatus);
                var dMoveIn = DateTime.Now;// product.PrintedDateTime.AddHours(_currentPo.TimeOffset);
                var transaction =   Mes.ExecuteMoveIn(_mesData, product.Barcode, dMoveIn);
                var resultMoveIn = transaction.Result || transaction.Message == "Move-in has already been performed for this operation.";
                if (!resultMoveIn)
                {
                    dMoveIn = DateTime.Now;
                    transaction =   Mes.ExecuteMoveIn(_mesData, product.Barcode, dMoveIn);
                    resultMoveIn = transaction.Result || transaction.Message == "Move-in has already been performed for this operation.";
                    if (!resultMoveIn)
                    {
                        dMoveIn = DateTime.Now;
                        transaction =   Mes.ExecuteMoveIn(_mesData, product.Barcode, dMoveIn);
                        resultMoveIn = transaction.Result || transaction.Message == "Move-in has already been performed for this operation.";
                    }
                }

                if (!resultMoveIn)
                {
                    return $"Container failed Move In. {transaction.Result}";
                }
                ThreadHelper.ControlSetText(lbMoveIn, dMoveIn.ToString(Mes.DateTimeStringFormat));
                //atributes
                var dMoveOut = DateTime.Now.AddHours(_currentPo.TimeOffset);
                var cDataPoint = product.LaserMarkingData.ToDataPointDetailsList().ToArray();
                var cAttributes = new ContainerAttrDetail[2];
                cAttributes[0] = new ContainerAttrDetail { Name = "LaserMoveOut", DataType = TrivialTypeEnum.String, AttributeValue = dMoveOut.ToString(Mes.DateTimeStringFormat), IsExpression = false };

                if (!_currentPo.PreparationFinished ||
                    _mesData.ResourceStatusDetails?.Reason?.Name == "Quality Inspection")
                {
                    _currentPo.ContainerList.Add(product.Barcode);
                    _currentPo.Save(_dataLocalPo);

                    cAttributes[1] = new ContainerAttrDetail
                    {
                        Name = MesContainerAttribute.LaserQualityMarkerInspect, DataType = TrivialTypeEnum.Integer,
                        AttributeValue = $"{_currentPo.ContainerList.Count}", IsExpression = false
                    };
                }
                var t = Mes.ExecuteContainerAttrMaint(_mesData, product.Barcode, cAttributes);
                //end attributes

                dMoveOut=DateTime.Now;
                var resultMoveStd =   Mes.ExecuteMoveStandard(_mesData, product.Barcode, dMoveOut, cDataPoint);
                if (!resultMoveStd.Result)
                {
                    var posAfterMoveStd = Mes.GetCurrentContainerStep(_mesData, product.Barcode);
                    resultMoveStd.Result |= !posAfterMoveStd.Contains("Laser");
                    if (!resultMoveStd.Result)
                    {
                        dMoveOut = DateTime.Now;
                        resultMoveStd = Mes.ExecuteMoveStandard(_mesData, product.Barcode, dMoveOut, cDataPoint);
                        if (!resultMoveStd.Result)
                        {
                            posAfterMoveStd = Mes.GetCurrentContainerStep(_mesData, product.Barcode);
                            resultMoveStd.Result |= !posAfterMoveStd.Contains("Laser");
                            if (!resultMoveStd.Result)
                            {
                                dMoveOut = DateTime.Now;
                                resultMoveStd =
                                    Mes.ExecuteMoveStandard(_mesData, product.Barcode, dMoveOut, cDataPoint);
                                if (!resultMoveStd.Result)
                                {
                                    posAfterMoveStd = Mes.GetCurrentContainerStep(_mesData, product.Barcode);
                                    resultMoveStd.Result |= !posAfterMoveStd.Contains("Laser");
                                }
                            }
                        }
                    }
                }

                if (resultMoveStd.Result)
                {
                    ThreadHelper.ControlSetText(lbMoveOut, dMoveOut.ToString(Mes.DateTimeStringFormat));

                   
                     
                        if (_currentPo.ContainerList.Count >= _currentPo.DummyQty && _mesData?.ResourceStatusDetails?.Reason?.Name!= "Quality Inspection")
                        {
                              Mes.SetResourceStatus(_mesData, "LS - Productive Time", "Quality Inspection");
                              GetStatusOfResource();
                        }
                    

                  
                    oContainerStatus =   Mes.GetCurrentContainerStep(_mesData, product.Barcode);
                      Mes.UpdateOrCreateFinishGoodRecordToCached(_mesData, _mesData.ManufacturingOrder.Name?.Value, product.Barcode, oContainerStatus);

                    _mesUnitCounter.UpdateCounter(product.Barcode);
                    MesUnitCounter.Save(_mesUnitCounter);

                    ThreadHelper.ControlSetText(Tb_LaserQty, _mesUnitCounter.Counter.ToString());
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
        private void GetStatusOfResource()
        {
            try
            {
                var resourceStatus =   Mes.GetResourceStatusDetails(_mesData);
                if (resourceStatus != null)
                {
                    _mesData.SetResourceStatusDetails(resourceStatus);
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
        private void GetStatusOfResourceDetail()
        {
            try
            {
                var resourceStatus =   Mes.GetResourceStatusDetails(_mesData);
                if (resourceStatus != null)
                {
                    _mesData.SetResourceStatusDetails(resourceStatus);
                    if (_mesData.ResourceStatusDetails?.Reason?.Name != "Quality Inspection" && _mesData.ManufacturingOrder != null)
                    {
                        btnFinishPreparation.Enabled = true;
                        btnStartPreparation.Enabled = false;
                    }
                    if (resourceStatus.Status != null) Cb_StatusCode.Text = resourceStatus.Status.Name;
                      Task.Delay(1000);
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
        private void GetStatusMaintenanceDetails()
        {
            try
            {
                var maintenanceStatusDetails =   Mes.GetMaintenanceStatusDetails(_mesData);
                _mesData.SetMaintenanceStatusDetails(maintenanceStatusDetails);
                if (maintenanceStatusDetails != null)
                {
                    getMaintenanceStatusDetailsBindingSource.DataSource =
                        new BindingList<GetMaintenanceStatusDetails>(maintenanceStatusDetails);
                    Dg_Maintenance.DataSource = getMaintenanceStatusDetailsBindingSource;
                    //get past due, warning, and tolerance
                    var pastDue = maintenanceStatusDetails.Where(x => x.MaintenanceState == "Past Due").ToList();
                    var due = maintenanceStatusDetails.Where(x => x.MaintenanceState == "Due").ToList();
                    var pending = maintenanceStatusDetails.Where(x => x.MaintenanceState == "Pending").ToList();

                    if (pastDue.Count > 0)
                    {
                        lblResMaintMesg.Text = @"Resource Maintenance Past Due";
                        lblResMaintMesg.BackColor = Color.Red;
                        lblResMaintMesg.Visible = true;
                        if (_mesData?.ResourceStatusDetails?.Reason?.Name != "Planned Maintenance")
                        {
                              Mes.SetResourceStatus(_mesData, "LS - Planned Downtime", "Planned Maintenance");
                        }
                        return;
                    }
                    if (due.Count > 0)
                    {
                        lblResMaintMesg.Text = @"Resource Maintenance Due";
                        lblResMaintMesg.BackColor = Color.Orange;
                        lblResMaintMesg.Visible = true;
                        return;
                    }
                    if (pending.Count > 0)
                    {
                        lblResMaintMesg.Text = @"Resource Maintenance Pending";
                        lblResMaintMesg.BackColor = Color.Yellow;
                        lblResMaintMesg.Visible = true;
                        return;
                    }
                }
                lblResMaintMesg.Visible = false;
                lblResMaintMesg.Text = "";
                getMaintenanceStatusDetailsBindingSource.DataSource = null;
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }
        private   void GetResourceStatusCodeList()
        {
            try
            {
                var oStatusCodeList =   Mes.GetListResourceStatusCode(_mesData);
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
            pbProduct.ImageLocation = "";
        }

        private   void SetProductionState(ProductionState currentProductionState)
        {
            switch (currentProductionState)
            {
                case ProductionState.Idle:
                    ClearTextBox();
                    btnStartPreparation.Enabled = true;
                   
                    btnFinishPreparation.Enabled = false;
                    Bt_SetMfgOrder.Enabled = false;
                    Tb_MfgOrder.Enabled = false;
                    break;
                case ProductionState.PreparationStarted:
                    ClearTextBox();
                    _mesData.SetManufacturingOrder(null);
                    btnStartPreparation.Enabled = false;
                  
                    btnFinishPreparation.Enabled = false;
                    Bt_SetMfgOrder.Enabled = false;
                    Tb_MfgOrder.Enabled = true;
                    ActiveControl = Tb_MfgOrder;
                    break;
                case ProductionState.ManufacturingOrderSet:
                   
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
                   
                    btnFinishPreparation.Enabled = false;
                    Bt_SetMfgOrder.Enabled = false;
                    Tb_MfgOrder.Enabled = false;
                    break;
                case ProductionState.ProductionEnd:
                    ClearTextBox();
                      Mes.SetResourceStatus(_mesData, "", "");
                    btnStartPreparation.Enabled = true;
                 
                    btnFinishPreparation.Enabled = false;
                    Bt_SetMfgOrder.Enabled = false;
                    Tb_MfgOrder.Enabled = false;
                    break;
            }
        }
        private   void TimerRealtime_Tick(object sender, EventArgs e)
        {
            TimerRealtime.Stop();


              GetStatusOfResource();
              GetStatusMaintenanceDetails();
            TimerRealtime.Start();
        }

        private void kryptonTextBox5_TextChanged(object sender, EventArgs e)
        {

        }

        private  void MainAuto24_Load(object sender, EventArgs e)
        {
        }

        private   void btnStartPreparation_Click(object sender, EventArgs e)
        {
            if (_mesData.ResourceStatusDetails == null)
            {
                KryptonMessageBox.Show("Resource Status not yet retrieved", "Start Preparation", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (_mesData.ResourceStatusDetails?.Reason?.Name == "Planned Maintenance")
            {
                KryptonMessageBox.Show("Resource Status under Maintenance", "Start Preparation", MessageBoxButtons.OK,
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
              Mes.SetResourceStatus(_mesData, "LS - Planned Downtime", "Setting");
              GetStatusOfResource();
        }

        private   void Bt_SetMfgOrder_Click(object sender, EventArgs e)
        {
            Bt_SetMfgOrder.Enabled = false;
            var data = Tb_MfgOrder.Text.Trim();
            if (data.Length < 7)
            {
                Bt_SetMfgOrder.Enabled = true;
                return;
            }

            if (_mesData.ResourceStatusDetails?.Reason?.Name == "Maintenance")
            {
                Bt_SetMfgOrder.Enabled = true;
                return;
            }

            lblLoadingPo.Visible = true;
            var result =   SetMfgOrder();
            lblLoadingPo.Visible = false;
            if (!result)
            {
                Bt_SetMfgOrder.Enabled = true;
                return;
            }
            _mesData.SetProductionDateStart(DateTime.Now);
            if (_currentPo.ContainerList.Count < _currentPo.DummyQty )
            {
                  Mes.SetResourceStatus(_mesData, "LS - Standby Time", "Ready");
            }
            else
            {
                if (_currentPo.PreparationFinished)
                {
                      Mes.SetResourceStatus(_mesData, "LS - Productive Time", "Pass");
                }
                else
                {
                      Mes.SetResourceStatus(_mesData, "LS - Productive Time", "Quality Inspection");
                }
            }
            
              GetStatusOfResource();

           
            SetProductionState(ProductionState.ManufacturingOrderSet);
        }

        private   void btnFinishPreparation_Click(object sender, EventArgs e)
        {
            if (_currentPo != null)
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

                if (_mesData.ResourceStatusDetails?.Reason?.Name == "Planned Maintenance")
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

                    if (ss.UserDetails.UserRole != UserRole.Quality) return;
                }

                _currentPo.PreparationFinished = true;
                _currentPo.Save(_dataLocalPo);

                  Mes.SetResourceStatus(_mesData, "LS - Productive Time", "Pass");
                  GetStatusOfResource();
                SetProductionState(ProductionState.PreparationFinished);
                return;
            }
              Mes.SetResourceStatus(_mesData, "LS - Productive Time", "Pass");
              GetStatusOfResource();
            SetProductionState(ProductionState.Idle);
        }

       
        private void Tb_MfgOrder_KeyUp(object sender, KeyEventArgs e)
        {
            Bt_SetMfgOrder.Enabled = Tb_MfgOrder.Text.Length >= 7;
           
        }

        private   void btnCallMaintenance_Click(object sender, EventArgs e)
        {
            var dlg = MessageBox.Show(@"Are you sure want to call maintenance?", @"Call Maintenance",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dlg == DialogResult.No)
            {
                return;
            }
              Mes.SetResourceStatus(_mesData, "LS - Internal Downtime", "Maintenance");
              GetStatusOfResource();
        }

        private   void Cb_StatusCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                var oStatusCode =   Mes.GetResourceStatusCode(_mesData, Cb_StatusCode.SelectedValue != null ? Cb_StatusCode.SelectedValue.ToString() : "");
                if (oStatusCode != null)
                {
                    Tb_StatusCodeM.Text = oStatusCode.Availability.ToString();
                    if (oStatusCode.ResourceStatusReasons != null)
                    {
                        var oStatusReason =   Mes.GetResourceStatusReasonGroup(_mesData, oStatusCode.ResourceStatusReasons.Name);
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

        private   void btnSetMachineStatus_Click(object sender, EventArgs e)
        {
            try
            {
                var result = false;
                if (Cb_StatusCode.Text != "" && Cb_StatusReason.Text != "")
                {
                    result =   Mes.SetResourceStatus(_mesData, Cb_StatusCode.Text, Cb_StatusReason.Text);
                }
                else if (Cb_StatusCode.Text != "")
                {
                    result =   Mes.SetResourceStatus(_mesData, Cb_StatusCode.Text, "");
                }

                GetStatusOfResourceDetail();
                KryptonMessageBox.Show(result ? "Setup status successful" : "Setup status failed");

            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }

        private   void kryptonNavigator1_SelectedPageChanged(object sender, EventArgs e)
        {
            if (kryptonNavigator1.SelectedIndex == 1)
            {
                  GetStatusOfResourceDetail();
            }
            //Serial Number of PO:
            if (kryptonNavigator1.SelectedIndex == 3)
            {
                lblPo.Text = $@"Serial Number of PO: {_mesData.ManufacturingOrder?.Name}";
                lblLoading.Visible = true;
                GetFinishedGoodRecord();
                if (!_syncWorker.IsBusy)lblLoading.Visible = false;
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

        private   async Task GetFinishedGoodRecord()
        {
            if (_mesData == null) return;
            if (_mesData.ManufacturingOrder==null)return;

            var data =   await Mes.GetFinishGoodRecordFromCached(_mesData, _mesData.ManufacturingOrder?.Name.ToString());
            if (data == null)
            {
                var temp =   await Mes.GetFinishGoodRecordSyncWithServer(_mesData, _mesData.ManufacturingOrder?.Name.ToString());
                data = temp.ToList();
            }

            var list =   Mes.IFinishGoodRecordToFinishedGoodLaser(data.ToArray());
            _bindingList = new BindingList<FinishedGoodLaser>(list);
            bindingSource1.DataSource = _bindingList;
            kryptonDataGridView2.DataSource = bindingSource1;
            Tb_FinishedGoodCounter.Text = list.Length.ToString();

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

        private void button1_Click(object sender, EventArgs e)
        {
            
        }
        private   void  Closing()
        {
            if (_mesUnitCounter != null)
            {
                  _mesUnitCounter.StopPoll();
            }
            if (!_allowClose)
            {
                using (var ss = new LoginForm24())
                {
                    var dlg = ss.ShowDialog(this);
                    if (dlg == DialogResult.Abort)
                    {
                        KryptonMessageBox.Show("Login Failed");
                        _allowClose = false;
                        return;
                    }
                    if (dlg == DialogResult.Cancel)
                    {
                        _allowClose = false;
                        return;
                    }
                }
            }
            _allowClose = true;
            Close();
        }
        private   void MainAuto24_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_allowClose)
            {
                e.Cancel = false;
                return;
            }
            e.Cancel = true;
               Closing();
        }

        private void kryptonPanel1_Paint(object sender, PaintEventArgs e)
        {

        }
        private void SyncWorkerProgress(object sender, ProgressChangedEventArgs e)
        {

        }

        private void SyncWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var data = (List<IFinishGoodRecord>) e.Result;
            var list = Mes.IFinishGoodRecordToFinishedGoodLaser(data.ToArray());
            _bindingList = new BindingList<FinishedGoodLaser>(list);
            bindingSource1.DataSource = _bindingList;
            kryptonDataGridView2.DataSource = bindingSource1;
            Tb_FinishedGoodCounter.Text = list.Length.ToString();
            lblLoading.Visible = false;
        }
        private void SyncDoWork(object sender, DoWorkEventArgs e)
        {
            var temp =  Mes.GetFinishGoodRecordSyncWithServer(_mesData, _mesData.ManufacturingOrder?.Name.ToString()).Result;
            var data =  Mes.GetFinishGoodRecordFromCached(_mesData, _mesData.ManufacturingOrder?.Name.ToString()).Result;
            e.Result = data;
        }

        private void btnSynchronized_Click(object sender, EventArgs e)
        {
            if (_syncWorker.IsBusy) return;
            if (_mesData == null) return;
            if (_mesData.ManufacturingOrder == null) return;
            lblLoading.Visible = true;
            _syncWorker.RunWorkerAsync();

        }

        private   void tmrFirstLoad_Tick(object sender, EventArgs e)
        {
            tmrFirstLoad.Stop();
              GetStatusOfResource();
              GetResourceStatusCodeList();
              GetStatusMaintenanceDetails();
        }

        private void pbProduct_Click(object sender, EventArgs e)
        {

        }

        private void kryptonDataGridView2_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (_bindingList == null) return;
            kryptonDataGridView2.DataSource = _sortAscending ? _bindingList.OrderBy(kryptonDataGridView2.Columns[e.ColumnIndex].DataPropertyName).ToList() : _bindingList.OrderBy(kryptonDataGridView2.Columns[e.ColumnIndex].DataPropertyName).Reverse().ToList();
            _sortAscending = !_sortAscending;
        }
    }
}
