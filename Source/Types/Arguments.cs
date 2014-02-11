using System;
using System.ComponentModel;

namespace VPServices.Types
{
    public class Arguments
    {
        [Description("Specifies INI file to use with Services")]
        [DefaultValue("Services.ini")]
        public string Ini { get; set; }

        [Description("Specifies the logging level to use throughout Services")]
        [DefaultValue(LogLevels.Production)]
        public LogLevels LogLevel { get; set; }
    }
}
