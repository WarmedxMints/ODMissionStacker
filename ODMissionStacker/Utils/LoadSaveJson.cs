using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows;

namespace ODMissionStacker.Utils
{
    public class LoadSaveJson
    {
        public static T LoadJson<T>(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    string saveString = File.ReadAllText(path);

                    T rslt = JsonConvert.DeserializeObject<T>(saveString);

                    return rslt;
                }
                else
                {
                    return default;
                }
            }
            catch
            {
                return default;
            }
        }

        public static bool SaveJson<T>(T objectToSave, string path)
        {
            try
            {
                string directory = Path.GetDirectoryName(path);

                if (!Directory.Exists(directory))
                {
                    _ = Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(objectToSave, Formatting.Indented);

                using (FileStream fs = new(path, FileMode.Create))
                {
                    using StreamWriter writer = new(fs);
                    writer.Write(json);
                }
                return true;
            }
            catch (Exception e)
            {
                _ = MessageBox.Show($"Error saving {path} \n {e}");
                return false;
            }
        }
    }
}
