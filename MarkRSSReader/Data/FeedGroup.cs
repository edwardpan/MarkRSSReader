using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkRSSReader.Data {
    /// <summary>
    /// RSS的Feed源分组
    /// </summary>
    public class FeedGroup : FeedCommon {
        public FeedGroup() { }
        public FeedGroup(String uniqueId, String title, String subtitle, String imagePath, String description)
            : base(uniqueId, title, subtitle, imagePath, description) {
        }

        private ObservableCollection<Feed> _feeds = new ObservableCollection<Feed>();
        public ObservableCollection<Feed> Feeds {
            get { return this._feeds; }
        }

        public IEnumerable<Feed> TopFeeds {
            // Provides a subset of the full items collection to bind to from a GroupedItemsPage
            // for two reasons: GridView will not virtualize large items collections, and it
            // improves the user experience when browsing through groups with large numbers of
            // items.
            //
            // A maximum of 12 items are displayed because it results in filled grid columns
            // whether there are 1, 2, 3, 4, or 6 rows displayed
            get { return this._feeds.Take(4); }
        }
    }
}
