using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Data;
using Windows.Storage;
using Windows.Web.Syndication;
using Windows.Data.Xml.Dom;
using System.Runtime.InteropServices;

// The data model defined by this file serves as a representative example of a strongly-typed
// model that supports notification when members are added, removed, or modified.  The property
// names chosen coincide with data bindings in the standard item templates.
//
// Applications may use this model as a starting point and build on it, or discard it entirely and
// replace it with something appropriate to their needs.

namespace MarkRSSReader.Data
{
    /// <summary>
    /// 程序数据源，RSS源数据获取对象
    /// </summary>
    public sealed class FeedDataSource
    {
        private static FeedDataSource _feedDataSource = new FeedDataSource();

        public static FeedDataSource getInstance() {
            if (_feedDataSource == null) {
                _feedDataSource = new FeedDataSource();
            }
            return _feedDataSource;
        }

        private ObservableCollection<FeedGroup> _allGroups = new ObservableCollection<FeedGroup>();
        public ObservableCollection<FeedGroup> AllGroups {
            get { return this._allGroups; }
        }
        private StorageFile feedsFile = null;
        private XmlDocument feedsDoc = null;

        private FeedDataSource() {}
        public async Task init() {
            var loadSettings = new Windows.Data.Xml.Dom.XmlLoadSettings();
            loadSettings.ProhibitDtd = true;
            loadSettings.ResolveExternals = false;

            if (feedsFile == null) {
                // 获取当前应用的配置文件夹（支持可同步的）
                StorageFolder folder = Windows.Storage.ApplicationData.Current.RoamingFolder;
                // 打开RSS源XML配置文件，如果不存在则创建
                feedsFile = await folder.CreateFileAsync("feeds.xml", CreationCollisionOption.OpenIfExists);

                // 如果应用为第一次运行，则复制基础XML配置文件
                ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                object notFirstRun = roamingSettings.Values["notFirstRun"];
                if (notFirstRun == null) {
                    StorageFolder templateFolder = await
                        Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("data");
                    StorageFile templateFile = await templateFolder.CreateFileAsync(
                        "feeds.xml", CreationCollisionOption.OpenIfExists);
                    await templateFile.CopyAndReplaceAsync(feedsFile);
                    roamingSettings.Values["notFirstRun"] = true;
                }
            }
            if (feedsDoc == null) {
                // 读取XML配置文件
                feedsDoc = await XmlDocument.LoadFromFileAsync(feedsFile, loadSettings);
            }
        }

        public async Task saveCache() {
            await feedsDoc.SaveToFileAsync(feedsFile);
        }

        //public static IEnumerable<FeedGroup> GetGroups(string uniqueId)
        //{
        //    if (!uniqueId.Equals("AllGroups")) throw new ArgumentException("只有'AllGroups'标识才支持分组数据集合。");
            
        //    return _feedDataSource.AllGroups;
        //}

