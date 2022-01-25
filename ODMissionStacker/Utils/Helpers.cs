using System;
using System.Windows;

namespace ODMissionStacker.Utils
{
    public class Helpers
    {
        public static void SetClipboard(object data)
        {
            try
            {
                Clipboard.SetDataObject(data, true);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Error sending Name to Clipboard\nError : {ex.Message}");
            }
        }
    }
}