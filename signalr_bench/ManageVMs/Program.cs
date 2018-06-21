using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace ManageVMs
{
    class Program
    {
        public static void Main (string[] args)
        {
            // parse args
            var argsOption = new ArgsOption();
            var result = Parser.Default.ParseArguments<ArgsOption>(args)
                .WithParsed(options => argsOption = options)
                .WithNotParsed(error => { });
            
            // auth file

            // create resource group

            // create vms
        
            // change settings

        }
    }
}
