﻿using System;
using System.IO;
using DBClientFiles.NET.Definitions.Parsers;

namespace DBClientFiles.NET.Mapper.Definitions
{
    internal static class DefinitionFactory
    {
        public static DBD Open(string definitionName)
        {
            var completePath = Path.Combine(Properties.Settings.Default.DefinitionRoot, definitionName + ".dbd");

            using (var fs = File.OpenRead(completePath))
                return new DBD(definitionName, fs);
        }

        public static void Save(string definitionName, Type newDefinition)
        {
            var completePath = Path.Combine(Properties.Settings.Default.DefinitionRoot, definitionName + ".dbd");

            using (var fs = new FileStream(completePath, FileMode.Open))
            {
                var definition = new DBD(definitionName, fs);
                definition.Save(newDefinition);
            }
        }
    }
}