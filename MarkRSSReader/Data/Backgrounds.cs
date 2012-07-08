using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkRSSReader.Data {
    public sealed class Backgrounds {
        
        private static Backgrounds _backgrounds = new Backgrounds();

        public static Backgrounds Instance {
            get { return _backgrounds; }
        }

        private ObservableCollection<string> _colors = new ObservableCollection<string>();

        public Backgrounds() {
            Random rand = new Random();
            int num = 0;
            for (int i = 0; i < 30; i++) {
                num = rand.Next(0, 200);
                string red = num.ToString("X");
                if (red.Length < 2) red = "0" + red;

                num = rand.Next(0, 200);
                string green = num.ToString("X");
                if (green.Length < 2) green = "0" + green;

                num = rand.Next(0, 200);
                string blue = num.ToString("X");
                if (blue.Length < 2) blue = "0" + blue;

                string color = "#" + red + green + blue;
                _colors.Add(color);
            }
        }

        private int index = 0;

        public string Color {
            get {
                //Random rand = new Random();
                //int i = rand.Next(_colors.Count);
                //return _colors.ElementAt(i);
                if (index == _colors.Count) index = 0;
                string color = _colors.ElementAt(index);
                index++;
                return color;
            }
        }
    }
}
