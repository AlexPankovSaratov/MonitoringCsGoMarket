using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringCsGoMarket.HelperClasses
{
    public class Order
    {
        public string hash_name { get; set; }
        public int count { get; set; }
        public string date { get; set; }
        public int price { get; set; }
        public string currency { get; set; }
        public object partner { get; set; }
        public object token { get; set; }
    }

    public class CurrentOrders
    {
        public bool success { get; set; }
        public List<Order> orders { get; set; }
    }

    public class Datum
    {
        public object id { get; set; }
        public string market_hash_name { get; set; }
        public int price { get; set; }
        public long @class { get; set; }
        public int instance { get; set; }
        public Extra extra { get; set; }
    }

    public class Extra
    {
        public string @float { get; set; }
        public string phase { get; set; }
    }

    public class Item
    {
        public bool success { get; set; }
        public string currency { get; set; }
        public List<Datum> data { get; set; }
    }
}
