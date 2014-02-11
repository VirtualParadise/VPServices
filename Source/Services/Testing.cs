using System;
using System.Text.RegularExpressions;

namespace VPServices.Services
{
    public class Testing : IService
    {
        public string Name
        {
            get { return "Testing"; }
        }

        public Command[] Commands
        {
            get { return new[] {
                new Command("Test", "test", onTest,
                    "This command should be disabled") { Enabled = false },

                new Command("Crash", "crash", onCrash,
                    "This command should crash the commandmanager")
                    {
                        Rights = new[] { Rights.Admin }
                    },
            }; }
        }

        public void Load() { }

        public void Unload() { }

        bool onTest(User who, string data)
        {
            throw new InvalidOperationException("This command is supposed to be disabled");
        }

        bool onCrash(User who, string data)
        {
            throw new Exception("Forced crash");
        }
    }
}
