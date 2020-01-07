using System.Collections.Generic;

namespace CanvasAppPackager.Poco
{
    public class AutoValues
    {
        public List<AutoValue> Values { get; set; }

        public AutoValues()
        {
            Values = new List<AutoValue>();
        }
    }

    public class AutoValue
    {
        public string Name { get; set; }
        public string ZIndex { get; set; }
        public int PublishOrderIndex { get; set; }
        public string ControlUniqueId { get; set; }
        public string TemplateVersion { get; set; }
    }
}
