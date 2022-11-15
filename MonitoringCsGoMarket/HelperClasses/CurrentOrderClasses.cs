using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringCsGoMarket.HelperClasses
{
    public class Root
    {
        public bool success { get; set; }
        public int time { get; set; }
        public string currency { get; set; }
        public Dictionary<string, Item> items { get; set; } = new();
    }
    public class Item
    {
        public decimal price { get; set; }
        public decimal buy_order { get; set; }
        public decimal? avg_price { get; set; }
        public string popularity_7d { get; set; }
        public string market_hash_name { get; set; }
        public string ru_name { get; set; }
        public string ru_rarity { get; set; }
        public string ru_quality { get; set; }
        public string text_color { get; set; }
        public string bg_color { get; set; }
    }

    public class SoloItemPrice
    {
        public bool success { get; set; }
        public string best_offer { get; set; }
    }
}
