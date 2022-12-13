using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringCsGoMarket.HelperClasses
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class BuyOffers
    {
        public int best_offer { get; set; }
    }

    public class Info
    {
        public object our_market_instanceid { get; set; }
        public string market_name { get; set; }
        public string name { get; set; }
        public string market_hash_name { get; set; }
        public string rarity { get; set; }
        public string quality { get; set; }
        public string type { get; set; }
        public string mtype { get; set; }
        public string slot { get; set; }
    }

    public class Result
    {
        public string classid { get; set; }
        public string instanceid { get; set; }
        public SellOffers sell_offers { get; set; }
        public BuyOffers buy_offers { get; set; }
        public object history { get; set; }
        public Info info { get; set; }
    }

    public class Root2
    {
        public bool success { get; set; }
        public List<Result> results { get; set; }
    }

    public class SellOffers
    {
        public int best_offer { get; set; }
    }


}
