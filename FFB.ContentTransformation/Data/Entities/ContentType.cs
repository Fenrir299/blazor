// Data/Entities/ContentType.cs
namespace FFB.ContentTransformation.Data.Entities
{
    /// <summary>
    /// Enum for the different types of content that can be generated
    /// </summary>
    public enum ContentType
    {
        WebArticle,
        LinkedInPost,
        Email
    }

    /// <summary>
    /// Enum for different content length options
    /// </summary>
    public enum ContentFormat
    {
        Short,
        Medium,
        Long
    }
}