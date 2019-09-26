using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CanvasAppPackager.Poco;
using Newtonsoft.Json;

namespace CanvasAppPackager
{
    public class AutoValueExtractor
    {
        private Dictionary<string, AutoValue> AutoValues { get; set; }
        private HashSet<string> JsRulesHandled { get; } = new HashSet<string>(new []{ "ZIndex" });
        private Stack<string> ControlNames { get; } = new Stack<string>();
        private string RootCodePath { get; set; }
        private string Name { get; set; }

        public AutoValueExtractor()
        {
            AutoValues = new Dictionary<string, AutoValue>();
        }

        public void PushControl(string name)
        {
            ControlNames.Push(name);
            Name = string.Join(Environment.NewLine, ControlNames.ToArray().Reverse());
        }

        public void PopControl()
        {
            ControlNames.Pop();
            Name = string.Join(Environment.NewLine, ControlNames.ToArray().Reverse());
        }

        public bool Extract(Rule rule)
        {
            if (!JsRulesHandled.Contains(rule.Property))
            {
                return false;
            }

            if (!AutoValues.TryGetValue(Name, out var value))
            {
                value = new AutoValue
                {
                    Name = Name,
                };
                AutoValues.Add(Name, value);
            }

            switch (rule.Property)
            {
                case "ZIndex":
                    value.ZIndex = rule.InvariantScript;
                    break;

                default:
                    throw new Exception($"Rule Property {rule.Property} not defined for AutoValueExtractor.Extract");
            }

            rule.InvariantScript = "AutoValue";
            return true;
        }

        public void Extract(IControl control)
        {
            if (!AutoValues.TryGetValue(Name, out var value))
            {
                value = new AutoValue
                {
                    Name = Name,
                };
                AutoValues.Add(Name, value);
            }

            value.ControlUniqueId = control.ControlUniqueId;
            control.ControlUniqueId = null;
            value.PublishOrderIndex = control.PublishOrderIndex.GetValueOrDefault();
            control.PublishOrderIndex = null;
        }

        public string Serialize()
        {
            var value = new AutoValues
            {
                Values = AutoValues.OrderBy(v => v.Key).Select(v => v.Value).ToList()
            };
            return value.Serialize(Formatting.Indented);
        }

        public void Inject(Rule rule, string controlPath)
        {
            if (!JsRulesHandled.Contains(rule.Property))
            {
                return;
            }

            var name = string.Join(Environment.NewLine, Path.GetRelativePath(RootCodePath, Path.GetDirectoryName(controlPath)).Split(new[] {Path.DirectorySeparatorChar}));
            if (!AutoValues.TryGetValue(name, out var value))
            {
                throw  new KeyNotFoundException($"Unable to find auto value for rule {rule.Property} at {controlPath}");
            }

            switch (rule.Property)
            {
                case "ZIndex":
                    rule.InvariantScript = value.ZIndex;
                    break;

                default:
                    throw new Exception($"Rule Property {rule.Property} not defined for AutoValueExtractor.Inject");
            }
        }

        public void Inject(IControl control, string controlPath)
        {
            var name = string.Join(Environment.NewLine, Path.GetRelativePath(RootCodePath, Path.GetDirectoryName(controlPath)).Split(new[] {Path.DirectorySeparatorChar}));
            if (!AutoValues.TryGetValue(name, out var value))
            {
                throw  new KeyNotFoundException($"Unable to find auto value for rule {control.Name} at {controlPath}");
            }

            control.ControlUniqueId = value.ControlUniqueId;
            control.PublishOrderIndex = value.PublishOrderIndex;
        }


        public static AutoValueExtractor Parse(string autoValuesPath)
        {
            return new AutoValueExtractor
            {
                AutoValues = JsonConvert.DeserializeObject<AutoValues>(File.ReadAllText(autoValuesPath))
                                        .Values.ToDictionary(k => k.Name),
                RootCodePath = Path.GetDirectoryName(autoValuesPath)
            };
        }
    }
}
