using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace CanvasAppPackager
{

    public class AppInfo
    {
        public string AppId { get; set; }
        public string Description { get; set; }
        public string DisplayName { get; set; }
        public string MsAppPath { get; set; }
        public string BackgroundImage { get; set; }
        public Dictionary<string, string> Icons { get; set; }


        public static AppInfo Parse(string json)
        {
            dynamic root = JsonConvert.DeserializeObject(json);
            var properties = root.appDefinitionTemplate.properties;
            var info = new AppInfo
            {
                DisplayName = properties.displayName.Value,
                Description = properties.description.Value,
                MsAppPath = FormatRelativePath(properties.appUris.documentUri.value.Value),
                BackgroundImage = FormatRelativePath(properties.backgroundImageUri.Value),
                Icons = new Dictionary<string,string>()
            };

            info.AppId = info.MsAppPath.Split('\\').First();

            foreach (var icon in ((Newtonsoft.Json.Linq.JObject)properties.icons).Properties())
            {
                info.Icons.Add(icon.Name, FormatRelativePath(icon.Value.ToString()));
            }
            return info;
        }

        private static string FormatRelativePath(string path)
        {
            return path.Substring(1, path.Length - 1).Replace("/", "\\");
        }
    }

}
