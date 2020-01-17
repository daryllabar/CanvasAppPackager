using System;
using System.Collections.Generic;
using System.Linq;

namespace CanvasAppPackager.Args
{
    public class Args
    {
        public bool Help { get; set; }

        public List<string> Extra { get; set; }

        // Option format strings:
        //  Regex-like BNF Grammar: 
        //    name: .+
        //    type: [=:]
        //    sep: ( [^{}]+ | '{' .+ '}' )?
        //    aliases: ( name type sep ) ( '|' name type sep )*
        // 
        // Each '|'-delimited name is an alias for the associated action.  If the
        // format string ends in a '=', it has a required value.  If the format
        // string ends in a ':', it has an optional value.  If neither '=' or ':'
        // is present, no value is supported.  `=' or `:' need only be defined on one
        // alias, but if they are provided on more than one they must be consistent.
        //
        // Each alias portion may also end with a "key/value separator", which is used
        // to split option values if the option accepts > 1 value.  If not specified,
        // it defaults to '=' and ':'.  If specified, it can be any character except
        // '{' and '}' OR the *string* between '{' and '}'.  If no separator should be
        // used (i.e. the separate values should be distinct arguments), then "{}"
        // should be used as the separator.
        //
        // Options are extracted either from the current option by looking for
        // the option name followed by an '=' or ':', or is taken from the
        // following option IFF:
        //  - The current option does not contain a '=' or a ':'
        //  - The current option requires a value (i.e. not a Option type of ':')
        //
        // The `name' used in the option format string does NOT include any leading
        // option indicator, such as '-', '--', or '/'.  All three of these are
        // permitted/required on any named option.
        public OptionSet Options
        {
            get
            {
                return new OptionSet
                {
                    {"a=|action", "Required. The action to perform.  The action can either be to extract the application package zip to a folder, or to pack a folder into a .zip file.", v =>
                        {
                            ActionText = v.Trim();
                            switch (ActionText.ToLower())
                            {
                                case "pack":
                                    Action = ActionType.Pack;
                                    break;
                                case "unpack":
                                    Action = ActionType.Unpack;
                                    break;
                                default:
                                    Action = ActionType.Invalid;
                                    break;
                            }
                        }
                    },
                    {"c|Clobber|clobber", "Optional. This argument is used only during an extraction. When /clobber is specified, files that have the read-only attribute set are overwritten or deleted. When not specified, files with the read-only attribute aren't overwritten or deleted.", v => Clobber = v != null }, 
                    {"z=|ZipFile", "Required. The path and name of an application package .zip file. When extracting, the file must exist and will be read from, or must be 'Latest' to use the latest downloaded file. When packing, the file is replaced.", v => PackageZip = v?.Trim() },
                    {"f=|folder", "Required. The path to a folder. When extracting, this folder is created and populated with component files. When packing, this folder must already exist and contain previously extracted component files.", v => UnpackPath = v?.Trim()},
                    {"l:|log", "Optional. A path and name to a log file. If the file already exists, new logging information is appended to the file.", v => LogPath = v?.Trim()},
                    {"o:|OnlyExtract", "Optional.  Only unpacks the MsApp file, does not parse it.", v => OnlyExtract = bool.Parse(v.Trim()) },
                    {"r=|RenameCopiedScreenControls", "Optional. Only valid on Unpack.  Should contain a piped delimited string of {OldScreenPostfixValue}|{NewScreenPostfixValue}.  So EditScreen|DisplayScreen to rename a control MyLabelEditScreen_1 to MyLabelDisplayScreen", v =>
                    {
                        RenameCopiedControlOldPostfix = v?.Split('|').FirstOrDefault()?.Trim();
                        RenameCopiedControlNewPostfix = v?.Split('|').Skip(1).FirstOrDefault()?.Trim();
                    }},
                    {"h|?|help", v => Help = v != null}
                };
            }
        }

        public ActionType Action { get; set; }
        public string ActionText { get; set; }
        public bool Clobber { get; set; }
        public string PackageZip { get; set; }
        public string UnpackPath { get; set; }
        public string LogPath { get; set; }
        public bool OnlyExtract { get; set; }
        public string RenameCopiedControlOldPostfix { get; set; }
        public string RenameCopiedControlNewPostfix { get; set; }

        public Args()
        {
            Clobber = false;
        }

        public static Args Parse(string[] args)
        {
            var result = new Args();
            result.Extra = result.Options.Parse(args);
            return result;
        }

        public enum ActionType
        {
            Pack, Unpack, Invalid
        }
    }
}
