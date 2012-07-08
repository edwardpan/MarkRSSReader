﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace MarkRSSReader.Data {
    /// <summary>
    /// RSS源抽象类，定义公共属性
    /// </summary>
    [Windows.Foundation.Metadata.WebHostHidden]
    public abstract class FeedCommon : MarkRSSReader.Common.BindableBase {
        private static Uri _baseUri = new Uri("ms-appx:///");

        public FeedCommon() { }
        public FeedCommon(String uniqueId, String title, String subtitle, String imagePath, String description) {
            this._uniqueId = uniqueId;
            this._title = title;
            this._subtitle = subtitle;
            this._description = description;
            this._imagePath = imagePath;
        }

        private string _uniqueId = string.Empty;
        public string UniqueId {
            get { return this._uniqueId; }
            set { this.SetProperty(ref this._uniqueId, value); }
        }

        private string _title = string.Empty;
        public string Title {
            get { return this._title; }
            set { this.SetProperty(ref this._title, value); }
        }

        private string _subtitle = string.Empty;
        public string Subtitle {
            get { return this._subtitle; }
            set { this.SetProperty(ref this._subtitle, value); }
        }

        private string _description = string.Empty;
        public string Description {
            get { return this._description; }
            set { this.SetProperty(ref this._description, value); }
        }

        private ImageSource _image = null;
        private String _imagePath = null;
        public ImageSource Image {
            get {
                if (this._image == null && this._imagePath != null) {
                    this._image = new BitmapImage(new Uri(FeedCommon._baseUri, this._imagePath));
                }
                return this._image;
            }

            set {
                this._imagePath = null;
                this.SetProperty(ref this._image, value);
            }
        }

        public void SetImage(String path) {
            this._image = null;
            this._imagePath = path;
            this.OnPropertyChanged("Image");
        }
    }
}
