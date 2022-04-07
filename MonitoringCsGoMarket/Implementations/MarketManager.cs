using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using StateKeeperMonitoringApp;
using MonitoringMarket.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace MonitoringMarket.Implementations
{
	internal static class MarketManager
	{
		#region Технические данные
		private static IConfigurationRoot _appSettings = new ConfigurationBuilder().AddJsonFile("appSettings.json").Build();
		private static ConcurrentDictionary<string, decimal> _currentShoppingList = new ConcurrentDictionary<string, decimal>();
		private static ConcurrentDictionary<string, decimal> _currentShoppingBlockList = new ConcurrentDictionary<string, decimal>();
		private static int countPages = 0;
		private static IUserInteractionManager _userManager = new ConsoleUserInteractionManager();
		private static IStateKeeperMonitoringApp _stateKeeperMonitoringApp = new StateKeeperMonitoringAppToDB(_appSettings["dbConnection"]);
		#endregion
		#region Конфигурация покупок
		/// <summary>
		/// Режим тестирования (Дополнительные вывод ы консоль и стоимость покупки = 0.1)
		/// </summary>
		private static bool _testMode = bool.Parse(_appSettings["testMode"]);
		/// <summary>
		/// Задержка проверки стоимость товара при поиске
		/// </summary>
		private static int _delayAtCheckItem = int.Parse(_appSettings["delayAtCheckItem"]);
		/// <summary>
		/// Задержка актуализации стоимость текущих покупок
		/// </summary>
		private static int _delayAtMonitoringMarket = int.Parse(_appSettings["delayAtMonitoringMarket"]);
		/// <summary>
		/// Процент вычитаемый  от прибыли
		/// </summary>
		private static int _commissionPercentage = int.Parse(_appSettings["commissionPercentage"]);
		/// <summary>
		/// Профит в рублях
		/// </summary>
		private static int _acceptableProfit = int.Parse(_appSettings["acceptableProfit"]);
		/// <summary>
		/// Текущий баланс в рублях
		/// </summary>
		private static int _currentMoney = int.Parse(_appSettings["currentMoney"]);
		/// <summary>
		/// Минимальная стоимость товара
		/// </summary>
		private static int _minMoney = int.Parse(_appSettings["minMoney"]);
		/// <summary>
		/// Минимальная кол-вол предметов сейчас напродаже
		/// </summary>
		private static int _minCountItem = int.Parse(_appSettings["minCountItem"]);
		#endregion
		#region Конфигурация маркета
		private static StringBuilder _mainUrlPage = new StringBuilder("");
		private static StringBuilder _subUrlPages = new StringBuilder($"?t=all&p=#pagenumber#&rs={_minMoney};{_currentMoney*2}&sd=desc");
		private static StringBuilder _mainUrlBuy = new StringBuilder("");
		private static NumberFormatInfo _priceSplitSeparator = new NumberFormatInfo { NumberDecimalSeparator = "." };
		private static Dictionary<string, string> _replacСharsInType = new Dictionary<string, string> { 
			{ "ё", "е" } 
		};
		#endregion


		internal static void SearchingItemsMarket()
		{
			RestoreStateApp();
			_userManager.SendUserMessage("Start searching items market");
			while (true)
			{
				foreach (var linkItem in GetAllLinksByItem())
				{
					CheckItem(linkItem, _delayAtCheckItem);
				}
			}
		}

		internal static void MonitoringСurrentShoppingList()
		{
			while (true)
			{
				Thread.Sleep(_delayAtMonitoringMarket);
				if (_testMode) _userManager.SendUserMessage($"Run MonitoringСurrentShoppingList. Count items in list = {_currentShoppingList.Count()}");
				foreach (var key in _currentShoppingList.Keys)
				{
					CheckItem(new StringBuilder(key), _delayAtCheckItem);
				}
				SaveStateApp();
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
			HD = web.Load(new StringBuilder().Append(_mainUrlPage).Append(_subUrlPages).Replace("#pagenumber#", "1").ToString());
			int.TryParse(HD.DocumentNode.SelectSingleNode("//span[@id='total_pages']").InnerText, out countPages);
			for (int i = 1; i <= countPages; i++)
			{
				HD = web.Load(new StringBuilder().Append(_mainUrlPage).Append(_subUrlPages).Replace("#pagenumber#", i.ToString()).ToString());
				var applications = HD.GetElementbyId("applications");
				var childNodes = applications.ChildNodes;
				foreach (var childNode in childNodes)
				{
					var href = childNode.GetAttributeValue("href", string.Empty);
					if (href != string.Empty)
					{
						yield return new StringBuilder().Append(_mainUrlPage).Append(href);
					}
				}
			}
		}

		private static void CheckItem(StringBuilder linkItem, int _delayAtCheckItem = 0)
		{
			Thread.Sleep(_delayAtCheckItem);
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
			if (_currentShoppingBlockList.ContainsKey(linkItem.ToString().ToLower())) return;
			if (!_currentShoppingList.ContainsKey(linkItem.ToString().ToLower()))
			{
				_userManager.SendUserMessage("");
				_userManager.SendUserMessage(linkItem.ToString());
				_userManager.SendUserMessage($"maxCost = {maxCostAutoBuy}");
				_userManager.SendUserMessage($"CurMinCost = {curMinCost}");

				if(_testMode == false)
				{
					if (!GetPermissionFromAdmin()) 
					{
						bool ItemAddedBlock = false;
						while (!ItemAddedBlock)
						{
							ItemAddedBlock = _currentShoppingBlockList.TryAdd(linkItem.ToString().ToLower(), maxCostAutoBuy + 0.01m);
						}
						_userManager.SendUserMessage("Предмет добавлен в блок");
						return;
					}
				}

				CreateBuyItem(linkItem, maxCostAutoBuy + 0.01m);
				_userManager.SendUserMessage("Предмет добавлен в список покупок");
				bool ItemAdded = false;
				while (!ItemAdded)
				{
					ItemAdded = _currentShoppingList.TryAdd(linkItem.ToString().ToLower(), maxCostAutoBuy + 0.01m);
				}
			}
			else
			{
				if (_currentShoppingList[linkItem.ToString().ToLower()] < maxCostAutoBuy)
				{
					CreateBuyItem(linkItem, maxCostAutoBuy + 0.01m);
					_currentShoppingList[linkItem.ToString().ToLower()] = maxCostAutoBuy + 0.01m;
				}
			}
		}

		private static bool GetPermissionFromAdmin()
		{
			_userManager.SendUserMessage("Разрешить ли покупку данного предмета? yes - да, else - нет");
			_userManager.NotifyUser();
			var userMessage = _userManager.GetUserMessage();
			if (userMessage ==  "yes")
			{
				return true;
			}
			else
			{
				return false;
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
				curentType == "" ||
				linkItem.ToString().ToLower().Contains("") ||
				_minMoney > curMinCost  ||
				linkItem.ToString().Contains("")
				) return false;
			decimal purchaseProfit = (curMinCost - (curMinCost / 100 * _commissionPercentage) - maxCostAutoBuy);
			if (purchaseProfit < _acceptableProfit || _currentMoney < maxCostAutoBuy) return false;
			#endregion

			return true;
		}

		private static void CreateBuyItem(StringBuilder linkItem, decimal sum)
		{
			if (_testMode) sum = 0.1m;
			var splitUrl = linkItem.ToString().Substring(linkItem.ToString().IndexOf("item/") + 5).Split('-');
			var buyUrl = new StringBuilder()
				.Append(_mainUrlBuy)
				.Append(splitUrl[0])
				.Append("/")
				.Append(splitUrl[1])
				.Append($"/{sum.ToString().Replace(',', '.')}").ToString();
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(buyUrl);
			request.Accept = "application/json";
			request.ContentType = "application/json; charset=UTF-8";
			request.Headers.Add("", "");
			request.Headers.Add("", "");
			request.Method = "POST";

			WebResponse response = request.GetResponse();
			using (Stream dataStream = response.GetResponseStream())
			{
				StreamReader reader = new StreamReader(dataStream);
				string responseFromServer = reader.ReadToEnd();
				if (_testMode) _userManager.SendUserMessage(responseFromServer);
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
				foreach (var item in _replacСharsInType)
				{
					dataWear = dataWear.Replace(item.Key, item.Value);
				}
				if (dataWear == CurentType)
				{
					var button = sameItem.ChildNodes.Where(i => i.InnerHtml.Contains("Купить")).FirstOrDefault();
					if (button != null)
					{
						countItems++;
						var cost = decimal.Parse(button.ChildNodes.Where(i => i.Name == "button").FirstOrDefault().InnerText.Replace("\n", "").Replace(" ", "").Trim(), _priceSplitSeparator);
						if (minCost == null) minCost = cost;
						if (minCost > cost) minCost = cost;
					}
				}
			}
			if (minCost != null && minCost > 0 && countItems >= _minCountItem) return (decimal)minCost;
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
				foreach (var item in _replacСharsInType)
				{
					targetType = targetType.Replace(item.Key, item.Value);
				}
				if (new string[] {
					"",
				}.Contains(
				targetType))
				{
					CurentType = appearanceItem.InnerText.Replace("\n", "").Trim();
				}
			}
			foreach (var item in _replacСharsInType)
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
						var cost = decimal.Parse(bItem.InnerText.Replace("и менее", "").Replace(" ", "").Trim(), _priceSplitSeparator);
						if (maxCost < cost) maxCost = cost;
					}
				}
			}
			return maxCost;
		}

		private static void SaveStateApp()
		{
			_stateKeeperMonitoringApp.SetState(new MomentoMonitoringApp(_currentShoppingList, _currentShoppingBlockList));
		}

		private static void RestoreStateApp()
		{
			var state = _stateKeeperMonitoringApp.GetState();
			_currentShoppingList = state.currentShoppingList;
			_currentShoppingBlockList= state.currentShoppingBlockList;
		}
	}
}
