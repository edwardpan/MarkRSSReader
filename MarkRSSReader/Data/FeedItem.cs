using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkRSSReader.Data {
    /// <summary>
    /// RSS源中的一篇文章
    /// </summary>
    public class FeedItem : FeedCommon {

        public string Author { get; set; }
        public DateTime PubDate { get; set; }
        public Uri Link { get; set; }
        public Boolean IsRead { get; set; }
        private string _content = string.Empty;
        public string Content {
            get { return this._content; }
            set { this.SetProperty(ref this._content, value); }
        }

        private Feed _feed;
        public Feed Feed {
            get { return this._feed; }
            set { this.SetProperty(ref this._feed, value); }
        }

        public FeedItem() { }
        public FeedItem(String uniqueId, String title, String subtitle, String imagePath, String description, String content, Feed feed)
            : base(uniqueId, title, subtitle, imagePath, description) {
            this._content = content;
            this._feed = feed;
        }
    }
}
