using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace MonitoringCsGoMarket
{
	internal static class MarketManager
	{
		private static IConfigurationRoot appSettings = new ConfigurationBuilder().AddJsonFile("appSettings.json").Build();
		/// <summary>
		/// Режим тестирования (Дополнительные вывод ы консоль и стоимость покупки = 0.1)
		/// </summary>
		private static bool testMode = bool.Parse(appSettings["testMode"]);
		#region Конфигурация покупок
		/// <summary>
		/// Задержка проверки стоимость товара при поиске
		/// </summary>
		private static int delayAtCheckItem = int.Parse(appSettings["delayAtCheckItem"]);
		/// <summary>
		/// Задержка актуализации стоимость текущих покупок
		/// </summary>
		private static int delayAtMonitoringMarket = int.Parse(appSettings["delayAtMonitoringMarket"]);
		/// <summary>
		/// Процент вычитаемый  от прибыли
		/// </summary>
		private static int commissionPercentage = int.Parse(appSettings["commissionPercentage"]);
		/// <summary>
		/// Профит в рублях
		/// </summary>
		private static int acceptableProfit = int.Parse(appSettings["acceptableProfit"]);
		/// <summary>
		/// Текущий баланс в рублях
		/// </summary>
		private static int currentMoney = int.Parse(appSettings["currentMoney"]);
		/// <summary>
		/// Минимальная стоимость товара
		/// </summary>
		private static int minMoney = int.Parse(appSettings["minMoney"]);
		/// <summary>
		/// Минимальная кол-вол предметов сейчас напродаже
		/// </summary>
		private static int minCountItem = int.Parse(appSettings["minCountItem"]);
		#endregion

		#region Конфигурация маркета
		private static StringBuilder mainUrlPage = new StringBuilder("https://market.csgo.com");
		private static StringBuilder subUrlPages = new StringBuilder($"?t=all&p=#pagenumber#&rs={minMoney};{currentMoney*2}&sd=desc");
		private static StringBuilder mainUrlBuy = new StringBuilder("https://market.csgo.com/orders/insert/");
		private static NumberFormatInfo priceSplitSeparator = new NumberFormatInfo { NumberDecimalSeparator = "." };
		private static Dictionary<string, string> replacСharsInType = new Dictionary<string, string> { 
			{ "ё", "е" } 
		};
		#endregion

		#region Технические данные
		private static ConcurrentDictionary<string, decimal> currentShoppingList = new ConcurrentDictionary<string, decimal>();
		private static int countPages = 0;
		#endregion


		internal static void SearchingItemsMarket()
		{
			SendMessage("Start searching items market");
			while (true)
			{
				foreach (var linkItem in GetAllLinksByItem())
				{
					CheckItem(linkItem, delayAtCheckItem);
				}
			}
		}

		internal static void MonitoringСurrentShoppingList()
		{
			while (true)
			{
				Thread.Sleep(delayAtMonitoringMarket);
				if (testMode) SendMessage($"Run MonitoringСurrentShoppingList. Count items in list = {currentShoppingList.Count()}");
				foreach (var key in currentShoppingList.Keys)
				{
					CheckItem(new StringBuilder(key), delayAtCheckItem);
				}
			}
		}

		private static IEnumerable<StringBuilder> GetAllLinksByItem()
		{
			HtmlDocument HD = new HtmlDocument();
			var web = new HtmlWeb
			{
				AutoDetectEncoding = false,
				OverrideEncoding = Encoding.UTF8,
			};
			HD = web.Load(new StringBuilder().Append(mainUrlPage).Append(subUrlPages).Replace("#pagenumber#", "1").ToString());
			int.TryParse(HD.DocumentNode.SelectSingleNode("//span[@id='total_pages']").InnerText, out countPages);
			for (int i = 1; i <= countPages; i++)
			{
				HD = web.Load(new StringBuilder().Append(mainUrlPage).Append(subUrlPages).Replace("#pagenumber#", i.ToString()).ToString());
				var applications = HD.GetElementbyId("applications");
				var childNodes = applications.ChildNodes;
				foreach (var childNode in childNodes)
				{
					var href = childNode.GetAttributeValue("href", string.Empty);
					if (href != string.Empty)
					{
						yield return new StringBuilder().Append(mainUrlPage).Append(href);
					}
				}
			}
		}

		private static void CheckItem(StringBuilder linkItem, int delayAtCheckItem = 0)
		{
			Thread.Sleep(delayAtCheckItem);
			HtmlDocument HD = new HtmlDocument();
			var web = new HtmlWeb
			{
				AutoDetectEncoding = false,
				OverrideEncoding = Encoding.UTF8,
			};
			HD = web.Load(linkItem.ToString());
			decimal maxCostAutoBuy = GetMaxCostAutoBuy(HD);
			string curentType = GetCurentType(HD);
			decimal curMinCost = GetCurMinCost(HD, curentType);
			if (!NeedAddItemToCurrentShoppingList(linkItem, maxCostAutoBuy, curentType, curMinCost)) return;
			if (!currentShoppingList.ContainsKey(linkItem.ToString().ToLower()))
			{
				SendMessage(linkItem.ToString());
				SendMessage($"maxCost = {maxCostAutoBuy}");
				SendMessage($"CurMinCost = {curMinCost}");

				CreateBuyItem(linkItem, maxCostAutoBuy + 0.01m);
				bool ItemAdded = false;
				while (!ItemAdded)
				{
					ItemAdded = currentShoppingList.TryAdd(linkItem.ToString().ToLower(), maxCostAutoBuy + 0.01m);
				}
			}
			else
			{
				if (currentShoppingList[linkItem.ToString().ToLower()] < maxCostAutoBuy)
				{
					CreateBuyItem(linkItem, maxCostAutoBuy + 0.01m);
					currentShoppingList[linkItem.ToString().ToLower()] = maxCostAutoBuy + 0.01m;
				}
			}
		}

		private static bool NeedAddItemToCurrentShoppingList(StringBuilder linkItem, decimal maxCostAutoBuy, string curentType, decimal curMinCost)
		{
			if (
			#region Технически не корректно
				curentType == "" ||
				maxCostAutoBuy == 0 ||
				curMinCost == 0 ||
			#endregion

			#region Бизнесово отказывемся
				curentType == "Граффити" ||
				linkItem.ToString().ToLower().Contains("stattrak") ||
				minCountItem < maxCostAutoBuy
				) return false;
			decimal purchaseProfit = (curMinCost - (curMinCost / 100 * commissionPercentage) - maxCostAutoBuy);
			if (purchaseProfit < acceptableProfit || currentMoney < maxCostAutoBuy) return false;
			#endregion

			return true;
		}

		private static void CreateBuyItem(StringBuilder linkItem, decimal sum)
		{
			if (testMode) sum = 0.1m;
			var splitUrl = linkItem.ToString().Substring(linkItem.ToString().IndexOf("item/") + 5).Split('-');
			var buyUrl = new StringBuilder()
				.Append(mainUrlBuy)
				.Append(splitUrl[0])
				.Append("/")
				.Append(splitUrl[1])
				.Append($"/{sum.ToString().Replace(',', '.')}").ToString();
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(buyUrl);
			request.Accept = "application/json";
			request.ContentType = "application/json; charset=UTF-8";
			request.Headers.Add("Cookie", "PHPSESSID=117336686%3Ali6ru5ldev7eaarej5ntcbfjqk; _csrf=926LXy15cNvN1sWv9QqZ9Mmnl_Tl5MDS; goon=0; d2mid=p0Why2Z1jHHlfUKQdxFG4gar757Gky; chr1=y; _ym_isad=2; _ym_d=1647607115; _gid=GA1.2.859436637.1647607116; _ym_visorc=w; _ga=GA1.2.1748404017.1647607116; _fbp=fb.1.1647607117182.1463398233; _ym_uid=1647607115548231279");
			request.Headers.Add("X-CSRF-TOKEN", "OLxroo7KTmdgH7XLsARar-afYyMA2WDtf4nfTqKRzKgBjl3u1rN_UgNRw4WBdw3Z384SeTmUDYMT1osil9yI-w==");
			request.Method = "POST";

			WebResponse response = request.GetResponse();
			using (Stream dataStream = response.GetResponseStream())
			{
				StreamReader reader = new StreamReader(dataStream);
				string responseFromServer = reader.ReadToEnd();
				if (testMode) SendMessage(responseFromServer);
			}
		}

		private static decimal GetCurMinCost(HtmlDocument HD, string CurentType)
		{
			var countItems = 0;
			decimal? minCost = null;
			HtmlNode sameitemsList = null;
			try
			{
				sameitemsList = HD.DocumentNode.SelectNodes("//self::node()[@class='sameitems-list']").FirstOrDefault();
			}
			catch (Exception)
			{
				return 0;
			}
			if (sameitemsList == null) return 0;
			foreach (var sameItem in sameitemsList.ChildNodes)
			{
				var dataWear = sameItem.GetAttributeValue("data-wear", string.Empty);
				foreach (var item in replacСharsInType)
				{
					dataWear = dataWear.Replace(item.Key, item.Value);
				}
				if (dataWear == CurentType)
				{
					var button = sameItem.ChildNodes.Where(i => i.InnerHtml.Contains("Купить")).FirstOrDefault();
					if (button != null)
					{
						countItems++;
						var cost = decimal.Parse(button.ChildNodes.Where(i => i.Name == "button").FirstOrDefault().InnerText.Replace("\n", "").Replace(" ", "").Trim(), priceSplitSeparator);
						if (minCost == null) minCost = cost;
						if (minCost > cost) minCost = cost;
					}
				}
			}
			if (minCost != null && minCost > 0 && countItems >= minCountItem) return (decimal)minCost;
			return 0;
		}

		private static string GetCurentType(HtmlDocument HD)
		{
			string CurentType = "";
			var appearanceItems = HD.DocumentNode.SelectNodes("//self::node()[@class='item-appearance']");
			if (appearanceItems == null) return "";
			foreach (var appearanceItem in appearanceItems)
			{
				var targetType = appearanceItem.InnerText.Replace("\n", "").Trim();
				foreach (var item in replacСharsInType)
				{
					targetType = targetType.Replace(item.Key, item.Value);
				}
				if (new string[] {
					"Закаленное в боях",
					"Поношенное",
					"После полевых испытаний",
					"Немного поношенное",
					"Прямо с завода",
				}.Contains(
				targetType))
				{
					CurentType = appearanceItem.InnerText.Replace("\n", "").Trim();
				}
			}
			foreach (var item in replacСharsInType)
			{
				CurentType = CurentType.Replace(item.Key, item.Value);
			}
			return CurentType;
		}

		private static decimal GetMaxCostAutoBuy(HtmlDocument HD)
		{
			decimal maxCost = 0;
			var allRectanglestats = HD.DocumentNode.SelectNodes("//self::node()[@class='rectanglestats']");
			if (allRectanglestats == null) return 0;
			foreach (var rectanglestats in allRectanglestats)
			{
				foreach (var rectanglestat in rectanglestats.ChildNodes.Where(i => i.InnerText.Contains("запро") && !i.InnerText.Contains("Всего запросов")))
				{
					foreach (var bItem in rectanglestat.ChildNodes.Where(i => i.Name == "b"))
					{
						var cost = decimal.Parse(bItem.InnerText.Replace("и менее", "").Replace(" ", "").Trim(), priceSplitSeparator);
						if (maxCost < cost) maxCost = cost;
					}
				}
			}
			return maxCost;
		}
		private static void SendMessage(string message)
		{
			Console.WriteLine(message);
		}
	}
}
