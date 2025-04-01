// Models/ContentFormatOptions.cs
using System.Collections.Generic;
using FFB.ContentTransformation.Data.Entities;

namespace FFB.ContentTransformation.Models
{
    /// <summary>
    /// Helper class to provide content format options
    /// </summary>
    public static class ContentFormatOptions
    {
        public static Dictionary<ContentFormat, string> Options => new()
        {
            { ContentFormat.Short, "Court" },
            { ContentFormat.Medium, "Moyen" },
            { ContentFormat.Long, "Long" }
        };

        public static Dictionary<ContentFormat, int> ApproximateWordCounts => new()
        {
            { ContentFormat.Short, 150 },
            { ContentFormat.Medium, 300 },
            { ContentFormat.Long, 600 }
        };
    }
}
