using System;

namespace golf1052.ThreadsAPI.Models
{
    public class ThreadsMediaObject
    {
        public string Id { get; set; } = string.Empty;
        public string? MediaProductId { get; set; }
        public string? MediaType { get; set; }
        public string? MediaUrl { get; set; }
        public string? Permalink { get; set; }
        public ThreadsMediaObjectOwner? Owner { get; set; }
        public string? Username { get; set; }
        public string? Text { get; set; }
        public DateTime? Timestamp { get; set; }
        public string? Shortcode { get; set; }
        public string? ThumbnailUrl { get; set; }
        public bool? IsQuotePost { get; set; }
    }

    public class ThreadsMediaObjectOwner
    {
        public string Id { get; set; } = string.Empty;
    }
}
