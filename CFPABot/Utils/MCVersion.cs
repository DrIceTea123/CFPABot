﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CFPABot.Utils
{
    public enum MCVersion
    {
        v1122,
        v116,
        v118,
        v116fabric,
        v118fabric
    }

    public static class MCVersionExtensions
    {
        public static string ToVersionString(this MCVersion version)
        {
            return version switch
            {
                MCVersion.v1122 => "1.12.2",
                MCVersion.v116 => "1.16",
                MCVersion.v118 => "1.18",
                MCVersion.v116fabric => "1.16-fabric",
                MCVersion.v118fabric => "1.18-fabric",
                _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
            };
        }

        public static MCVersion ToMCVersion(this string version)
        {
            return version switch
            {
                "1.12.2" => MCVersion.v1122,
                "1.16" => MCVersion.v116,
                "1.18" => MCVersion.v118,
                "1.16-fabric" => MCVersion.v116fabric ,
                "1.18-fabric" => MCVersion.v118fabric,
                _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
            };
        }

        public static string ToCNLangFile(this MCVersion version)
        {
            return version switch
            {
                MCVersion.v116 or MCVersion.v118 => "zh_cn.json",
                MCVersion.v1122 => "zh_cn.lang"
            };
        }

        public static string ToENLangFile(this MCVersion version)
        {
            return version switch
            {
                MCVersion.v116 or MCVersion.v118 => "en_us.json",
                MCVersion.v1122 => "en_us.lang"
            };
        }
    }
}
