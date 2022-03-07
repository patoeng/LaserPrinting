using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ComponentFactory.Krypton.Toolkit;
using LaserPrinting.Model;
using LaserPrinting.Helpers;
using LaserPrinting.Services;
using OpcenterWikLibrary;
using Camstar.WCF.Services;
using Camstar.WCF.ObjectStack;
using System.Reflection;
using System.Threading;


namespace LaserPrinting
{
    public partial class MainAuto : KryptonForm
    {
        #region Private fields
        private static GetMaintenanceStatusDetails[] oMaintenanceStatus = null;
        private static ProductChanges oProductChanges = null;
        private static MfgOrderChanges oMfgOrderChanges = null;
        private ServiceUtil oServiceUtil = new ServiceUtil();

        private delegate void DgDataSourceDelegate(KryptonDataGridView kdg, BindingList<LaserPrintingProduct> list);

        #endregion
        public MainAuto()
        {
            InitializeComponent();
            InitLaserPrinting();
            InitFileWatcher();
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
           

            GetStatusOfResource();
            GetStatusMaintenanceDetails();
            ContainerOfMfgOrder();


            Text = OpecLibrary.AddVersionNumber(Text);
            lbResourceName.Text = AppSettings.Resource;
        }
        private void InitFileWatcher()
        {
            var setting = new Properties.Settings();
            var watcher = new DatalogFileWatcher(setting.LaserDatalogLocation, setting.LaserDatalogPattern, DatalogParserMethod);
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
        private bool DatalogParserMethod(string FileLocation)
        {
            var dataLogFile = DatalogFile.GetDatalogFileByFileName("DatalogFile.db", FileLocation);
            var list = DatalogFile.FileParse(ref dataLogFile);

            if (oMfgOrderChanges == null)
            {
                return true; 
            }

            SetDgDataSource(kryptonDataGridView1, new BindingList<LaserPrintingProduct>(list));
            

            // MoveStart, MoveIn, Move
            DatalogFile.SaveDatalogFileHistory("DatalogFile.db", dataLogFile);

            foreach(var sn in list)
            {
                StartMoveInMove(sn);
            }
           
            return true;
        }
        private void GetStatusOfResource()
        {
            BackgroundWorker bw = new BackgroundWorker();
            ResourceStatusDetails oResourceStatusDetails = null;
           bw.DoWork += delegate
            {
                try
                {
                    oResourceStatusDetails = OpecLibrary.GetStatusOfResource(AppSettings.Resource, oServiceUtil);

                }
                catch (Exception ex)
                {
                    ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                    EventLogUtil.LogErrorEvent(ex.Source, ex);
                }

            };

            bw.RunWorkerCompleted += delegate
            {
                if (oResourceStatusDetails != null)
                {
                    if (oResourceStatusDetails.Status != null) ThreadHelper.ControlSetText(Tb_StatusCode, oResourceStatusDetails.Status.Name);
                    if (oResourceStatusDetails.Reason != null) ThreadHelper.ControlSetText(Tb_StatusReason, oResourceStatusDetails.Reason.Name);
                    if (oResourceStatusDetails.Availability != null)
                    {
                        ThreadHelper.ControlSetText(Tb_Availability, oResourceStatusDetails.Availability.Value);
                        if (oResourceStatusDetails.Availability.Value == "Up")
                        {
                            ThreadHelper.ControlSetBgColor(Pb_IndicatorPicture, Color.Green);
                        }
                        else if (oResourceStatusDetails.Availability.Value == "Down")
                        {
                            ThreadHelper.ControlSetBgColor(Pb_IndicatorPicture, Color.Red);
                        }
                    }
                    else
                    {
                        ThreadHelper.ControlSetBgColor(Pb_IndicatorPicture, Color.Orange);
                    }

                    if (oResourceStatusDetails.TimeAtStatus != null)
                        ThreadHelper.ControlSetText(Tb_TimeAtStatus,
                            DateTime.FromOADate(oResourceStatusDetails.TimeAtStatus.Value).ToString("s"));
                }
            };

            bw.RunWorkerAsync();
          
        }
        private void ContainerOfMfgOrder()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += delegate
            {
                try
                {
                    if (Tb_MfgOrder.Text != "")
                    {
                        oMfgOrderChanges = OpecLibrary.ContainerOfMfgOrder(Tb_MfgOrder.Text, oServiceUtil);
                       
                    }
                }
                catch (Exception ex)
                {
                    ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod().Name : MethodBase.GetCurrentMethod().Name + "." + ex.Source;
                    EventLogUtil.LogErrorEvent(ex.Source, ex);
                }

            };

            bw.RunWorkerCompleted += delegate
            {
                Lb_ContainerList.Items.Clear();
                if (oMfgOrderChanges != null)
                {
                    if (oMfgOrderChanges.Name != null) MfgContainerLabel.Text = $"List Container of {oMfgOrderChanges.Name}";
                    if (oMfgOrderChanges.Containers != null)
                    {
                        if (oMfgOrderChanges.Containers.Length > 0)
                        {
                            //var oListOfContainer = oMfgOrderChanges.Containers.OrderBy(x => x.Value.ToString()).ToList();
                            foreach (var container in oMfgOrderChanges.Containers)
                            {
                                Lb_ContainerList.Items.Add(container.Value);
                            }
                        }

                        Tb_MfgInProcess.Text =  Lb_ContainerList.Items.Count.ToString();
                    }
                }
            };

            bw.RunWorkerAsync();
        }
        private void GetMfgOrderMustBeDoing()
        {
            try
            {
                MfgOrderChanges oMfgOrderChanges = oServiceUtil.GetMfgOrderDispatch();
                if (oMfgOrderChanges != null)
                {
                    ClearTextBox();
                    if (oMfgOrderChanges.Name.ToString() != "") Tb_MfgOrder.Text = oMfgOrderChanges.Name.ToString();
                    if (oMfgOrderChanges.Product != null)
                    {
                        Tb_MfgProduct.Text = oMfgOrderChanges.Product.Name;
                        oProductChanges = oServiceUtil.GetProduct(oMfgOrderChanges.Product.Name);
                        if (oProductChanges != null) Tb_MfgProductDescription.Text = Convert.ToString(oProductChanges.Description);
                    }
                    if (oMfgOrderChanges.isWorkflow != null) Tb_MfgWorkflow.Text = oMfgOrderChanges.isWorkflow.Name;
                    if (oMfgOrderChanges.UOM != null) Tb_MfgUOM.Text = oMfgOrderChanges.UOM.Name;
                    if (oMfgOrderChanges.sswQtyStarted != null) Tb_MfgInProcess.Text = Convert.ToString(oMfgOrderChanges.sswQtyStarted);
                    if (oMfgOrderChanges.Qty != null) Tb_MfgQty.Text = Convert.ToString(oMfgOrderChanges.Qty);
                    if (Convert.ToString(oMfgOrderChanges.PlannedStartDate) != "") Tb_MfgStartedDate.Text = Convert.ToString(oMfgOrderChanges.PlannedStartDate);
                    if (Convert.ToString(oMfgOrderChanges.PlannedCompletionDate) != "") Tb_MfgEndDate.Text = Convert.ToString(oMfgOrderChanges.PlannedCompletionDate);
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod().Name : MethodBase.GetCurrentMethod().Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }
        private void StartMoveInMove(LaserPrintingProduct laserPrintingProduct)
        {
            try
            {
                bool resultStart = false;
                bool resultMoveIn = false;
                if (Tb_MfgOrder.Text != "" && laserPrintingProduct.Barcode != "")
                {
                    resultStart = oServiceUtil.ExecuteStart(laserPrintingProduct.Barcode, Tb_MfgOrder.Text, Tb_MfgProduct.Text, "", Tb_MfgWorkflow.Text, "", "Unit", "Production", "Normal", "", 1, Tb_MfgUOM.Text, "", "", Convert.ToString(laserPrintingProduct.PrintedStartDateTime));
                    resultMoveIn = oServiceUtil.ExecuteMoveIn(laserPrintingProduct.Barcode, AppSettings.Resource, "", "", null, "", false, false, "", "", Convert.ToString(laserPrintingProduct.PrintedStartDateTime));
                    if (resultStart && resultMoveIn)
                    {
                        
                        bool resultMoveStd = oServiceUtil.ExecuteMoveStd(laserPrintingProduct.Barcode, "", AppSettings.Resource, "", "", null, "", false, "", "", Convert.ToString(DateTime.Now));
                        if (resultMoveStd)
                        {
                           // ContainerOfMfgOrder();
                            //MessageBox.Show("Container success to Started, MoveIn and Moved Std");
                        }
                        else
                        {
                           // MessageBox.Show("Container success to Started and MoveIn but failed to Moved Std");
                        }
                    }
                    else
                    {
                       // MessageBox.Show("Container failed to Started and Move In");
                    }
                }
                else
                {
                   // MessageBox.Show("Manufacturing Order or Container Name is Needed!");
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod().Name : MethodBase.GetCurrentMethod().Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }
        private void kryptonButton1_Click(object sender, EventArgs e)
        {
            var dtl = new DatalogFile
            {
                Id = Guid.NewGuid(),
                FileName = @"C:\Users\Precision\Downloads\Laser marking\HL_20211014.txt"
            };

            var list = DatalogFile.FileParse(ref dtl);
        }

        private void RealtimeMfg_Click(object sender, EventArgs e)
        {
            if (RealtimeMfg.Checked == true)
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

        private void TimerRealtime_Tick(object sender, EventArgs e)
        {
            TimerRealtime.Stop();

           
                GetStatusOfResource();
                GetStatusMaintenanceDetails();
                ContainerOfMfgOrder();
                if (RealtimeMfg.Checked == true)
                {
                    //  GetMfgOrderMustBeDoing();
                }

                TimerRealtime.Start();
        }

        private void GetStatusMaintenanceDetails()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += delegate
            {
                try
                {
                    oMaintenanceStatus = oServiceUtil.GetGetMaintenanceStatus(AppSettings.Resource);
                }
                catch (Exception ex)
                {
                    ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod().Name : MethodBase.GetCurrentMethod().Name + "." + ex.Source;
                    EventLogUtil.LogErrorEvent(ex.Source, ex);
                }

            };

            bw.RunWorkerCompleted += delegate
            {
                if (oMaintenanceStatus != null)
                {
                    Dg_Maintenance.DataSource = oMaintenanceStatus;
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


            };
            bw.RunWorkerAsync();
            
           
        }

        private void Bt_SetMfgOrder_Click(object sender, EventArgs e)
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += delegate
            {
                try
                {
                    if (Tb_MfgOrder.Text != "")
                    {
                        oMfgOrderChanges = oServiceUtil.GetMfgOrder(Tb_MfgOrder.Text);
                    }
                }
                catch (Exception ex)
                {
                    ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                    EventLogUtil.LogErrorEvent(ex.Source, ex);
                }

            };

            bw.RunWorkerCompleted += delegate
            {
                if (oMfgOrderChanges != null)
                {
                    ClearTextBox();
                    if (oMfgOrderChanges.Name.ToString() != "") Tb_MfgOrder.Text = oMfgOrderChanges.Name.ToString();
                    if (oMfgOrderChanges.Product != null)
                    {
                        Tb_MfgProduct.Text = oMfgOrderChanges.Product.Name;
                        oProductChanges = oServiceUtil.GetProduct(oMfgOrderChanges.Product.Name);
                        if (oProductChanges != null) Tb_MfgProductDescription.Text = Convert.ToString(oProductChanges.Description);
                    }
                    if (oMfgOrderChanges.isWorkflow != null) Tb_MfgWorkflow.Text = oMfgOrderChanges.isWorkflow.Name;
                    if (oMfgOrderChanges.UOM != null) Tb_MfgUOM.Text = oMfgOrderChanges.UOM.Name;
                    if (oMfgOrderChanges.sswQtyStarted != null) Tb_MfgInProcess.Text = Convert.ToString(oMfgOrderChanges.sswQtyStarted);
                    if (oMfgOrderChanges.Qty != null) Tb_MfgQty.Text = Convert.ToString(oMfgOrderChanges.Qty);
                    if (Convert.ToString(oMfgOrderChanges.PlannedStartDate) != "") Tb_MfgStartedDate.Text = Convert.ToString(oMfgOrderChanges.PlannedStartDate);
                    if (Convert.ToString(oMfgOrderChanges.PlannedCompletionDate) != "") Tb_MfgEndDate.Text = Convert.ToString(oMfgOrderChanges.PlannedCompletionDate);
                    ContainerOfMfgOrder();
                }
                else
                {
                    MessageBox.Show("Manufacturing Order is not found!");
                }
            };

            bw.RunWorkerAsync();
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

        private void Tb_MfgOrder_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode==Keys.Enter) Bt_SetMfgOrder_Click(this,null);
        }
    }
}
