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


        public static AppInfo Parse(string json)
        {
            dynamic root = JsonConvert.DeserializeObject(json);
            var info = new AppInfo
            {
                DisplayName = root.appDefinitionTemplate.properties.displayName.Value,
                Description = root.appDefinitionTemplate.properties.description.Value,
                MsAppPath = root.appDefinitionTemplate.properties.appUris.documentUri.value.Value
            };
            info.MsAppPath = info.MsAppPath.Substring(1, info.MsAppPath.Length - 1).Replace("/", "\\");
            info.AppId = info.MsAppPath.Split('\\').First();

            return info;
        }
    }

}
