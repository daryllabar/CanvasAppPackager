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
        private Stack<string> ComponentControlNames { get; } = new Stack<string>();
        private string RootCodePath { get; set; }
        private string Name { get; set; }
        private string ComponentName { get; set; }

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

        public void PushComponentControl(string name)
        {
            ComponentControlNames.Push(name);
            ComponentName = string.Join(Environment.NewLine, ComponentControlNames.ToArray().Reverse());
        }

        public void PopComponentControl()
        {
            ComponentControlNames.Pop();
            ComponentName = string.Join(Environment.NewLine, ComponentControlNames.ToArray().Reverse());
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
            var name = string.IsNullOrWhiteSpace(ComponentName) ? Name : Name + "|" + ComponentName;
            if (!AutoValues.TryGetValue(name, out var value))
            {
                value = new AutoValue
                {
                    Name = name,
                };
                AutoValues.Add(name, value);
            }

            value.ControlUniqueId = control.ControlUniqueId;
            control.ControlUniqueId = null;
            value.PublishOrderIndex = control.PublishOrderIndex.GetValueOrDefault();
            control.PublishOrderIndex = null;

            if (control.Template.Id == @"http://microsoft.com/appmagic/Component")
            {
                // Only remove version for Templates.
                value.TemplateLastModified = control.Template.LastModifiedTimestamp;
                control.Template.LastModifiedTimestamp = null;
                value.TemplateComponentDefinitionLastModified = control.Template.ComponentDefinitionInfo.LastModifiedTimestamp;
                control.Template.ComponentDefinitionInfo.LastModifiedTimestamp = null;
                value.TemplateVersion = control.Template.Version;
                control.Template.Version = null;
            }
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
            var name = string.Join(Environment.NewLine, Path.GetRelativePath(RootCodePath, Path.GetDirectoryName(controlPath)).Split(new[] { Path.DirectorySeparatorChar }));
            name = string.IsNullOrWhiteSpace(ComponentName) ? name : name + "|" + ComponentName;
            if (!AutoValues.TryGetValue(name, out var value))
            {
                throw  new KeyNotFoundException($"Unable to find auto value for rule {control.Name} at {controlPath}");
            }

            control.ControlUniqueId = value.ControlUniqueId;
            control.PublishOrderIndex = value.PublishOrderIndex;
            if (control.Template.Id == @"http://microsoft.com/appmagic/Component")
            {
                // Only remove version for Templates.
                control.Template.Version = value.TemplateVersion;
                control.Template.LastModifiedTimestamp = value.TemplateLastModified;
                control.Template.ComponentDefinitionInfo.LastModifiedTimestamp = value.TemplateComponentDefinitionLastModified;
            }
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

        public void ExtractComponentChildren(List<Child> children)
        {
            foreach (var child in children)
            {
                PushComponentControl(child.Name);
                Extract(child);
                ExtractComponentChildren(child.Children);
                PopComponentControl();
            }
        }

        public void InjectComponentChildren(List<Child> children, string jsonFile)
        {
            foreach (var child in children)
            {
                PushComponentControl(child.Name);
                Inject(child, jsonFile);
                ExtractComponentChildren(child.Children);
                PopComponentControl();
            }
        }
    }
}
