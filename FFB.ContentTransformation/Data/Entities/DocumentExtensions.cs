// Data/Entities/DocumentExtensions.cs
using System.ComponentModel;

namespace FFB.ContentTransformation.Data.Entities
{
    /// <summary>
    /// Extensions for the Document entity
    /// </summary>
    public static class DocumentExtensions
    {
        // This property will not be persisted to the database
        // It's used for UI binding in multi-document selection
        public static bool IsSelected { get; set; }
    }

    /// <summary>
    /// Partial class to extend Document with non-database properties
    /// </summary>
    public partial class Document
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsSelected { get; set; }
    }
}