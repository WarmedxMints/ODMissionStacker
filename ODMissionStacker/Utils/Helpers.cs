using System;
using System.Collections.Generic;
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

        public static long SafeDivisionLong(long Numeraotr, long Denominator)
        {
            return (Denominator == 0) ? 0 : Numeraotr / Denominator;
        }

        public static double SafeDivisionDouble(double Numeraotr, double Denominator)
        {
            return (Denominator == 0) ? 0 : Numeraotr / Denominator;
        }

        public static List<T> CloneList<T>(ICollection<T> collectionToClone)
        {
            var source = new List<T>();

            foreach (T item in collectionToClone)
            {
                source.Add(item.Clone());
            }

            return source;
        }
    }
}