using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkRSSReader.Data {
    /// <summary>
    /// 一个RSS源
    /// </summary>
    public class Feed : FeedCommon {
        public DateTime PubDate { get; set; }
        public Uri Source { get; set; }
        public string Background { get; set; }

        public Feed() { }
        public Feed(String uniqueId, String title, String subtitle, String imagePath, String description, FeedGroup group)
            : base(uniqueId, title, subtitle, imagePath, description) {
            this._group = group;
        }

        private ObservableCollection<FeedItem> _items = new ObservableCollection<FeedItem>();
        public ObservableCollection<FeedItem> Items {
            get { return this._items; }
            set { this.SetProperty(ref this._items, value); }
        }

        public IEnumerable<FeedItem> TopItems {
            // Provides a subset of the full items collection to bind to from a GroupedItemsPage
            // for two reasons: GridView will not virtualize large items collections, and it
            // improves the user experience when browsing through groups with large numbers of
            // items.
            //
            // A maximum of 12 items are displayed because it results in filled grid columns
            // whether there are 1, 2, 3, 4, or 6 rows displayed
            get { return this._items.Take(12); }
        }

        private FeedGroup _group;
        public FeedGroup Group {
            get { return this._group; }
            set { this.SetProperty(ref this._group, value); }
        }
    }
}
