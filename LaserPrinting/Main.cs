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
using Camstar.WCF.Services;
using Camstar.WCF.ObjectStack;
using System.Configuration;
using System.Reflection;
using Squirrel;
using System.Diagnostics;
using OpcenterWikLibrary;

namespace LaserPrinting
{
    public partial class Main : KryptonForm
    {
        #region CONSTRUCTOR
        public Main()
        {
            InitializeComponent();

            Rectangle r = new Rectangle(0, 0, Pb_IndicatorPicture.Width, Pb_IndicatorPicture.Height);
            System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();
            int d = 28;
            gp.AddArc(r.X, r.Y, d, d, 180, 90);
            gp.AddArc(r.X + r.Width - d, r.Y, d, d, 270, 90);
            gp.AddArc(r.X + r.Width - d, r.Y + r.Height - d, d, d, 0, 90);
            gp.AddArc(r.X, r.Y + r.Height - d, d, d, 90, 90);
            Pb_IndicatorPicture.Region = new Region(gp);
            this.WindowState = FormWindowState.Normal;
            this.Size = new Size(1000, 731);

            MyTitle.Text = $"Laser Printing - {AppSettings.Resource}";
            ResourceGrouping.Values.Heading = $"Resource Status: {AppSettings.Resource}";
            ResourceSetupGrouping.Values.Heading = $"Resource Setup: {AppSettings.Resource}";
            GetResourceStatusCodeList();
            GetStatusOfResource();
            GetStatusMaintenanceDetails();
            ContainerOfMfgOrder();
            Cb_StatusCode.SelectedItem = null;
            Cb_StatusReason.SelectedItem = null;
            Tb_SetupAvailability.Text = "";
            if (RealtimeMfg.Checked == true)
            {
                //GetMfgOrderMustBeDoing();
            }
            AddVersionNumber();
        }
        #endregion

        #region INSTANCE VARIABLE
        private static GetMaintenanceStatusDetails[] oMaintenanceStatus = null;
        private static ProductChanges oProductChanges = null;
        private static MfgOrderChanges oMfgOrderChanges = null;
        private  ServiceUtil oServiceUtil = new ServiceUtil();
        #endregion

        #region FUNCTION USEFULL
        private void AddVersionNumber()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

