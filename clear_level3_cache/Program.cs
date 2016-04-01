﻿using System;
using Level3Utils;

namespace clear_level3_cache
{
    class Program
    {
        public static void Main(string[] args)
        {
//            var inputReader = new HardCodedInput();
//            var inputReader = new Level3Utils.OctopusInput();
            var inputReader = new Level3Utils.ArgumentsInput(args);
            new Level3Utils.CacheInvalidatorProgram(inputReader).Execute();

            Console.ReadKey(true);
        }

        public class HardCodedInput : CacheInvalidatorProgram.IInputReader
        {
            public CacheInvalidatorProgram.Input Read()
            {
                return new CacheInvalidatorProgram.Input
                {
                    ApiKey = "288519499",
                    ApiSecret = "9TJtJkxW66jXGQS2zS4s",
                    UrlsSeparatedByComma = "sadminmsc.ipcoop.com,stg.mysubwaycareer.com",
                    NotificationEmail = "csanchez@ipcoop.com"
                };
            }
        }

    }
}