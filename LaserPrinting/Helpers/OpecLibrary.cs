using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using OpcenterWikLibrary;
using Camstar.WCF.Services;
using Camstar.WCF.ObjectStack;

namespace LaserPrinting.Helpers
{
    public class OpecLibrary
    {
        public static string AddVersionNumber(string text)
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

            text += $" V.{versionInfo.FileVersion}";
            return text;
        }
        public static NamedObjectRef[] GetResourceStatusCodeList(ServiceUtil oServiceUtil)
        {
            NamedObjectRef[] oStatusCodeList = oServiceUtil.GetListResourceStatusCode();
            if (oStatusCodeList != null)
            {
                return oStatusCodeList;
            }
            return new NamedObjectRef[] { };
        }
        public static MfgOrderChanges ContainerOfMfgOrder(string MfgOrder, ServiceUtil oServiceUtil)
        {
            try
            {
                if (!string.IsNullOrEmpty(MfgOrder))
                {
                    var oMfgOrderChanges = oServiceUtil.GetMfgOrder(MfgOrder);
                    if (oMfgOrderChanges != null)
                    {
                        return oMfgOrderChanges;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
            return null;
        }
        public static GetMaintenanceStatusDetails[] GetStatusMaintenanceDetails(string resource, ServiceUtil oServiceUtil)
        {
            try
            {
                var oMaintenanceStatus = oServiceUtil.GetGetMaintenanceStatus(AppSettings.Resource);
                if (oMaintenanceStatus != null)
                {
                    return oMaintenanceStatus;
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod().Name : MethodBase.GetCurrentMethod().Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
            return null;
        }
        public static ResourceStatusDetails GetStatusOfResource(string resource, ServiceUtil oServiceUtil)
        {
            try
            {
                ResourceStatusDetails oResourceStatusDetails = oServiceUtil.GetResourceStatusDetails(resource);
                if (oResourceStatusDetails != null)
                {
                    return oResourceStatusDetails;
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod().Name : MethodBase.GetCurrentMethod().Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
            return null;
        }
    }
}