            this.Text += $" V.{versionInfo.FileVersion}";
        }
        private void GetResourceStatusCodeList()
        {
            NamedObjectRef[] oStatusCodeList = oServiceUtil.GetListResourceStatusCode();
            if (oStatusCodeList != null)
            {
                Cb_StatusCode.DataSource = oStatusCodeList;
            }
        }
        private void ContainerOfMfgOrder()
        {
            try
            {
                Lb_ContainerList.Items.Clear();
                if (Tb_MfgOrder.Text != "")
                {
                    oMfgOrderChanges = oServiceUtil.GetMfgOrder(Tb_MfgOrder.Text);
                    if (oMfgOrderChanges != null)
                    {
                        if (oMfgOrderChanges.Name != null) MfgContainerLabel.Text = $@"List Container of {oMfgOrderChanges.Name}";
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

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod().Name : MethodBase.GetCurrentMethod().Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
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
        private void GetStatusMaintenanceDetails()
        {
            try
            {
                oMaintenanceStatus = oServiceUtil.GetGetMaintenanceStatus(AppSettings.Resource);
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
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod().Name : MethodBase.GetCurrentMethod().Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }
        private void GetStatusOfResource()
        {
            try
            {
                ResourceStatusDetails oResourceStatusDetails = oServiceUtil.GetResourceStatusDetails(AppSettings.Resource);
                if (oResourceStatusDetails != null)
                {
                    if (oResourceStatusDetails.Status != null) Tb_StatusCode.Text = oResourceStatusDetails.Status.Name;
                    if (oResourceStatusDetails.Reason != null) Tb_StatusReason.Text = oResourceStatusDetails.Reason.Name;
                    if (oResourceStatusDetails.Availability != null)
                    {
                        Tb_Availability.Text = oResourceStatusDetails.Availability.Value;
                        if (oResourceStatusDetails.Availability.Value == "Up")
                        {
                            Pb_IndicatorPicture.BackColor = Color.Green;
                        }
                        else if (oResourceStatusDetails.Availability.Value == "Down")
                        {
                            Pb_IndicatorPicture.BackColor = Color.Red;
                        }
                    }
                    else
                    {
                        Pb_IndicatorPicture.BackColor = Color.Orange;
                    }
                    if (oResourceStatusDetails.TimeAtStatus != null) Tb_TimeAtStatus.Text = Convert.ToString(oResourceStatusDetails.TimeAtStatus.Value);
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod().Name : MethodBase.GetCurrentMethod().Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
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
        #endregion

        #region COMPONENT EVENT
        private void Bt_SetMfgOrder_Click(object sender, EventArgs e)
        {
            try
            {
                oMfgOrderChanges = oServiceUtil.GetMfgOrder(Tb_MfgOrder.Text);
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
                } else
                {
                    MessageBox.Show("Manufacturing Order is not found!");
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod().Name : MethodBase.GetCurrentMethod().Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }
        private void Bt_StartMove_Click(object sender, EventArgs e)
        {
            try
            {
                bool resultStart = false;
                bool resultMoveIn = false;
                if (Tb_MfgOrder.Text != "" && Tb_SerialNumber.Text != "")
                {
                    resultStart = oServiceUtil.ExecuteStart(Tb_SerialNumber.Text, Tb_MfgOrder.Text, Tb_MfgProduct.Text, "", Tb_MfgWorkflow.Text, "", "Unit", "Production", "Normal", "", 1, Tb_MfgUOM.Text, "", "", Convert.ToString(Dt_CycleTime.Value));
                    resultMoveIn = oServiceUtil.ExecuteMoveIn(Tb_SerialNumber.Text, AppSettings.Resource, "", "", null, "",false, false, "", "", Convert.ToString(Dt_CycleTime.Value));
                    if (resultStart && resultMoveIn)
                    {
                        Dt_MoveOut.Value = DateTime.Now;
                        bool resultMoveStd = oServiceUtil.ExecuteMoveStd(Tb_SerialNumber.Text, "", AppSettings.Resource, "", "", null, "", false, "", "", Convert.ToString(DateTime.Now));
                        if (resultMoveStd)
                        {
                            ContainerOfMfgOrder();
                            MessageBox.Show("Container success to Started, MoveIn and Moved Std");
                        }
                        else
                        {
                            MessageBox.Show("Container success to Started and MoveIn but failed to Moved Std");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Container failed to Started and Move In");
                    }
                } else
                {
                    MessageBox.Show("Manufacturing Order or Container Name is Needed!");
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod().Name : MethodBase.GetCurrentMethod().Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }

        }
        private void Cb_StatusCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                ResourceStatusCodeChanges oStatusCode = oServiceUtil.GetResourceStatusCode(Cb_StatusCode.SelectedValue != null ? Cb_StatusCode.SelectedValue.ToString() : "");
                if (oStatusCode != null)
                {
                    Tb_SetupAvailability.Text = oStatusCode.Availability.ToString();
                    if (oStatusCode.ResourceStatusReasons != null)
                    {
                        ResStatusReasonGroupChanges oStatusReason = oServiceUtil.GetResourceStatusReasonGroup(oStatusCode.ResourceStatusReasons.Name);
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
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod().Name : MethodBase.GetCurrentMethod().Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }
        private void Bt_SetResourceStatus_Click(object sender, EventArgs e)
        {
            try
            {
                if (Cb_StatusCode.Text != "" && Cb_StatusReason.Text != "")
                {
                    oServiceUtil.ExecuteResourceSetup(AppSettings.Resource, Cb_StatusCode.Text, Cb_StatusReason.Text);
                } else if (Cb_StatusCode.Text != "")
                {
                    oServiceUtil.ExecuteResourceSetup(AppSettings.Resource, Cb_StatusCode.Text, "");
                }
                GetStatusOfResource();
                GetStatusMaintenanceDetails();
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod().Name : MethodBase.GetCurrentMethod().Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }
        private void Dg_Maintenance_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                foreach (DataGridViewRow row in Dg_Maintenance.Rows)
                {
                    //Console.WriteLine(Convert.ToString(row.Cells["MaintenanceState"].Value));
                    if (Convert.ToString(row.Cells["MaintenanceState"].Value) == "Pending")
                    {
                        row.DefaultCellStyle.BackColor = Color.Yellow;
                    } else if (Convert.ToString(row.Cells["MaintenanceState"].Value) == "Due")
                    {
                        row.DefaultCellStyle.BackColor = Color.Orange;
                    } else if (Convert.ToString(row.Cells["MaintenanceState"].Value) == "Past Due")
                    {
                        row.DefaultCellStyle.BackColor = Color.Red;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod().Name : MethodBase.GetCurrentMethod().Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }
        private void TimerRealtime_Tick(object sender, EventArgs e)
        {
            GetStatusOfResource();
            GetStatusMaintenanceDetails();
            ContainerOfMfgOrder();
            if (RealtimeMfg.Checked == true)
            {
                //GetMfgOrderMustBeDoing();
            }
        }
        private void RealtimeMfg_Click(object sender, EventArgs e)
        {
            if(RealtimeMfg.Checked == true)
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
            } else
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
        private void Cb_StatusCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }
        private void Cb_StatusReason_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }
        #endregion

        private void RealtimeMfg_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
