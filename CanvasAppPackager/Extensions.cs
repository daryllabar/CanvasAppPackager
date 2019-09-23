using Newtonsoft.Json;

namespace CanvasAppPackager
{
    public static class Extensions
    {
        public static string Serialize(this object obj)
        {
            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }
    }
}
