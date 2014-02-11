using System;
using System.ComponentModel;

namespace VPServices.Internal
{
    public class Arguments
    {
        [Description("Specifies INI file to use with Services")]
        [DefaultValue("Settings.ini")]
        public string Ini { get; set; }

        [Description("Specifies the logging level to use throughout Services")]
        [DefaultValue(LogLevels.Production)]
        public LogLevels LogLevel { get; set; }
    }
}
