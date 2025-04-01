// Models/ContentTargetOptions.cs
using System.Collections.Generic;
using FFB.ContentTransformation.Data.Entities;

namespace FFB.ContentTransformation.Models
{
    /// <summary>
    /// Helper class to provide content target options
    /// </summary>
    public static class ContentTargetOptions
    {
        public static Dictionary<ContentType, string> Options => new()
        {
            { ContentType.WebArticle, "Article Web" },
            { ContentType.LinkedInPost, "LinkedIn" },
            { ContentType.Email, "Mail" }
        };
    }
}