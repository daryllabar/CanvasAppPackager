using System;
using System.Collections.Generic;
using System.Text;

namespace CanvasAppPackager.Poco
{
    public class PublishInfo
    {
        public string AppName { get; set; }
        public string BackgroundColor { get; set; }
        public string PublishTarget { get; set; }
        public string LogoFileName { get; set; }
        public bool PublishResourcesLocally { get; set; }
        public bool PublishDataLocally { get; set; }
        public string UserLocale { get; set; }
    }
}
