using System.Diagnostics;

namespace PopUpMessage
{
    public class PopUpMessageHelper
    {
        public static void Show()
        {
            var process = Process.Start(".\\PopUpMessage.exe");
        }

        public static void CloseAll()
        {
            var getProcesses = Process.GetProcesses();
            var processes = Process.GetProcessesByName("PopUpMessage");
            foreach (var process in processes)
            {
                process.Kill();
            }
        }
    }
}