        public FeedGroup GetGroup(string uniqueId) {
            // Simple linear search is acceptable for small data sets
            var matches = _feedDataSource.AllGroups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        //public static Feed GetFeed(string uniqueId)
        //{
        //    // Simple linear search is acceptable for small data sets
        //    var matches = _feedDataSource.AllGroups.SelectMany(group => group.Feeds).Where((feed) => feed.UniqueId.Equals(uniqueId));
        //    if (matches.Count() == 1) return matches.First();
        //    return null;
        //}

        //public static FeedItem GetFeedItem(string uniqueId) {
        //    var matches = _feedDataSource.AllGroups.SelectMany(group => group.Feeds).Where((feed) => feed.UniqueId.Equals(uniqueId));
        //    var itemMatches = matches.SelectMany(feed => feed.Items).Where((item) => item.UniqueId.Equals(uniqueId));
        //    if (itemMatches.Count() == 1) return itemMatches.First();
        //    return null;
        //}

        public void initFeedGroup()
        {
            AllGroups.Clear();

            IXmlNode root = feedsDoc.SelectSingleNode("groups");
            XmlNodeList groupList = root.SelectNodes("group");
            IOrderedEnumerable<IXmlNode> groupOrderList = groupList.OrderBy(g => 
                int.Parse(g.SelectSingleNode("order").InnerText) );
            foreach (IXmlNode node in groupOrderList) {
                var group = new FeedGroup();
                group.UniqueId = node.SelectSingleNode("uniqueId").InnerText;
                group.Title = node.SelectSingleNode("title").InnerText;
                group.Description = node.SelectSingleNode("description").InnerText;
                group.Order = int.Parse(node.SelectSingleNode("order").InnerText);

                IXmlNode feeds = node.SelectSingleNode("feeds");
                if (feeds != null) {
                    XmlNodeList feedList = feeds.SelectNodes("feed");
                    IOrderedEnumerable<IXmlNode> feedOrderList = feedList.OrderBy(f =>
                        int.Parse(f.SelectSingleNode("order").InnerText));
                    foreach (IXmlNode feedXml in feedOrderList) {
                        Feed feed = new Feed();
                        // 编号
                        feed.UniqueId = feedXml.SelectSingleNode("uniqueId").InnerText;
                        // 标题
                        feed.Title = feedXml.SelectSingleNode("title").InnerText;
                        // URI
                        string uri = feedXml.SelectSingleNode("uri").InnerText;
                        feed.Source = new Uri(uri);
                        // 分组
                        feed.Group = group;
                        // 颜色
                        IXmlNode bgcolorXml = feedXml.SelectSingleNode("bgcolor");
                        if (bgcolorXml == null) {
                            bgcolorXml = feedsDoc.CreateElement("bgcolor");
                            bgcolorXml.InnerText = Backgrounds.Instance.Color;
                            //feedXml.AppendChild(bgcolorXml);
                        }
                        feed.Background = bgcolorXml.InnerText;
                        feed.Order = int.Parse(feedXml.SelectSingleNode("order").InnerText);
                        
                        group.Feeds.Add(feed);
                    }
                }

                this.AllGroups.Add(group);
            }
        }

        public async Task initFeedAsync(Feed loadFeed) {
            // using Windows.Web.Syndication;
            SyndicationClient client = new SyndicationClient();

            try {
                SyndicationFeed synFeed = await client.RetrieveFeedAsync(loadFeed.Source);

                // This code is executed after RetrieveFeedAsync returns the SyndicationFeed.
                // Process it and copy the data we want into our FeedData and FeedItem classes.

                if (loadFeed.Title.Trim().Length == 0) {
                    loadFeed.Title = synFeed.Title.Text;
                }
                if (synFeed.Subtitle.Text != null && synFeed.Subtitle.Text.Trim().Length != 0) {
                    loadFeed.Subtitle = synFeed.Subtitle.Text;
                }
                if (synFeed.Items.Count != 0) {
                    loadFeed.Description = synFeed.Items[0].Title.Text;
                }
                // Use the date of the latest post as the last updated date.
                loadFeed.PubDate = synFeed.Items[0].PublishedDate.DateTime;

                // 加载Feed中的文章，并保存到DB中
                List<FeedItem> feedItemList = new List<FeedItem>();
                foreach (SyndicationItem item in synFeed.Items) {
                    FeedItem feedItem = new FeedItem();
                    feedItem.Title = item.Title.Text;
                    feedItem.PubDate = item.PublishedDate.DateTime;
                    feedItem.Author = item.Authors[0].Name.ToString();
                    // Handle the differences between RSS and Atom feeds
                    if (synFeed.SourceFormat == SyndicationFormat.Atom10) {
                        feedItem.Content = item.Content.Text;
                        feedItem.Link = new Uri("http://windowsteamblog.com" + item.Id);
                    } else if (synFeed.SourceFormat == SyndicationFormat.Rss20) {
                        feedItem.Content = item.Summary.Text;
                        feedItem.Link = item.Links[0].Uri;
                    }
                    feedItem.Feed = loadFeed;
                    feedItemList.Add(feedItem);
                }
                // 保存到DB
                FeedItemDatabase.getInstance().saveFeedItems(loadFeed, feedItemList);
                // 读取DB
                FeedItemDatabase.getInstance().loadFeedItems(loadFeed);
                //// 保存到数据文件
                //await FeedItemDatabase.getInstance().saveCache();
            } catch (Exception) {
                return;
            }
        }

        public async Task<Feed> saveFeed(Feed feed) {
            // 查找到要添加的组
            IXmlNode group = feedsDoc.SelectSingleNode("/groups/group[uniqueId='" + feed.Group.UniqueId + "']");
            IXmlNode feedsXml = group.SelectSingleNode("feeds");
            if (feedsXml == null) {
                feedsXml = feedsDoc.CreateElement("feeds");
                group.AppendChild(feedsXml);
            }

            if (feed.UniqueId == null || "".Equals(feed.UniqueId)) {// 新创建的
                feed.UniqueId = Guid.NewGuid().ToString();

                IXmlNode feedXml = createFeedXml(feed);
                feedsXml.AppendChild(feedXml);
                feed.Group.Feeds.Add(feed);
            } else {// 对已有的编辑
                // 找到旧的Feed和分组信息
                IXmlNode oldFeedXml = feedsDoc.SelectSingleNode("//feed[uniqueId='" + feed.UniqueId + "']");
                IXmlNode oldGroupXml = oldFeedXml.ParentNode.ParentNode;

                string oldFeedUri = oldFeedXml.SelectSingleNode("uri").InnerText;

                if (feed.Group.UniqueId.Equals(
                    oldGroupXml.SelectSingleNode("uniqueId").InnerText)) {// 分组没有改变

                    oldFeedXml.SelectSingleNode("title").InnerText = feed.Title;
                    oldFeedXml.SelectSingleNode("uri").InnerText = feed.Source.ToString();
                    // 当URI变更后，清除原来的数据
                    if (!oldFeedUri.Equals(feed.Source.ToString())) {
                        FeedItemDatabase.getInstance().clearOldFeedItems(feed);
                    }

                    // 重新加Feed数据
                    await initFeedAsync(feed);
                } else {// 分组改变
                    // 删除原Feed
                    oldGroupXml.SelectSingleNode("feeds").RemoveChild(oldFeedXml);
                    var oldGroup = FeedDataSource.getInstance().GetGroup(oldGroupXml.SelectSingleNode("uniqueId").InnerText);
                    oldGroup.Feeds.Remove(feed);

                    // 重新添加
                    IXmlNode feedXml = createFeedXml(feed);
                    feedsXml.AppendChild(feedXml);
                    feed.Group.Feeds.Add(feed);
                }
            }

            //// 保存XML数据文件
            //await feedsDoc.SaveToFileAsync(feedsFile);
            return feed;
        }

        // 创建用于描述Feed信息的XML节点
        private IXmlNode createFeedXml(Feed feed) {
            // 创建源XML节点
            XmlElement feedXml = feedsDoc.CreateElement("feed");
            // 创建源唯一编号节点
            XmlElement uniqueIdXml = feedsDoc.CreateElement("uniqueId");
            uniqueIdXml.InnerText = feed.UniqueId;
            feedXml.AppendChild(uniqueIdXml);
            // 创建标题节点
            XmlElement titleXml = feedsDoc.CreateElement("title");
            titleXml.InnerText = feed.Title;
            feedXml.AppendChild(titleXml);
            // 创建地址节点
            XmlElement uriXml = feedsDoc.CreateElement("uri");
            uriXml.InnerText = feed.Source.ToString();
            feedXml.AppendChild(uriXml);
            // 创建背景色节点
            XmlElement bgcolorXml = feedsDoc.CreateElement("bgcolor");
            bgcolorXml.InnerText = feed.Background;
            feedXml.AppendChild(bgcolorXml);
            return feedXml;
        }

        public void delFeed(Feed feed) {
            // 查找到要删除的源
            IXmlNode feedXml = feedsDoc.SelectSingleNode("//feed[uniqueId='" + feed.UniqueId + "']");
            IXmlNode feedsXml = feedXml.ParentNode;
            feedsXml.RemoveChild(feedXml);

            //// 保存XML数据文件
            //await feedsDoc.SaveToFileAsync(feedsFile);
        }

        public FeedGroup saveFeedGroup(FeedGroup group) {
            IXmlNode groupsXml = feedsDoc.SelectSingleNode("groups");

            if (group.UniqueId == null || "".Equals(group.UniqueId)) {// 新创建的
                group.UniqueId = Guid.NewGuid().ToString();

                IXmlNode groupXml = createFeedGroupXml(group);
                groupsXml.AppendChild(groupXml);
                AllGroups.Add(group);
            } else {// 对已有的编辑
                // 找到旧的FeedGroup信息
                IXmlNode oldGroupXml = feedsDoc.SelectSingleNode("//group[uniqueId='" + group.UniqueId + "']");

                if (group.UniqueId.Equals(
                    oldGroupXml.SelectSingleNode("uniqueId").InnerText)) {// 分组没有改变

                    oldGroupXml.SelectSingleNode("title").InnerText = group.Title;
                } else {// 分组改变
                    deleteFeedGroup(group);// 删除原分组

                    // 重新添加
                    IXmlNode groupXml = createFeedGroupXml(group);
                    groupsXml.AppendChild(groupXml);
                    AllGroups.Add(group);
                }
            }
            //// 保存XML数据文件
            //await feedsDoc.SaveToFileAsync(feedsFile);
            return group;
        }

        // 创建用于描述FeedGroup信息的XML节点
        private IXmlNode createFeedGroupXml(FeedGroup group) {
            // 创建分组XML节点
            XmlElement groupXml = feedsDoc.CreateElement("group");
            // 创建唯一编号节点
            XmlElement uniqueIdXml = feedsDoc.CreateElement("uniqueId");
            uniqueIdXml.InnerText = group.UniqueId;
            groupXml.AppendChild(uniqueIdXml);
            // 创建标题节点
            XmlElement titleXml = feedsDoc.CreateElement("title");
            titleXml.InnerText = group.Title;
            groupXml.AppendChild(titleXml);
            // 创建描述节点
            XmlElement desXml = feedsDoc.CreateElement("description");
            desXml.InnerText = group.Description;
            groupXml.AppendChild(desXml);
            // 创建Feed源集合节点
            XmlElement feedsXml = feedsDoc.CreateElement("feeds");
            groupXml.AppendChild(feedsXml);

            return groupXml;
        }

        /// <summary>
        /// 删除分组，并移动原分组中的Feed源到“未分组”中
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="group"></param>
        private void deleteFeedGroup(FeedGroup group) {
            IXmlNode groupsXml = feedsDoc.SelectSingleNode("groups");

            // 找到要删除的旧的FeedGroup信息
            IXmlNode oldGroupXml = feedsDoc.SelectSingleNode("//group[uniqueId='" + group.UniqueId + "']");

            // 移动所有分组中的Feed到未分组中，包括XML和对象
            IXmlNode noGroupXml = groupsXml.SelectSingleNode("//group[title='未分组']");
            IXmlNode noGroupFeedsXml = noGroupXml.SelectSingleNode("feeds");

            XmlNodeList oldGroupFeedXmlList = oldGroupXml.SelectNodes("feeds/feed");
            foreach (IXmlNode feedXml in oldGroupFeedXmlList) {// 移动Feed的XML信息到“未分组”
                noGroupFeedsXml.AppendChild(feedXml);
            }

            var matches = _feedDataSource.AllGroups.Where((inGroup) => inGroup.Title.Equals("未分组"));
            if (matches.Count() == 1) {
                FeedGroup inGroup = matches.First();
                foreach (Feed feed in group.Feeds) {// 移动Feed的对象信息到“未分组”
                    inGroup.Feeds.Add(feed);
                }
            }

            // 删除旧FeedGroup
            groupsXml.RemoveChild(oldGroupXml);
            AllGroups.Remove(group);
        }
    }
}
