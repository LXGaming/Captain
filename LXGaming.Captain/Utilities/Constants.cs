﻿using System.Reflection;
using LXGaming.Common.Utilities;

namespace LXGaming.Captain.Utilities;

public static class Constants {

    public static class Application {

        public const string Name = "Captain";
        public const string Authors = "LX_Gaming";
        public const string Website = "https://lxgaming.github.io/";

        public static readonly string Version = AssemblyUtils.GetVersion(Assembly.GetExecutingAssembly(), "Unknown");
        public static readonly string UserAgent = $"{Name}/{Version} (+{Website})";
    }
}