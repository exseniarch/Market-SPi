using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using System.Globalization;
using System.Data.OleDb;
using System.Data;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using System.Net;
using RestSharp;
using System.ComponentModel;
using System.Windows.Threading;
using System.Threading;
using System.Collections;
using MFIWPF;

namespace Spi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string SelectedAsset = "";
        private bool SignalSettingsActive = false;

        private int DaysToGoBack = 30;
        private int FaultyRecords = 0;
        private decimal FlowRate;
        private decimal CoBuyPercentage = 10;
        private decimal CoSellPercentage = 10;
        private bool CountsWorkDays = false;
        private bool SmartMoneyIn = true;
        private bool CompareSides = false;
        private bool CompareLastNClosing = false;
        private bool ControlLastPricePercentage = false;
        private bool LoadEveryAsset = false;
        
        private bool ListPriorityAssets= false;
        private bool ListInvestmentFunds = false;
        private bool ListInvestmentCos = false;

        private bool ConsiderVolume = true;
        private List<Asset> AssetList = new List<Asset>();
        private BackgroundWorker backgroundWorker_Loader = new BackgroundWorker();
        private List<SmartMoney> SmartMoneyTotal = new List<SmartMoney>();
        private List<SmartMoney>[] SmartMoneyTotalPeriods;// = new ArrayList();
        private string[] PeriodsLabel;
        private int SmartMoneyPeriodCount = 1;
        private List<MoneyFlow> DailyMoneyFlow = new List<MoneyFlow>();



        #region Signal Parameters
        private int SignalRepeat { get; set; }// = 3;
        private decimal SignalBriefing = 3;
        private decimal SignalBuyerStrength = (decimal)1.5;
        private decimal SignalVolumeToAverage = 2;
        private decimal SignalShareOfFreePublic = 0;
        private int SignalBuyerCount = 1;
        private decimal SignalCoBuy = 0;
        private decimal SignalCoSell = 0;
        private decimal SignalCoBuyerGreatness = 0;
        private decimal SignalBasisVolumeGreatness = 0;

        private bool BriefingGreater = true;
        private bool BuyerStrengthGreater = true;
        private bool ShareOfFreePublic = true;
        private bool BuyerCountGreater = true;
        private bool CoBuyCompareGreater = false;
        private bool CoSellCompareGreater = false;
        private bool CoBuyerGreater = false;
        private bool BasisVolumeSmaller = false;
        #endregion Signal Parameters

        #region Smart Signal Parameters
        private bool BriefingToBuyerStrengthIsGreater = true;
        private bool PerCapitaBuyValueIsGreater = true;

        private decimal PerCapitaBuyValue = 0;
        private decimal BriefingToBuyerStrength = 0;
        private decimal BriefingToBuyerStrengthDiff = 0;
        #endregion Smart Signal Parameters

        private string ListDisplayDate = "";
        //ListCollectionView SmartMoneyTotal;
        private int Progress = 0;
        private bool ProgressStopped = false;

        private bool ColorDefine = false;

        private bool ConsiderPtoE { get; set; }
        private decimal PtoEMax { get; set; }
        private string SourceArenaToken { get; set; }

        private decimal ConsideredMarketGrowth { get; set; }

        public Months[] PersianMonths = { new Months( "فروردین", "01" ),
                new Months( "اردیبهشت", "02" ),
                new Months( "خرداد", "03" ),
                new Months( "تیر", "04" ),
                new Months( "مرداد", "05" ),
                new Months( "شهریور", "06" ),
                new Months( "مهر", "07" ),
                new Months( "آبان", "08" ),
                new Months( "آذر", "09" ),
                new Months( "دی", "10" ),
                new Months( "بهمن", "11" ),
                new Months( "اسفند", "12" )
            };

        private SolidColorBrush[] RateSpectrum = new SolidColorBrush[] {
                    new SolidColorBrush(Color.FromArgb(150, 6, 74, 66)),//Heaviest
                    new SolidColorBrush(Color.FromArgb(150, 19, 142, 191)),
                    new SolidColorBrush(Color.FromArgb(150, 63, 170, 169)),
                    new SolidColorBrush(Color.FromArgb(150, 146, 210, 199)),
                    new SolidColorBrush(Color.FromArgb(150, 196, 235, 228)),//Lightest
                };
        
        public MainWindow()
        {
            InitializeComponent();

            FlowRate = decimal.Parse(TextBoxDifferance.Text);
            //DataGridSmartMoney.ItemsSource = (new ObservableCollection<SmartMoney>(SmartMoneyTotal));
            comboboxYear.Items.Add(DateTools.PersianToday().Substring(0, 4));
            comboboxYear.Items.Add(int.Parse(DateTools.PersianToday().Substring(0, 4)) - 1);

            comboboxMonth.ItemsSource = (new ObservableCollection<Months>(PersianMonths));

            for (int Day = 1; Day <= 31; Day++)
                comboboxDay.Items.Add(Day.ToString("00"));

            comboboxYear.SelectedValue = DateTools.PersianToday().Split('/')[0];
            comboboxMonth.SelectedValue = DateTools.PersianToday().Split('/')[1];
            comboboxDay.SelectedValue = DateTools.PersianToday().Split('/')[2];

            SourceArenaToken = WindowToken.ReadToken();
        }
        
        private void BackgroundWorker_Loader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var SortedAssetList = from item in AssetList orderby item.Name select item;
            ComboBoxAsset.ItemsSource = (new ObservableCollection<Asset>(SortedAssetList));
            grid_Wait.Visibility = Visibility.Collapsed;
            LabelLoadedCompanies.Content = "نمادها: " + AssetList.Count();
            if (AssetList.Count() == 0)
                MessageBox.Show("پاسخی از ارایه کننده سرویس دریافت نشد", "اخطار", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void BackgroundWorker_Loader_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int ProgressArguments = (int)e.ProgressPercentage;
            if (ProgressArguments == 1)
            {
                GridAnalysisStatus.Visibility = Visibility.Collapsed;
                labelAssetCounter.Content = e.UserState;
                progressbarRetrieval.Value = progressbarRetrieval.Value >= progressbarRetrieval.Maximum ? 0 : progressbarRetrieval.Value + 1;
            }

            if (ProgressArguments == 2)
            {
                GridAnalysisStatus.Visibility = Visibility.Visible;
                labelDataRetrieval.Content = " دریافت از پایگاه داده ... " + e.UserState;
                // progressbarRetrieval.Maximum 
                progressbarRetrieval.Value = progressbarRetrieval.Value >= AssetList.Count ? 0 : progressbarRetrieval.Value + 1;
            }

            if (ProgressArguments == 3)
            {
                GridAnalysisStatus.Visibility = Visibility.Visible;
                LabelDataAnalysis.Content = "تحلیل داده ها..." + e.UserState;
                progressbarAnalysis.Value = progressbarAnalysis.Value >= progressbarAnalysis.Maximum ? 0 : progressbarAnalysis.Value + 1;
            }
            if (ProgressArguments == 4)
            {
                GridAnalysisStatus.Visibility = Visibility.Visible;
                LabelDataAnalysis.Content = "فیلتر: " + e.UserState;
                progressbarAnalysis.Value = progressbarAnalysis.Value >= progressbarAnalysis.Maximum ? 0 : progressbarAnalysis.Value + 1;
            }
            //if (ProgressArguments == 4)
            //    LabelWhereAmI.Content = e.UserState;
        }

        private void BackgroundWorker_Loader_DoWork(object sender, DoWorkEventArgs e)
        {
            GetAssetList(sender);
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadFreshWindow();            
        }

        private void LoadFreshWindow()
        {
            LoadLocalData();
            backgroundWorker_Loader = new BackgroundWorker();
            grid_Wait.Visibility = Visibility.Visible;
            backgroundWorker_Loader.DoWork += BackgroundWorker_Loader_DoWork;
            backgroundWorker_Loader.ProgressChanged += BackgroundWorker_Loader_ProgressChanged;
            backgroundWorker_Loader.WorkerReportsProgress = true;
            backgroundWorker_Loader.WorkerSupportsCancellation = true;
            backgroundWorker_Loader.RunWorkerCompleted += BackgroundWorker_Loader_RunWorkerCompleted;

            backgroundWorker_Loader.RunWorkerAsync();
        }

        private void LoadLocalData()
        {
            LoadLastSettings();
        }

        public void GetAssetList(object sender)
        {
            GetOnLine(sender);
            //GetOffLine();
        }

        private void GetOnLine(object sender)
        {
            try
            {
                var client = new RestClient("https://sourcearena.ir/api/?token=" + SourceArenaToken + "&all&type=" + (LoadEveryAsset ? "2" : "0"));
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                IRestResponse response = client.Execute(request);
                LoadData(sender, response.Content.ToString());
            }   
            catch (WebException exp)
            {
                MessageBox.Show("اختلال در دسترسی به بانک داده ها\nبا زدن دکمه بازنشانی یا اجرای مجدد برنامه\nدوباره تلاش کنید", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void GetAssetHistory(object sender, Asset LoadedAsset)
        {
            int Position = 0;
            int Correction = 0;
            List<Asset> History = new List<Asset>();
            
            try
            {
                var client = new RestClient("https://sourcearena.ir/api/?token=" + SourceArenaToken + "&name=" + LoadedAsset.Name);
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                IRestResponse response = client.Execute(request);
                if (response.StatusCode.ToString() == "OK")
                {
                    Asset LoadedRecord = LoadHistory(response.Content.ToString(), DateTools.PersianToday());
                    if (LoadedRecord != null)
                        History.Add(LoadedRecord);//[DaysToGoBack - Position++ - 1] = LoadedRecord;
                }

                for (int DayCounter = 1; (DayCounter < DaysToGoBack + Correction) && (DayCounter <= 360); DayCounter++)
                {                    
                    DateTime DateTimeRetrivalDay = DateTime.Now.Subtract(new TimeSpan(DayCounter, 0, 0, 0));
                    string RetrievalDate = DateTools.ADToJalali(DateTimeRetrivalDay.Year, DateTimeRetrivalDay.Month, DateTimeRetrivalDay.Day);
                    (sender as BackgroundWorker).ReportProgress(2, RetrievalDate);

                    client = new RestClient("https://sourcearena.ir/api/?token=" + SourceArenaToken + "&name=" + LoadedAsset.Name + "&time=" + RetrievalDate);
                    client.Timeout = -1;
                    request = new RestRequest(Method.GET);
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Ssl3;
                    response = client.Execute(request);
                    if (response.StatusCode.ToString() == "OK")
                    {
                        Asset LoadedRecord = LoadHistory(response.Content.ToString(), RetrievalDate);
                        if (LoadedRecord != null)
                            History.Add(LoadedRecord);//[DaysToGoBack - Position++ - 1] = LoadedRecord;
                        else
                        {
                            if (CountsWorkDays)
                            {
                                Correction++;
                                FaultyRecords++;
                            }
                        }
                    }
                    else if (CountsWorkDays)
                        Correction++;
                }
                LoadedAsset.History = new Asset[History.Count];
                LoadedAsset.History = History.ToArray<Asset>();
            }
            catch (WebException ex)
            {
                throw ex;
            }
        }

        public void GetAssetsDaily(object sender)
        {
            //int Position = 0;
            int Correction = 0;
            //List<Asset> History = new List<Asset>();

            try
            {
                LoadDayToAssets(sender, DateTools.PersianToday());

                for (int DayCounter = 1; (DayCounter < DaysToGoBack + Correction) && (DayCounter <= 360); DayCounter++)
                {
                    DateTime DateTimeRetrivalDay = DateTime.Now.Subtract(new TimeSpan(DayCounter, 0, 0, 0));
                    string RetrievalDate = DateTools.ADToJalali(DateTimeRetrivalDay.Year, DateTimeRetrivalDay.Month, DateTimeRetrivalDay.Day);
                    (sender as BackgroundWorker).ReportProgress(2, RetrievalDate);

                    LoadDayToAssets(sender, RetrievalDate);
                }
                //LoadedAsset.History = new Asset[History.Count];
                //LoadedAsset.History = History.ToArray<Asset>();
            }
            catch (WebException ex)
            {
                throw ex;
            }
        }

        private void LoadDayToAssets(object sender, string RetrievalDate)
        {
            string Result = "";
            if (File.Exists(RetrievalDate.Replace("/", "") + ".adi") && RetrievalDate.Replace("/", "").CompareTo(DateTools.PersianToday().Replace("/", "")) < 0 && File.GetCreationTime(RetrievalDate.Replace("/", "") + ".adi").TimeOfDay >= (new TimeSpan(14,0,0)))
            {
                StreamReader AssetDataInfo = new StreamReader(File.Open(RetrievalDate.Replace("/", "") + ".adi", FileMode.Open));
                Result = AssetDataInfo.ReadToEnd();
                AssetDataInfo.Close();                
                if (!Result.StartsWith("NoDATA"))// && !Result.Contains("request timeout or empty response"))
                {
                    ConvertToAssets(sender, RetrievalDate, Result);
                    return;
                }
            }

            var client = new RestClient("https://sourcearena.ir/api/?token=" + SourceArenaToken + "&all&type=" + (LoadEveryAsset ? "2" : "0") + (RetrievalDate.CompareTo(DateTools.PersianToday()) == 0 ? "" : "&time=" + RetrievalDate));
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; //SecurityProtocolType.Tls | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Ssl3;
            IRestResponse response = client.Execute(request);
            Result = response.Content.ToString();

            if (response.StatusCode.ToString() == "OK")
            {
                ConvertToAssets(sender, RetrievalDate, Result);
                WriteToADI(RetrievalDate, Result);
            }
            else
                WriteToADI(RetrievalDate, "NoDATA");
        }

        private void WriteToADI(string RetrievalDate, string FileContent)
        {
            //if (RetrievalDate.CompareTo("") == 0)
            //    RetrievalDate = DateTools.PersianToday();
            StreamWriter AssetDataInfo = new StreamWriter(File.Open(RetrievalDate.Replace("/", "") + ".adi", FileMode.OpenOrCreate));
            AssetDataInfo.Write(FileContent);
            AssetDataInfo.Close();
        }

        private void ConvertToAssets(object sender, string RetrievalDate, string Result)
        {
            int nCnt = 0;
            int AssetsNum = AssetList.Count();

            try
            {
                dynamic DayWholeAssets = JsonConvert.DeserializeObject(Result);

                foreach (var AssetObj in DayWholeAssets)
                {                    
                    Asset LoadedRecord = LoadHistory(AssetObj.ToString(), (RetrievalDate == "" ? DateTools.PersianToday() : RetrievalDate));

                    if (LoadedRecord != null)
                    {
                        (sender as BackgroundWorker).ReportProgress(1, "(" + nCnt++ + "/" + AssetsNum + ")   " + LoadedRecord.Name);
                        Asset CurrentAsset = AssetList.Find(Item => Item.Name == LoadedRecord.Name);

                        if (CurrentAsset != null)
                        {
                            
                            //Thread.Sleep(1);
                            List<Asset> AssetHistory = CurrentAsset.History == null ? new List<Asset>() : CurrentAsset.History.ToList<Asset>();
                            AssetHistory.Add(LoadedRecord);
                            CurrentAsset.History = AssetHistory.ToArray<Asset>();
                        }
                    }
                }

            }
            catch (Exception exp)
            {

            }
        }

        private void LoadData(object sender, string DataStream)
        {
            
            //try
            //{
            int RecordCount = 0;
            dynamic WholeAssets = JsonConvert.DeserializeObject(DataStream);

            foreach (dynamic AssetObj in WholeAssets)
            {
                //backgroundWorker_Loader.ReportProgress(100, 0);
                //(sender as BackgroundWorker).ReportProgress(100, 2);

                if ((!ListPriorityAssets && (AssetObj.name.ToString().EndsWith("ح") || (AssetObj.full_name.ToString().StartsWith("ح ") || AssetObj.full_name.ToString().Replace(" ", "").StartsWith("ح.")))) ||
                    (!ListInvestmentFunds && AssetObj.full_name.ToString().StartsWith("صندوق")) ||
                    (!ListInvestmentCos && (AssetObj.full_name.ToString().StartsWith(".شرکت س") || AssetObj.full_name.ToString().StartsWith(" شرکت س"))))
                    continue;

                (sender as BackgroundWorker).ReportProgress(1, RecordCount++ + "  " + AssetObj.name);
                //(sender as BackgroundWorker).ReportProgress(1, RecordCount++);
                //System.Threading.Thread.Sleep(1);

                Asset AssetItem = new Asset();
                AssetItem.Name = AssetObj.name;
                AssetItem.FullName = AssetObj.full_name + " - " + AssetObj.latin_name + " - " + AssetObj.type + ": " + AssetObj.subtype + " - " + AssetObj.market;
                AssetItem.AssetSign = AssetObj.namad_code;
                AssetItem.Date = DateTools.PersianToday();
                //AssetItem.Today = new Status();
                int EPS;
                AssetItem.EPS = (int.TryParse(AssetObj.eps.ToString(), out EPS) ? EPS : 0);
                AssetItem.PtoE = decimal.Parse(AssetObj["P:E"].ToString());
                AssetItem.High = int.Parse(AssetObj.daily_price_high.ToString().Split('.')[0]);
                AssetItem.Low = int.Parse(AssetObj.daily_price_low.ToString().Split('.')[0]);

                AssetItem.Open = int.Parse(AssetObj.first_price.ToString());
                AssetItem.Close = int.Parse(AssetObj.close_price.ToString());
                AssetItem.TradedCount = int.Parse(AssetObj.trade_number.ToString());
                AssetItem.TradedVolume = decimal.Parse(AssetObj.trade_volume.ToString());
                AssetItem.TradedValue = decimal.Parse(AssetObj.trade_value.ToString());

                AssetItem.StockCount = decimal.Parse(AssetObj.all_stocks.ToString());
                if (AssetObj.free_float.Value != "" && AssetObj.free_float.Value != null)
                    AssetItem.PublicFloat = decimal.Parse(AssetObj.free_float.ToString());
                AssetItem.BasisVolume = int.Parse(AssetObj.basis_volume.ToString());

                decimal Temp;
                AssetItem.CoBuyVolume = decimal.TryParse(AssetObj.co_buy_volume.ToString(), out Temp) ? Temp : 0;
                AssetItem.CoSellVolume = decimal.TryParse(AssetObj.co_sell_volume.ToString(), out Temp) ? Temp : 0;
                AssetItem.CoBuyCount = decimal.TryParse(AssetObj.co_buy_count.ToString(), out Temp) ? Temp : 0;
                AssetItem.CoSellCount = decimal.TryParse(AssetObj.co_sell_count.ToString(), out Temp) ? Temp : 0;
                AssetItem.RealBuyVolume = decimal.TryParse(AssetObj.real_buy_volume.ToString(), out Temp) ? Temp : 0;
                AssetItem.RealSellVolume = decimal.TryParse(AssetObj.real_sell_volume.ToString(), out Temp) ? Temp : 0;
                AssetItem.RealBuyCount = decimal.TryParse(AssetObj.real_buy_count.ToString(), out Temp) ? Temp : 0;
                AssetItem.RealSellCount = decimal.TryParse(AssetObj.real_sell_count.ToString(), out Temp) ? Temp : 0;

                AssetItem.RealSellValue = decimal.TryParse(AssetObj.real_sell_value.ToString(), out Temp) ? Temp : 0;
                AssetItem.RealBuyValue = decimal.TryParse(AssetObj.real_buy_value.ToString(), out Temp) ? Temp : 0;
                AssetItem.CoSellValue = decimal.TryParse(AssetObj.co_sell_value.ToString(), out Temp) ? Temp : 0;
                AssetItem.CoBuyValue = decimal.TryParse(AssetObj.co_buy_value.ToString(), out Temp) ? Temp : 0;

                AssetList.Add(AssetItem);
            }

            backgroundWorker_Loader.CancelAsync();
            //}
            //catch(Exception exp)
            //{
            //    backgroundWorker_Loader.CancelAsync();
            //    MessageBox.Show(exp.Message);
            //}
        }

        private Asset LoadHistory(string DataStream, string DataDate)
        {
            try
            {
                //if (DataStream.Contains("وکار"))
                //    MessageBox.Show("");
                //String.Format("T:{0}\nRB:{1} - RS:{2}\nCB:{3} - CS{4}"), AssetObj.trade_volume.ToString(), AssetObj.real_buy_volume.ToString(), AssetObj.real_sell_volume.ToString(), AssetObj.co_buy_volume.ToString(), AssetObj.co_sell_volume.ToString());
                Asset AssetHistoryDay = new Asset();
                dynamic AssetObj = JsonConvert.DeserializeObject(DataStream);

                if (AssetObj != null && decimal.Parse(AssetObj.trade_volume.ToString()) != 0)
                {
                    AssetHistoryDay.Name = AssetObj.name;
                    AssetHistoryDay.FullName = AssetObj.full_name + " - " + AssetObj.latin_name + " - " + AssetObj.type + ": " + AssetObj.subtype + " - " + AssetObj.market;
                    //AssetHistoryDay.AssetSign = AssetObj.namad_code;
                    AssetHistoryDay.Date = DataDate;

                    int EPS;
                    decimal Temp;
                    AssetHistoryDay.EPS = (int.TryParse(AssetObj.eps.ToString(), out EPS) ? EPS : 0);
                    AssetHistoryDay.PtoE = decimal.Parse(AssetObj["P:E"].ToString());
                    AssetHistoryDay.High = int.Parse(AssetObj.daily_price_high.ToString().Split('.')[0]);
                    AssetHistoryDay.Low = int.Parse(AssetObj.daily_price_low.ToString().Split('.')[0]);

                    AssetHistoryDay.Open = int.Parse(AssetObj.first_price.ToString());
                    AssetHistoryDay.Close = int.Parse(AssetObj.close_price.ToString());
                    AssetHistoryDay.Final = int.Parse(AssetObj.final_price.ToString());
                    
                    AssetHistoryDay.LastPricePercentage = decimal.TryParse(AssetObj.close_price_change_percent.ToString().Replace("%", "") , out Temp) ? Temp : 0; 

                    AssetHistoryDay.TradedCount = int.Parse(AssetObj.trade_number.ToString());
                    AssetHistoryDay.TradedVolume = decimal.Parse(AssetObj.trade_volume.ToString());
                    AssetHistoryDay.TradedValue = decimal.Parse(AssetObj.trade_value.ToString());

                    AssetHistoryDay.StockCount = decimal.Parse(AssetObj.all_stocks.ToString());
                    if(AssetObj.free_float.Value != "")
                        AssetHistoryDay.PublicFloat = decimal.Parse(AssetObj.free_float.ToString());
                    AssetHistoryDay.BasisVolume = int.Parse(AssetObj.basis_volume.ToString());
                                        
                    AssetHistoryDay.CoBuyVolume = decimal.TryParse(AssetObj.co_buy_volume.ToString(), out Temp) ? Temp : 0;
                    AssetHistoryDay.CoSellVolume = decimal.TryParse(AssetObj.co_sell_volume.ToString(), out Temp) ? Temp : 0;
                    AssetHistoryDay.CoBuyCount = decimal.TryParse(AssetObj.co_buy_count.ToString(), out Temp) ? Temp : 0;
                    AssetHistoryDay.CoSellCount = decimal.TryParse(AssetObj.co_sell_count.ToString(), out Temp) ? Temp : 0;
                    AssetHistoryDay.RealBuyVolume = decimal.TryParse(AssetObj.real_buy_volume.ToString(), out Temp) ? Temp : 0;
                    AssetHistoryDay.RealSellVolume = decimal.TryParse(AssetObj.real_sell_volume.ToString(), out Temp) ? Temp : 0;
                    AssetHistoryDay.RealBuyCount = decimal.TryParse(AssetObj.real_buy_count.ToString(), out Temp) ? Temp : 0;
                    AssetHistoryDay.RealSellCount = decimal.TryParse(AssetObj.real_sell_count.ToString(), out Temp) ? Temp : 0;

                    AssetHistoryDay.RealSellValue = decimal.TryParse(AssetObj.real_sell_value.ToString(), out Temp) ? Temp : 0;
                    AssetHistoryDay.RealBuyValue = decimal.TryParse(AssetObj.real_buy_value.ToString(), out Temp) ? Temp : 0;
                    AssetHistoryDay.CoSellValue = decimal.TryParse(AssetObj.co_sell_value.ToString(), out Temp) ? Temp : 0;
                    AssetHistoryDay.CoBuyValue = decimal.TryParse(AssetObj.co_buy_value.ToString(), out Temp) ? Temp : 0;
                }
                else
                    return null;

                return AssetHistoryDay;
            }
            catch(Exception exp)
            {
                FaultyRecords++;
                return null;
            }
        }

        //private void DataGridAssetList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    Asset AssetSelected = (Asset)DataGridAssetList.SelectedItem;
        //    GetAssetHistory(AssetSelected);
        //    ProcessAssetData(AssetSelected);
        //}

        private List<SmartMoney> ProcessAssetData(object sender, Asset SelectedAsset)
        {
            decimal TotalVolume = 0;
            foreach (Asset AssetHistory in SelectedAsset.History)
            {
                //(sender as BackgroundWorker).ReportProgress(4, "Total Vol");
                if (AssetHistory != null)
                    TotalVolume += AssetHistory.TradedVolume;
                else
                    return null;
            }
            decimal AverageVolume = (TotalVolume / SelectedAsset.History.Length);

            List<SmartMoney> SmartMoneyList = new List<SmartMoney>();

            foreach (Asset AssetHistory in SelectedAsset.History)
            {
                //(sender as BackgroundWorker).ReportProgress(4, "Money Flow Vol");
                MoneyFlow DateFlow = DailyMoneyFlow.Find(Item => Item.Date == AssetHistory.Date);
                if (DateFlow != null)
                {
                    DateFlow.TotalRealBuy += AssetHistory.RealBuyValue;
                    DateFlow.TotalRealSell += AssetHistory.RealSellValue;
                    DateFlow.Flow = DateFlow.TotalRealBuy - DateFlow.TotalRealSell;
                }
                else
                {
                    DateFlow = new MoneyFlow();
                    DateFlow.Date = AssetHistory.Date;
                    DateFlow.TotalRealBuy = AssetHistory.RealBuyValue;
                    DateFlow.TotalRealSell = AssetHistory.RealSellValue;
                    DateFlow.Flow = DateFlow.TotalRealBuy - DateFlow.TotalRealSell;
                    DailyMoneyFlow.Add(DateFlow);
                }

                //(sender as BackgroundWorker).ReportProgress(4, "DP Begin");
                (sender as BackgroundWorker).ReportProgress(3, AssetHistory.Date);
                SmartMoney SmartMoneyRecord = new SmartMoney();

                SmartMoneyRecord.AssetName = SelectedAsset.Name;
                SmartMoneyRecord.FullName = SelectedAsset.FullName;
                SmartMoneyRecord.Date = AssetHistory.Date;

                SmartMoneyRecord.PtoE = AssetHistory.PtoE;

                SmartMoneyRecord.High = SelectedAsset.High;
                SmartMoneyRecord.Low = SelectedAsset.Low;
                SmartMoneyRecord.Close = SelectedAsset.Close;
                SmartMoneyRecord.Open = SelectedAsset.Open;
                SmartMoneyRecord.Final = SelectedAsset.Final;
                SmartMoneyRecord.LastPricePercentage = SelectedAsset.LastPricePercentage;

                SmartMoneyRecord.RealBuyPart = AssetHistory.TradedVolume == 0 ? 0 : AssetHistory.RealBuyVolume * 100 / AssetHistory.TradedVolume;
                SmartMoneyRecord.RealSellPart = AssetHistory.TradedVolume == 0 ? 0 : AssetHistory.RealSellVolume * 100 / AssetHistory.TradedVolume;
                SmartMoneyRecord.CoBuyPart = AssetHistory.TradedVolume == 0 ? 0 : AssetHistory.CoBuyVolume * 100 / AssetHistory.TradedVolume;
                SmartMoneyRecord.CoSellPart = AssetHistory.TradedVolume == 0 ? 0 : AssetHistory.CoSellVolume * 100 / AssetHistory.TradedVolume;

                SmartMoneyRecord.RealBuyerNumber = AssetHistory.RealBuyCount;
                SmartMoneyRecord.RealSellerNumber = AssetHistory.RealSellCount;

                SmartMoneyRecord.RealBuyVolume = AssetHistory.RealBuyVolume;
                SmartMoneyRecord.RealSellVolume = AssetHistory.RealSellVolume;

                SmartMoneyRecord.RealBuyValue = AssetHistory.RealBuyValue;
                SmartMoneyRecord.RealSellValue = AssetHistory.RealSellValue;

                SmartMoneyRecord.RealInOut = SmartMoneyRecord.RealBuyValue - SmartMoneyRecord.RealSellValue;

                SmartMoneyRecord.BuyerSellerRatio = AssetHistory.RealSellCount == 0 ? 0 : AssetHistory.RealBuyCount / AssetHistory.RealSellCount;

                decimal PerCodeBuyValue = AssetHistory.RealBuyCount == 0 ? 0 : AssetHistory.RealBuyValue / AssetHistory.RealBuyCount;
                SmartMoneyRecord.RealPerCodeBuyValue = PerCodeBuyValue;
                decimal PerCodeSellValue = AssetHistory.RealSellCount == 0 ? 0 : AssetHistory.RealSellValue / AssetHistory.RealSellCount;
                SmartMoneyRecord.RealPerCodeSellValue = PerCodeSellValue;
                SmartMoneyRecord.RealBuyToSellValue = PerCodeSellValue == 0 ? 0 : PerCodeBuyValue / PerCodeSellValue; //ToDo: Check if this has correct parameters

                SmartMoneyRecord.TradedVolume = AssetHistory.TradedVolume;
                SmartMoneyRecord.TradedVolumePercent = (AssetHistory.TradedVolume * 100) / AssetHistory.StockCount;
                SmartMoneyRecord.StockCount = AssetHistory.StockCount;
                SmartMoneyRecord.PublicFloat = (AssetHistory.PublicFloat * AssetHistory.StockCount) / 100; 
                SmartMoneyRecord.PublicFloatPercentage = AssetHistory.PublicFloat;
                SmartMoneyRecord.BasisVolume = AssetHistory.BasisVolume;

                SmartMoneyRecord.VolumeGrowthRatio = AssetHistory.TradedVolume / AverageVolume;
                SmartMoneyRecord.AverageVolume = AverageVolume;
                SmartMoneyRecord.AverageVolumePercent = (AverageVolume * 100) / AssetHistory.StockCount;
                decimal PublicFloatCount = (AssetHistory.PublicFloat * AssetHistory.StockCount) / 100;
                SmartMoneyRecord.ShareOfPublicFloat = PublicFloatCount.Equals(decimal.Zero) ? decimal.Zero : (AssetHistory.RealBuyVolume * 100) / PublicFloatCount;

                decimal RealBuyers = (AssetHistory.RealBuyCount > 0 ? AssetHistory.RealBuyCount : 0);
                decimal RealSellers = (AssetHistory.RealSellCount > 0 ? AssetHistory.RealSellCount : 0);
                SmartMoneyRecord.BriefingStrength = RealSellers / (RealBuyers > 0 ? RealBuyers : 1);//(AssetHistory.RealBuyVolume / AssetHistory.RealBuyCount) / (AssetHistory.RealSellVolume / AssetHistory.RealSellCount);

                decimal PerCodeBuyVolume = (AssetHistory.RealBuyCount > 0 ? AssetHistory.RealBuyVolume / AssetHistory.RealBuyCount : 0);
                decimal PerCodeSellVolume = (AssetHistory.RealSellCount > 0 ? AssetHistory.RealSellVolume / AssetHistory.RealSellCount : 0);
                SmartMoneyRecord.RealPerCodeBuyVolume = PerCodeBuyVolume;
                SmartMoneyRecord.RealPerCodeSellVolume = PerCodeSellVolume;
                SmartMoneyRecord.RealBuyToSellVolume = PerCodeBuyVolume / (PerCodeSellVolume > 0 ? PerCodeSellVolume : 1);//(AssetHistory.RealBuyVolume / AssetHistory.RealBuyCount) / (AssetHistory.RealSellVolume / AssetHistory.RealSellCount);

                SmartMoneyRecord.MarketCapital = AssetHistory.Close * AssetHistory.StockCount;
                //SmartMoneyRecord.Density = (AssetHistory.PtoE > 0 && SmartMoneyRecord.RealBuyToSellVolume > 0) ? (SmartMoneyRecord.BriefingStrength / SmartMoneyRecord.RealBuyToSellVolume) / AssetHistory.PtoE : -1;

                SmartMoneyList.Add(SmartMoneyRecord);
                //(sender as BackgroundWorker).ReportProgress(4, "DP End");
            }

            return SmartMoneyList;
        }
        //ToDO: Whole time money flow by days also can be calculated
        
        private void TextBoxHistoryDays_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(TextBoxHistoryDays.Text != "")
                DaysToGoBack = int.Parse(TextBoxHistoryDays.Text);
        }

        private void ComboBoxAsset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (ComboBoxAsset.SelectedIndex >= 0)
            //    checkboxSingleAsset.IsChecked = true;
            //else
            //    checkboxSingleAsset.IsChecked = false;
            if (ComboBoxAsset.SelectedIndex >= 0)
                SelectedAsset = ComboBoxAsset.SelectedValue.ToString();
            else
                SelectedAsset = "";

            Filtrate(true);
        }

        public DispatcherTimer dispatcherTimer;

        private void ButtonInvestigate_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show(SmartMoneyTotal.Count.ToString());

            if (ComboBoxAsset.Items.Count == 0)
                MessageBox.Show("لیست نمادها بارگیری نشده، پس از اطمینان از اتصال اینترنت و قطع بودن وی پی ان، بازنشانی کنید", "خطا", MessageBoxButton.OK, MessageBoxImage.Information);
            //if (checkboxSingleAsset.IsChecked.Value)
            //{
            //    Asset AssetSelected = (Asset)ComboBoxAsset.SelectedItem;

            //    InvestigateSmartMoney(sender, AssetSelected.Name);
            //    //LabelFaultRecord.Content = "Faults: " + FaultyRecords;
            //}
            //else
                InvestigateSmartMoney(sender, "");

            LabelLastUpdate.Content = DateTools.TimeLabel();
            SaveLocalData();
        }

        private void InvestigateSmartMoney(Object sender, string AssetName)
        {
            ProgressStopped = false;
            SmartMoneyTotal.Clear();
            DailyMoneyFlow.Clear();
            SmartMoneyTotal = new List<SmartMoney>();
            //SmartMoneyTotalPeriods.Clear();


            if (LabelCompareHistoryDays.Content.ToString().CompareTo("") > 0)
            {
                PeriodsLabel = new string[LabelCompareHistoryDays.Content.ToString().Split(',').Length];
                PeriodsLabel = LabelCompareHistoryDays.Content.ToString().Split(',');
                SmartMoneyTotalPeriods = new List<SmartMoney>[PeriodsLabel.Length];
            }
            //int[] Periods = new int[PeriodsLabel.Length];
            //int Counter = 0;
            //foreach (string PeriodCircle in PeriodsLabel)
            //    Periods[Counter++] = int.Parse(PeriodCircle);
            //SmartMoneyPeriodCount = Periods.Length + 1;
            //}

            foreach (Asset AssetItem in AssetList)
                AssetItem.History = null;
            //MessageBox.Show(SmartMoneyTotal.Count.ToString());

            //DataGridSmartMoney.ItemsSource = null;
            //DataGridSmartMoney.Items.Clear();
            
            grid_Wait.Visibility = Visibility.Visible;
            Thread.Sleep(100);
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();

            backgroundWorker_Loader = new BackgroundWorker();

            backgroundWorker_Loader.DoWork -= BackgroundWorker_Loader_DoWork;
            backgroundWorker_Loader.DoWork += BackgroundWorker_DetectSmartMoney;
            backgroundWorker_Loader.ProgressChanged -= BackgroundWorker_Loader_ProgressChanged;
            backgroundWorker_Loader.ProgressChanged += BackgroundWorker_Loader_ProgressChanged;
            backgroundWorker_Loader.WorkerReportsProgress = true;
            backgroundWorker_Loader.WorkerSupportsCancellation = true;
            backgroundWorker_Loader.RunWorkerCompleted -= BackgroundWorker_Loader_RunWorkerCompleted;
            backgroundWorker_Loader.RunWorkerCompleted += BackgroundWorker_DetectionCompleted;

            progressbarRetrieval.Minimum = 0;
            progressbarRetrieval.Maximum = DaysToGoBack;
            progressbarRetrieval.Value = 0;

            progressbarAnalysis.Minimum = 0;
            progressbarAnalysis.Maximum = DaysToGoBack;
            progressbarAnalysis.Value = 0;

            backgroundWorker_Loader.RunWorkerAsync(argument: AssetName);
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {            
            if (ProgressStopped)
            {
                dispatcherTimer.Stop();
                grid_Wait.Visibility = Visibility.Collapsed;
            }
        }

        private void BackgroundWorker_DetectionCompleted(object sender, RunWorkerCompletedEventArgs e)
        {   
            ProgressStopped = true;
            Filtrate(true);
        }

        private void BackgroundWorker_DetectSmartMoney(object sender, DoWorkEventArgs e)
        {
            //string SingleAsset = e.Argument.ToString();
            DetectAllAssets(sender);
        }

        //private void DetectSingleAsset(object sender, string AssetName)
        //{
        //    int nCnt = 0;
        //    int AssetsNum = 0;
        //    List<Asset> InvestigatingAssets = new List<Asset>();            
        //    InvestigatingAssets.Add(AssetList.Find(Item => Item.Name.CompareTo(AssetName) == 0));
        //    AssetsNum = InvestigatingAssets.Count();
            
        //    //ToDO: Remove Loop later this is only for one asset
        //    foreach (Asset AssetItem in InvestigatingAssets)
        //    {
        //        (sender as BackgroundWorker).ReportProgress(1, "(" + nCnt++ + "/" + AssetsNum + ")   " + AssetItem.Name);

        //        GetAssetHistory(sender, AssetItem);

        //        if (AssetItem.History.Length > 0)
        //        {
        //            List<SmartMoney> Q = ProcessAssetData(sender, AssetItem);
        //            //SmartMoneyTotal = new ListCollectionView(Q);
        //            //martMoneyTotal.GroupDescriptions.Add(new PropertyGroupDescription("Name"));
        //            SmartMoneyTotal.AddRange(Q);

        //            //if (nCnt++ >= 2)
        //            //{
        //            //    ProgressStopped = true;
        //            //    return;
        //            //}

        //            this.Dispatcher.Invoke(new Action(delegate
        //            {
        //                //ListDisplayDate = checkboxShowOnlyToday.IsChecked.Value ? comboboxYear.SelectedValue + "/" + comboboxMonth.SelectedValue + "/" + comboboxDay.SelectedValue : "";
        //                DataGridSmartMoney.ItemsSource = ListDisplayDate != "" ? (new ObservableCollection<SmartMoney>(SmartMoneyTotal.Where(Items => Items.Date == ListDisplayDate))) : (new ObservableCollection<SmartMoney>(SmartMoneyTotal));
        //            }));
        //        }
        //    }
        //}

        private void DetectAllAssets(object sender)
        {
            int nCnt = 0;
            int AssetsNum = AssetList.Count();
                        
            GetAssetsDaily(sender);

            foreach (Asset AssetItem in AssetList)
            {
                if (AssetItem.History != null) //.Length > 0)
                {
                    (sender as BackgroundWorker).ReportProgress(1, "(" + nCnt++ + "/" + AssetsNum + ")   " + AssetItem.Name);
                    //if (AssetItem.Name.Equals("وکار"))
                    //    MessageBox.Show("Oh K");
                    List<SmartMoney> Q = ProcessAssetData(sender, AssetItem);
                    //SmartMoneyTotal = new ListCollectionView(Q);
                    //martMoneyTotal.GroupDescriptions.Add(new PropertyGroupDescription("Name"));
                    SmartMoneyTotal.AddRange(Q);

                    this.Dispatcher.Invoke(new Action(delegate
                    {
                        DataGridDailyFlow.ItemsSource = (new ObservableCollection<MoneyFlow>(DailyMoneyFlow));
                    }));
                }
            }

            if (PeriodsLabel.Length > 0)
            {
                //PeriodsLabel = LabelCompareHistoryDays.Content.ToString().Split(',');
                int[] Periods = new int[PeriodsLabel.Length];
                int Counter = 0;
                foreach (string PeriodCircle in PeriodsLabel)
                    Periods[Counter++] = int.Parse(PeriodCircle);
                SmartMoneyPeriodCount = Periods.Length + 1;

                for (Counter = 0; Counter < SmartMoneyPeriodCount - 1; Counter++)
                {
                    List<SmartMoney> SmartMoneyTotalPeriod = new List<SmartMoney>();
                    DateTime PeriodMinDay = DateTime.Now.Subtract(new TimeSpan(Periods[Counter], 0, 0, 0));
                    string PeriodMinDate = DateTools.ADToJalali(PeriodMinDay.Year, PeriodMinDay.Month, PeriodMinDay.Day);
                    SmartMoneyTotalPeriod.AddRange(SmartMoneyTotal.Where(Asset => Asset.Date.CompareTo(PeriodMinDate) >= 0));
                    SmartMoneyTotalPeriods[Counter] = SmartMoneyTotalPeriod;
                }
            }
            //SmartMoneyTotal.ForEach(Repeateted => Repeateted.RepeatCount = (from AssetObj in SmartMoneyTotal where AssetObj.FullName.Equals(Repeateted.FullName) select AssetObj).Count());            
        }

        private void Filtrate(bool DoCalculation)
        {
            if (IsLoaded && SmartMoneyTotal.Count > 0)
            {
                if (DoCalculation)
                    SmartMoneyTotal.ToList().ForEach(InOut =>
                    {
                        decimal PeriodIn = (from AssetObj in SmartMoneyTotal where AssetObj.FullName.Equals(InOut.FullName) && InOut.Date.CompareTo(AssetObj.Date) >= 0 select AssetObj.RealBuyValue).Sum();
                        decimal PeriodOut = (from AssetObj in SmartMoneyTotal where AssetObj.FullName.Equals(InOut.FullName) && InOut.Date.CompareTo(AssetObj.Date) >= 0 select AssetObj.RealSellValue).Sum();
                        decimal Result = PeriodIn - PeriodOut;
                        InOut.RealPeriodInOut = Result == 0 ? 0 : Result;
                    });

                ObservableCollection<SmartMoney> SmartMoneyFiltered = DoFiltrationOn(DoCalculation, SmartMoneyTotal);                
                DataGridSmartMoney.ItemsSource = new ObservableCollection<SmartMoney>(SmartMoneyFiltered.OrderByDescending(Records => Records.RealInOut));
                LabelListedCompanies.Content = "لیست: " + DataGridSmartMoney.Items.Count;
                LabelMarketPtoE.Content = "P/E بازار: " + string.Format("{0:F}", SmartMoneyTotal.Select(Item => Item.PtoE).Sum() / SmartMoneyTotal.Count);

                if(SmartMoneyPeriodCount > 1)
                {
                    int Counter = 0;
                    if (TabControlSmartMoneyList.Items.Count > 2)
                        for (Counter = 1; Counter < TabControlSmartMoneyList.Items.Count - 1; Counter++)
                            TabControlSmartMoneyList.Items.RemoveAt(1);
                    Counter = 0;
                    foreach (List<SmartMoney> SmartMoneyPeriod in SmartMoneyTotalPeriods)
                    {
                        TabItem TabItemExtraSmartMoney = new TabItem { Header = "دوره " + PeriodsLabel[Counter].ToString() + " روزه" };
                        //TabItemExtraSmartMoney.Header = new TextBlock();
                        DataGrid DataGridExtraSmartMoney = new DataGrid();                        
                        DataGridExtraSmartMoney.IsReadOnly = true;
                        DataGridExtraSmartMoney.Margin = new Thickness(10);
                        DataGridExtraSmartMoney.AutoGenerateColumns = true;

                        Grid GridExtraSmartMoney = new Grid();
                        
                        TabItemExtraSmartMoney.Content = GridExtraSmartMoney;
                        GroupBox GroupBoxExSMoney = new GroupBox();
                        GroupBoxExSMoney.Content = DataGridExtraSmartMoney;
                        GridExtraSmartMoney.Children.Add(GroupBoxExSMoney);

                        TabControlSmartMoneyList.Items.Insert(1, TabItemExtraSmartMoney);

                        //DataGridColumn[] NewGridColumns = new DataGridColumn[DataGridSmartMoney.Columns.Count];
                        //DataGridSmartMoney.Columns.CopyTo(NewGridColumns, 0);
                        //DataGridExtraSmartMoney.Columns.Clear();
                        //foreach (DataGridColumn RefColumn in NewGridColumns)
                        //{
                        //    RefColumn.Header = RefColumn.Header.ToString() + PeriodsLabel[Counter].ToString();
                        //    DataGridExtraSmartMoney.Columns.Add(RefColumn);
                        //}

                        Counter++;
                        SmartMoneyFiltered = DoFiltrationOn(DoCalculation, SmartMoneyPeriod);
                        DataGridExtraSmartMoney.ItemsSource = new ObservableCollection<SmartMoney>(SmartMoneyFiltered.OrderByDescending(Records => Records.RealInOut));                       
                    }
                }

                //Weighten();
                SaveLocalData();
            }
        }

        private ObservableCollection<SmartMoney> DoFiltrationOn(bool DoCalculation, List<SmartMoney> SmartMoneyReference)
        {
            ObservableCollection<SmartMoney> SmartMoneyFiltered = new ObservableCollection<SmartMoney>(from Records in SmartMoneyReference select Records);

            if (DoCalculation)
            { 
                SmartMoneyFiltered.ToList().ForEach(MarketCap =>
                {
                    decimal StartCap = (from AssetObj in SmartMoneyReference where AssetObj.FullName.Equals(MarketCap.FullName) orderby AssetObj.Date select AssetObj.MarketCapital).First();
                    decimal EndCap = (from AssetObj in SmartMoneyReference where AssetObj.FullName.Equals(MarketCap.FullName) select AssetObj.MarketCapital).First();
                    //MarketCap.StartCap = StartCap;
                    //MarketCap.EndCap = EndCap;
                    MarketCap.MarketGrowth = EndCap - StartCap;
                    MarketCap.MarketGrowthPercentage = (MarketCap.MarketGrowth * 100) / StartCap;
                });

                SmartMoneyFiltered.ToList().ForEach(TotalTraded =>
                {
                    TotalTraded.TotalTradedVolume = (from AssetObj in SmartMoneyReference where AssetObj.FullName.Equals(TotalTraded.FullName) && AssetObj.Date.CompareTo(TotalTraded.Date) <= 0 orderby AssetObj.Date select AssetObj.TradedVolume).Sum();
                    TotalTraded.TotalTradedToStockCount = TotalTraded.StockCount > 0 ? (TotalTraded.TotalTradedVolume * 100) / TotalTraded.StockCount : 0;
                    TotalTraded.TotalTradedToPublicFloat = TotalTraded.PublicFloat > 0 ? (TotalTraded.TotalTradedVolume * 100) / TotalTraded.PublicFloat : 0;
                });
            }

            SmartMoneyFiltered = new ObservableCollection<SmartMoney>(from Records in SmartMoneyReference
                                                                      where
                                                                      (SelectedAsset.CompareTo("") != 0 ? Records.AssetName.CompareTo(SelectedAsset) == 0 : true) &&
                                                                      (
                                                                           (ConsiderVolume ? (Records.TradedVolume > (Records.AverageVolume + (Records.AverageVolume * FlowRate / 100))) : true) &&
                                                                           (
                                                                               (SmartMoneyIn &&
                                                                                   (CompareSides ? Records.RealPerCodeBuyVolume >= Records.RealPerCodeSellVolume : true) &&
                                                                                   (CompareLastNClosing ? Records.Close >= Records.Final : true) &&
                                                                                   (ControlLastPricePercentage ? Records.LastPricePercentage > 0 : true)) ||
                                                                               (!SmartMoneyIn &&
                                                                                   (CompareSides ? Records.RealPerCodeBuyVolume <= Records.RealPerCodeSellVolume : true) &&
                                                                                   (CompareLastNClosing ? Records.Close <= Records.Final : true) &&
                                                                                   (ControlLastPricePercentage ? Records.LastPricePercentage < 0 : true))
                                                                           ) &&
                                                                           (ConsiderPtoE ? (0 < Records.PtoE && Records.PtoE <= PtoEMax) : true) &&
                                                                           (Records.MarketGrowthPercentage <= ConsideredMarketGrowth)
                                                                      )
                                                                      select Records);


            SmartMoneyFiltered.ToList().ForEach(Repeated => Repeated.RepeatCount = (from AssetObj in SmartMoneyFiltered where AssetObj.FullName.Equals(Repeated.FullName) select AssetObj).Count());

            if (ColorDefine)
                SmartMoneyFiltered.ToList().ForEach(Record =>
                {
                    double Spectrum = Record.RealBuyToSellVolume == 0 ? 0 : (double)(Record.BriefingStrength / Record.RealBuyToSellVolume);
                    int ColorIndex = 0;
                    if (Spectrum < 0.5)
                        ColorIndex = 0;
                    else if (0.5 <= Spectrum && Spectrum < 1)
                        ColorIndex = 1;
                    else if (Spectrum == 1)
                        ColorIndex = 2;
                    else if (1 < Spectrum && Spectrum < 2)
                        ColorIndex = 3;
                    else if (2 <= Spectrum)
                        ColorIndex = 4;

                    Record.BackgroundColor = RateSpectrum[ColorIndex];
                    Record.Spectrum = Spectrum;
                });
            else
                SmartMoneyFiltered.ToList().ForEach(Record => Record.BackgroundColor = new SolidColorBrush(Colors.White));


            if (SignalSettingsActive)
                SmartMoneyFiltered = new ObservableCollection<SmartMoney>(SmartMoneyFiltered.Where(Records =>
                                                                                (SignalRepeat != 0 ? Records.RepeatCount >= SignalRepeat : true) &&
                                                                                (BriefingToBuyerStrengthDiff != 0 ? Records.BriefingStrength / Records.RealBuyToSellVolume >= BriefingToBuyerStrengthDiff : true) &&
                                                                                (BriefingGreater ? (SignalBriefing != 0 ? Records.BriefingStrength >= SignalBriefing : true) : (SignalBriefing != 0 ? Records.BriefingStrength <= SignalBriefing : true)) &&

                                                                                (BriefingToBuyerStrengthIsGreater ? (BriefingToBuyerStrength != 0 ? Records.BriefingStrength >= (Records.RealBuyToSellVolume + (Records.RealBuyToSellVolume * BriefingToBuyerStrength / 100)) : true) : (BriefingToBuyerStrength != 0 ? Records.BriefingStrength >= (Records.RealBuyToSellVolume - (Records.RealBuyToSellVolume * BriefingToBuyerStrength / 100)) : true)) &&
                                                                                (PerCapitaBuyValueIsGreater ? (PerCapitaBuyValue != 0 ? Records.RealPerCodeBuyValue >= PerCapitaBuyValue * 1000000 : true) : (PerCapitaBuyValue != 0 ? Records.RealPerCodeBuyValue <= PerCapitaBuyValue * 1000000 : true)) &&

                                                                                (BuyerStrengthGreater ? (SignalBuyerStrength != 0 ? Records.RealBuyToSellVolume >= SignalBuyerStrength : true) : (SignalBuyerStrength != 0 ? Records.RealBuyToSellVolume <= SignalBuyerStrength : true)) &&
                                                                                (ShareOfFreePublic ? (SignalShareOfFreePublic != 0 ? Records.ShareOfPublicFloat >= SignalShareOfFreePublic : true) : (SignalShareOfFreePublic != 0 ? Records.ShareOfPublicFloat <= SignalShareOfFreePublic : true)) &&
                                                                                (BuyerCountGreater ? (SignalBuyerCount != 0 ? Records.RealBuyerNumber >= SignalBuyerCount : true) : (SignalBuyerCount != 0 ? Records.RealBuyerNumber <= SignalBuyerCount : true)) &&
                                                                                (!CoBuyCompareGreater ? (SignalCoBuy != 0 ? Records.CoBuyPart <= SignalCoBuy : true) : (SignalCoBuy != 0 ? Records.CoBuyPart >= SignalCoBuy : true)) &&
                                                                                (!CoSellCompareGreater ? (SignalCoSell != 0 ? Records.CoSellPart <= SignalCoSell : true) : (SignalCoSell != 0 ? Records.CoSellPart >= SignalCoSell : true)) &&
                                                                                (SignalCoBuyerGreatness != 0 ? Math.Abs(Records.CoBuyPart - Records.CoSellPart) <= SignalCoBuyerGreatness : true) &&
                                                                                (CoBuyerGreater ? (Records.CoBuyPart >= Records.CoSellPart) : true) &&
                                                                                (BasisVolumeSmaller ? (SignalBasisVolumeGreatness != 0 ? Records.TradedVolume / Records.BasisVolume <= SignalBasisVolumeGreatness : true) : (SignalBasisVolumeGreatness != 0 ? Records.TradedVolume / Records.BasisVolume >= SignalBasisVolumeGreatness : true)) &&
                                                                                (ListDisplayDate != "" ? Records.Date.Equals(ListDisplayDate) : true)
                //}
                ));
            else
                SmartMoneyFiltered = new ObservableCollection<SmartMoney>(SmartMoneyFiltered.Where(Records => (ListDisplayDate != "" ? Records.Date.Equals(ListDisplayDate) : true)).OrderByDescending(Records => Records.RealInOut));

            return SmartMoneyFiltered;
        }

        //private void AutoFiltrate()
        //{
        //    ObservableCollection<SmartMoney> SmartMoneyFiltered = new ObservableCollection<SmartMoney>(from Records in SmartMoneyTotal
        //                                                                                               where (
        //                                                                                                        (ConsiderVolume ? (Records.TradedVolume > (Records.AverageVolume + (Records.AverageVolume * FlowRate / 100))) : true) &&
        //                                                                                                        (
        //                                                                                                            (SmartMoneyIn &&
        //                                                                                                                 (CompareSides ? Records.RealPerCodeBuyVolume >= Records.RealPerCodeSellVolume : true) &&
        //                                                                                                                 (CompareLastNClosing ? Records.Close >= Records.Final : true) &&
        //                                                                                                                 (ControlLastPricePercentage ? Records.LastPricePercentage > 0 : true)) ||
        //                                                                                                            (!SmartMoneyIn &&
        //                                                                                                                 (CompareSides ? Records.RealPerCodeBuyVolume <= Records.RealPerCodeSellVolume : true) &&
        //                                                                                                                 (CompareLastNClosing ? Records.Close <= Records.Final : true) &&
        //                                                                                                                 (ControlLastPricePercentage ? Records.LastPricePercentage < 0 : true))
        //                                                                                                        )
        //                                                                                                    //&&
        //                                                                                                    //(ListDisplayDate != "" ? Records.Date.Equals(ListDisplayDate) : true)
        //                                                                                                    )
        //                                                                                               select Records);

        //    decimal AutoSignalPerCodeBuyValue = 50000000;


        //    SmartMoneyFiltered.ToList().ForEach(Repeateted => Repeateted.RepeatCount = (from AssetObj in SmartMoneyFiltered where AssetObj.FullName.Equals(Repeateted.FullName) select AssetObj).Count());
        //    DataGridSmartMoney.ItemsSource = SmartMoneyFiltered.Where(Records => (ListDisplayDate != "" ? Records.Date.Equals(ListDisplayDate) : true));
        //    if (checkboxSignalSettings.IsChecked.Value)
        //        DataGridSmartMoney.ItemsSource = SmartMoneyFiltered.Where(Records =>
        //                                                                    (Records.RealPerCodeBuyValue >= AutoSignalPerCodeBuyValue) &&
        //                                                                    ((Records.TradedVolume * 100 / 20) <= Records.StockCount) 

        //                                                                    (SignalRepeat != 0 ? Records.RepeatCount >= SignalRepeat : true) &&
        //                                                                    (BriefingGreater ? (SignalBriefing != 0 ? Records.BriefingStrength >= SignalBriefing : true) : (SignalBriefing != 0 ? Records.BriefingStrength <= SignalBriefing : true)) &&
        //                                                                    (BuyerStrengthGreater ? (SignalBuyerStrength != 0 ? Records.RealBuyToSellVolume >= SignalBuyerStrength : true) : (SignalBuyerStrength != 0 ? Records.RealBuyToSellVolume <= SignalBuyerStrength : true)) &&
        //                                                                    (ShareOfFreePublic ? (SignalShareOfFreePublic != 0 ? Records.ShareOfPublicFloat >= SignalShareOfFreePublic : true) : (SignalShareOfFreePublic != 0 ? Records.ShareOfPublicFloat <= SignalShareOfFreePublic : true)) &&
        //                                                                    (BuyerCountGreater ? (SignalBuyerCount != 0 ? Records.RealBuyerNumber >= SignalBuyerCount : true) : (SignalBuyerCount != 0 ? Records.RealBuyerNumber <= SignalBuyerCount : true)) &&
        //                                                                    (!CoBuyCompareGreater ? (SignalCoBuy != 0 ? Records.CoBuyPart <= SignalCoBuy : true) : (SignalCoBuy != 0 ? Records.CoBuyPart >= SignalCoBuy : true)) &&
        //                                                                    (!CoSellCompareGreater ? (SignalCoSell != 0 ? Records.CoSellPart <= SignalCoSell : true) : (SignalCoSell != 0 ? Records.CoSellPart >= SignalCoSell : true)) &&
        //                                                                    (SignalCoBuyerGreatness != 0 ? Math.Abs(Records.CoBuyPart - Records.CoSellPart) <= SignalCoBuyerGreatness : true) &&
        //                                                                    (CoBuyerGreater ? (Records.CoBuyPart >= Records.CoSellPart) : true) &&
        //                                                                    (BasisVolumeSmaller ? (SignalBasisVolumeGreatness != 0 ? Records.TradedVolume / Records.BasisVolume <= SignalBasisVolumeGreatness : true) : (SignalBasisVolumeGreatness != 0 ? Records.TradedVolume / Records.BasisVolume >= SignalBasisVolumeGreatness : true)) &&
        //                                                                    (ListDisplayDate != "" ? Records.Date.Equals(ListDisplayDate) : true)
        //                                                                    );

        //}

        private void SaveLocalData()
        {
            SaveLastSettings();
        }

        private void SaveLastSettings()
        {
            try
            {                
                BinaryWriter BinaryWriterSettings = new BinaryWriter(File.Open("DefaultSettings.sps", FileMode.OpenOrCreate));
                BinaryWriterSettings.Write(DaysToGoBack);
                BinaryWriterSettings.Write(FlowRate);
                BinaryWriterSettings.Write(CoBuyPercentage);
                BinaryWriterSettings.Write(CoSellPercentage);
                BinaryWriterSettings.Write(CountsWorkDays);
                BinaryWriterSettings.Write(SmartMoneyIn);
                BinaryWriterSettings.Write(CompareSides);
                BinaryWriterSettings.Write(CompareLastNClosing);
                BinaryWriterSettings.Write(ControlLastPricePercentage);
                //BinaryWriterSettings.Write(LoadEveryAsset);
                BinaryWriterSettings.Write(ConsiderVolume);

                if(IsLoaded)
                    BinaryWriterSettings.Write(checkboxSignalSettings.IsChecked.Value);

                #region Signal Parameters
                BinaryWriterSettings.Write(SignalRepeat);
                BinaryWriterSettings.Write(SignalBriefing);
                BinaryWriterSettings.Write(SignalBuyerStrength);
                BinaryWriterSettings.Write(SignalVolumeToAverage);
                BinaryWriterSettings.Write(SignalShareOfFreePublic);
                BinaryWriterSettings.Write(SignalBuyerCount);
                BinaryWriterSettings.Write(SignalCoBuy);
                BinaryWriterSettings.Write(SignalCoSell);
                BinaryWriterSettings.Write(SignalCoBuyerGreatness);
                BinaryWriterSettings.Write(SignalBasisVolumeGreatness);
                
                BinaryWriterSettings.Write(BriefingToBuyerStrengthDiff);
                BinaryWriterSettings.Write(BriefingToBuyerStrength);
                BinaryWriterSettings.Write(PerCapitaBuyValue);

                BinaryWriterSettings.Write(BriefingGreater);
                BinaryWriterSettings.Write(BuyerStrengthGreater);
                BinaryWriterSettings.Write(ShareOfFreePublic);
                BinaryWriterSettings.Write(BuyerCountGreater);
                BinaryWriterSettings.Write(CoBuyCompareGreater);
                BinaryWriterSettings.Write(CoSellCompareGreater);
                BinaryWriterSettings.Write(CoBuyerGreater);
                BinaryWriterSettings.Write(BasisVolumeSmaller);
                BinaryWriterSettings.Write(BriefingToBuyerStrengthIsGreater);
                BinaryWriterSettings.Write(PerCapitaBuyValueIsGreater);
                #endregion Signal Parameters

                BinaryWriterSettings.Write(ListDisplayDate);

                BinaryWriterSettings.Close();
            } 
            catch(Exception exp)
            {

            }
        }

        private void LoadLastSettings()
        {
            try
            {
                //TextBoxHistoryDays.Text = DaysToGoBack;
                //TextBoxDifferance.Text = FlowRate;

                BinaryReader BinaryReaderSettings = new BinaryReader(File.Open("DefaultSettings.sps", FileMode.Open));
                DaysToGoBack = BinaryReaderSettings.ReadInt32();
                TextBoxHistoryDays.Text = DaysToGoBack.ToString();

                FlowRate = BinaryReaderSettings.ReadDecimal();
                TextBoxDifferance.Text = FlowRate.ToString();

                CoBuyPercentage = BinaryReaderSettings.ReadDecimal();
                TextBoxSignalCoBuy.Text = string.Format("{0:F}", CoBuyPercentage);

                CoSellPercentage = BinaryReaderSettings.ReadDecimal();
                TextBoxSignalCoSell.Text = string.Format("{0:F}", CoSellPercentage);

                CountsWorkDays = BinaryReaderSettings.ReadBoolean();
                radiobuttonCountWorkDays.IsChecked = CountsWorkDays;
                radiobuttonCountDays.IsChecked = !CountsWorkDays;

                SmartMoneyIn = BinaryReaderSettings.ReadBoolean();
                radiobuttonSmartMoneyIn.IsChecked = SmartMoneyIn;
                radiobuttonSmartMoneyOut.IsChecked = !SmartMoneyIn;

                CompareSides= BinaryReaderSettings.ReadBoolean();
                checkboxCompareSides.IsChecked = CompareSides;

                CompareLastNClosing= BinaryReaderSettings.ReadBoolean();
                checkboxCompareLastNClosing.IsChecked = CompareLastNClosing;

                ControlLastPricePercentage= BinaryReaderSettings.ReadBoolean();
                checkboxControlLastPricePercentage.IsChecked = ControlLastPricePercentage;

                //LoadEveryAsset= BinaryReaderSettings.ReadBoolean();
                ConsiderVolume= BinaryReaderSettings.ReadBoolean();
                RadioButtonConsiderVolume.IsChecked = ConsiderVolume ? true : false;
                RadioButtonDoNotConsiderVolume.IsChecked = !ConsiderVolume ? true : false;

                checkboxSignalSettings.IsChecked = BinaryReaderSettings.ReadBoolean();

                #region Signal Parameters
                SignalRepeat = BinaryReaderSettings.ReadInt32();
                TextBoxSignalRepeat.Text = string.Format("{0}", SignalRepeat);

                SignalBriefing = BinaryReaderSettings.ReadDecimal();
                TextBoxSignalBriefing.Text = string.Format("{0:F}", SignalBriefing);

                SignalBuyerStrength = BinaryReaderSettings.ReadDecimal();
                TextBoxSignalBuyStrength.Text = string.Format("{0:F}", SignalBuyerStrength);
                
                SignalVolumeToAverage = BinaryReaderSettings.ReadDecimal();
                //TextBoxSignalBasisVolumeGreatness.Text = string.Format("{0:F}", SignalVolumeToAverage);
                
                SignalShareOfFreePublic = BinaryReaderSettings.ReadDecimal();
                TextBoxSignalShareOfFreePublic.Text = string.Format("{0:F}", SignalShareOfFreePublic);

                SignalBuyerCount = BinaryReaderSettings.ReadInt32();
                TextBoxSignalBuyerCount.Text = string.Format("{0}", SignalBuyerCount);

                SignalCoBuy = BinaryReaderSettings.ReadDecimal();
                TextBoxSignalCoBuy.Text = string.Format("{0:F}", SignalCoBuy);
                
                SignalCoSell = BinaryReaderSettings.ReadDecimal();
                TextBoxSignalCoSell.Text = string.Format("{0:F}", SignalCoSell);

                SignalCoBuyerGreatness = BinaryReaderSettings.ReadDecimal();
                TextBoxSignalCoBuyerGreatness.Text = string.Format("{0:F}", SignalCoBuyerGreatness);

                SignalBasisVolumeGreatness = BinaryReaderSettings.ReadDecimal();
                TextBoxSignalBasisVolumeGreatness.Text = string.Format("{0:F}", SignalBasisVolumeGreatness);

                BriefingToBuyerStrengthDiff = BinaryReaderSettings.ReadDecimal();
                TextBoxBriefingToBuyerStrengthDiff.Text = string.Format("{0:F}", BriefingToBuyerStrengthDiff);

                BriefingToBuyerStrength = BinaryReaderSettings.ReadDecimal();
                TextBoxBrifingToBuyerStrength.Text = string.Format("{0:F}", BriefingToBuyerStrength);

                PerCapitaBuyValue = BinaryReaderSettings.ReadDecimal();
                TextBoxPerCapitaBuyValue.Text = string.Format("{0:F}", PerCapitaBuyValue);

                BriefingGreater = BinaryReaderSettings.ReadBoolean();
                CheckBoxBriefing.IsChecked = !BriefingGreater;

                BuyerStrengthGreater = BinaryReaderSettings.ReadBoolean();
                CheckBoxBuyerStrength.IsChecked = !BuyerStrengthGreater;

                ShareOfFreePublic = BinaryReaderSettings.ReadBoolean();
                CheckBoxShareOfFreePublic.IsChecked = !ShareOfFreePublic;

                BuyerCountGreater = BinaryReaderSettings.ReadBoolean();
                CheckBoxBuyerCount.IsChecked = !BuyerCountGreater;

                CoBuyCompareGreater = BinaryReaderSettings.ReadBoolean();
                CoBuyGreater.IsChecked = CoBuyCompareGreater;

                CoSellCompareGreater = BinaryReaderSettings.ReadBoolean();
                CoSellGreater.IsChecked = CoSellCompareGreater;

                CoBuyerGreater = BinaryReaderSettings.ReadBoolean();
                CheckBoxCoBuyerGreater.IsChecked = CoBuyerGreater;

                BasisVolumeSmaller = BinaryReaderSettings.ReadBoolean();
                CheckBoxBasisVolumeSmaller.IsChecked = BasisVolumeSmaller;

                BriefingToBuyerStrengthIsGreater = BinaryReaderSettings.ReadBoolean();
                CheckBoxBrifingToBuyerStrength.IsChecked = !BriefingToBuyerStrengthIsGreater;

                PerCapitaBuyValueIsGreater = BinaryReaderSettings.ReadBoolean();
                CheckBoxPerCapitaBuyValue.IsChecked = !PerCapitaBuyValueIsGreater;
                #endregion Signal Parameters

                //ListDisplayDate = BinaryReaderSettings.ReadString();
                //if (ListDisplayDate.CompareTo("") != 0)
                //{                    
                //    string[] DisplayDate = ListDisplayDate.Split('/');
                //    comboboxYear.Text = DisplayDate[0];
                //    comboboxMonth.SelectedIndex = int.Parse(DisplayDate[1]) - 1;
                //    comboboxDay.Text = DisplayDate[2];
                //}
                //else
                //    checkboxShowOnlyToday.IsChecked = true;

                BinaryReaderSettings.Close();
            }
            catch(Exception exp)
            {

            }
        }

        void SaveLastData()
        {
            List<Asset> AssetList = new List<Asset>();
            List<SmartMoney> SmartMoneyTotal = new List<SmartMoney>();
            List<MoneyFlow> DailyMoneyFlow = new List<MoneyFlow>();
        }

        private void Weighten()
        {
            SmartMoneyTotal.ForEach(SmartMoneyItem =>
            {
                float CalibratedMarketGrowth = (float)(100 - (SmartMoneyItem.MarketGrowthPercentage + 25)) / 10;
                if (CalibratedMarketGrowth < 0)//Very large growth in MarketGrowth must not have good point
                    CalibratedMarketGrowth = 0;
                else if (CalibratedMarketGrowth >= 10)//Very large drops in MarketGrowth must not have biggest point
                    CalibratedMarketGrowth = 5;
                
                float CalibratedTotalTradedVol = 0;
                if (SmartMoneyItem.PublicFloatPercentage != 0)
                {
                    CalibratedTotalTradedVol = (float)(100 - SmartMoneyItem.TotalTradedToPublicFloat) / 10;
                    CalibratedTotalTradedVol = CalibratedTotalTradedVol > 0 ? CalibratedTotalTradedVol : 0;
                }
                else if (SmartMoneyItem.PublicFloatPercentage == 0)
                {
                    decimal PseudoPublicFloat = (SmartMoneyItem.StockCount * 25) / 100;
                    if(PseudoPublicFloat < SmartMoneyItem.TotalTradedToStockCount)
                        CalibratedTotalTradedVol = 0;
                    CalibratedTotalTradedVol = (float)(100 - SmartMoneyItem.TotalTradedToStockCount) / 10;
                }

                //Factor 1: P/E
                int PEWeight = 0;
                if (0 < SmartMoneyItem.PtoE && SmartMoneyItem.PtoE <= 10)
                    PEWeight = 10;
                if (10 < SmartMoneyItem.PtoE && SmartMoneyItem.PtoE <= 15)
                    PEWeight = 8;
                if (15 < SmartMoneyItem.PtoE && SmartMoneyItem.PtoE <= 30)
                    PEWeight = 5;
                if (25 < SmartMoneyItem.PtoE && SmartMoneyItem.PtoE <= 50)
                    PEWeight = 3;
                if (25 < SmartMoneyItem.PtoE)
                    PEWeight = 1;

                //Factor 2: Briefing
                int BriefingWeight = 0;
                if (1 < SmartMoneyItem.BriefingStrength && SmartMoneyItem.BriefingStrength < 2)
                    BriefingWeight = 1;
                if (2 <= SmartMoneyItem.BriefingStrength && SmartMoneyItem.BriefingStrength < 10)
                    BriefingWeight = 5;
                if (10 < SmartMoneyItem.BriefingStrength && SmartMoneyItem.BriefingStrength < 50)
                    BriefingWeight = 8;
                if (50 <= SmartMoneyItem.BriefingStrength)
                    BriefingWeight = 10;

                //Factor 3: Buyer Strength
                int BuyerStrengthWeight = 0;
                if (1 < SmartMoneyItem.RealBuyToSellVolume && SmartMoneyItem.RealBuyToSellVolume < 2)
                    BuyerStrengthWeight = 1;
                if (2 <= SmartMoneyItem.RealBuyToSellVolume && SmartMoneyItem.RealBuyToSellVolume < 10)
                    BuyerStrengthWeight = 5;
                if (10 < SmartMoneyItem.RealBuyToSellVolume && SmartMoneyItem.RealBuyToSellVolume < 50)
                    BuyerStrengthWeight = 8;
                if (50 <= SmartMoneyItem.RealBuyToSellVolume)
                    BuyerStrengthWeight = 10;

                //Factor 4: Repeat
                int RepeatWeight = SmartMoneyItem.RepeatCount / 10;
                
                //Factor 5: Day Volume Growth to Period's Total
                int VolGrowthWeight = 0;
                if ((decimal)0.1 < SmartMoneyItem.VolumeGrowthRatio && SmartMoneyItem.VolumeGrowthRatio <= (decimal)0.5)
                    VolGrowthWeight = 1;
                if ((decimal)0.5 < SmartMoneyItem.VolumeGrowthRatio && SmartMoneyItem.VolumeGrowthRatio <= (decimal)1)
                    VolGrowthWeight = 2;
                if ((decimal)1 < SmartMoneyItem.VolumeGrowthRatio && SmartMoneyItem.VolumeGrowthRatio <= (decimal)1.5)
                    VolGrowthWeight = 5;
                if ((decimal)1.5 < SmartMoneyItem.VolumeGrowthRatio && SmartMoneyItem.VolumeGrowthRatio <= (decimal)2)
                    VolGrowthWeight = 6;
                if ((decimal)2 < SmartMoneyItem.VolumeGrowthRatio && SmartMoneyItem.VolumeGrowthRatio <= (decimal)5)
                    VolGrowthWeight = 8;
                if (5 < SmartMoneyItem.VolumeGrowthRatio)
                    VolGrowthWeight = 10;

                //Factor 6: Public Float Percentage
                float PublicFloatPercentage = (float)SmartMoneyItem.PublicFloatPercentage;
                int PublicFloatWeight = 10;
                if (0 < PublicFloatPercentage && PublicFloatPercentage <= 15)
                    PublicFloatWeight = 9;
                if (15 < PublicFloatPercentage && PublicFloatPercentage <= 25)
                    PublicFloatWeight = 5;
                if (25 < PublicFloatPercentage && PublicFloatPercentage <= 50)
                    PublicFloatWeight = 1;
                if (50 < PublicFloatPercentage)
                    PublicFloatWeight = 0;



                ////ToDO: Change the Average * Repeat -> Days Volume Summation / Day Number
                float AverageVolumeCirculation = (float)SmartMoneyItem.AverageVolume * SmartMoneyItem.RepeatCount;
                ////Factor 7: Circulation to Public Float
                int AverageVolumeCirculationWeight = 10;
                //if (PublicFloatPercentage > 0)
                //{
                //    if (AverageVolumeCirculation < (float)SmartMoneyItem.PublicFloat)
                //        AverageVolumeCirculationWeight = 10;
                //    if ((float)SmartMoneyItem.PublicFloat < AverageVolumeCirculation && AverageVolumeCirculation < (float)SmartMoneyItem.PublicFloat * 1.5)
                //        AverageVolumeCirculationWeight = 5;
                //    if ((float)SmartMoneyItem.PublicFloat * 1.5 <= AverageVolumeCirculation && AverageVolumeCirculation <= (float)SmartMoneyItem.PublicFloat * 2.5)
                //        AverageVolumeCirculationWeight = 3;
                //    if ((float)SmartMoneyItem.PublicFloat * 2.5 < AverageVolumeCirculation)
                //        AverageVolumeCirculationWeight = 0;
                //}
                //else
                //{
                //    if (AverageVolumeCirculation < ((float)SmartMoneyItem.StockCount * 0.5) / 100 )
                //        AverageVolumeCirculationWeight = 10;
                //    if (((float)SmartMoneyItem.StockCount * 0.5 / 100) <= AverageVolumeCirculation && AverageVolumeCirculation < ((float)SmartMoneyItem.StockCount * 1.5) / 100)
                //        AverageVolumeCirculationWeight = 5;
                //    if (((float)SmartMoneyItem.StockCount * 1.5 / 100) <= AverageVolumeCirculation && AverageVolumeCirculation < ((float)SmartMoneyItem.StockCount * 5) / 100)
                //        AverageVolumeCirculationWeight = 1;
                //    if (((float)SmartMoneyItem.StockCount * 5 / 100) <= AverageVolumeCirculation)
                //        AverageVolumeCirculationWeight = 0;
                //}

                //Factor 8: Co Buy Weight
                float CoBuyWeight = 10;// (float)SmartMoneyItem.CoBuyPart / 10;

                //Factor 9: Public Float Percentage
                float CoSellWeight = 10;// (100 - (float)SmartMoneyItem.CoSellPart) / 10;

                ////Factor 10: Public Float Percentage
                //float CoBuyWeight = 10;
                //if (0 < PublicFloatPercentage && PublicFloatPercentage <= 15)
                //    PublicFloatWeight = 9;
                float DayFlowWeight = SmartMoneyItem.RealInOut > 0 ? 10 : 0;
                float PeriodFlowWeight = SmartMoneyItem.RealPeriodInOut > 0 ? 10 : 0;

                SmartMoneyItem.Spectrum = (CalibratedMarketGrowth + CalibratedTotalTradedVol + PEWeight + BriefingWeight + BuyerStrengthWeight + RepeatWeight + VolGrowthWeight + PublicFloatWeight + AverageVolumeCirculationWeight + CoBuyWeight + CoSellWeight + DayFlowWeight + PeriodFlowWeight) / 13;
            });
            //foreach(SmartMoney SmartMoneyItem in SmartMoneyTotal)
            //{
            //    var AssetSmartMoney = from SmartMoneyObj in SmartMoneyTotal
            //                          where SmartMoneyObj.FullName.CompareTo(SmartMoneyItem.FullName) == 0 //&&
            //                                //SmartMoneyObj.Density
            //                          select SmartMoneyObj;
            //    decimal Density = 0;
            //    foreach (SmartMoney AssetObj in AssetSmartMoney)
            //    {
            //        decimal DayWeight = 0;
            //        TimeSpan Today = new TimeSpan(DateTime.Now.Ticks);
            //        TimeSpan Repeat = new TimeSpan(DateTools.SortableToAD(AssetObj.Date + "-00:00:00").Ticks);                    
            //        Density *= (1 / (int)Math.Floor(Today.TotalDays) - (int)Math.Floor(Repeat.TotalDays));
            //    }
            //    SmartMoneyTotal.Where(AssetObj => AssetObj.AssetName == SmartMoneyItem.AssetName).ToList().ForEach(AssetObj => AssetObj.Density = Density == 0 ? 0 : Math.Floor(1 / Density));

            //}
        }

        private void DrawFlowChart()
        {
            if (DailyMoneyFlow.Count != 0)
            {
                canvasFlowChart.Children.Clear();

                int MarginLeft = 5;
                int MarginTop = 5;
                int MarginButtom = 5;

                Line XAxis = new Line();
                XAxis.X1 = 0;
                XAxis.Y1 = canvasFlowChart.ActualHeight / 2;
                XAxis.X2 = canvasFlowChart.ActualWidth;
                XAxis.Y2 = canvasFlowChart.ActualHeight / 2;
                XAxis.Stroke = new SolidColorBrush(Colors.LightGray);
                canvasFlowChart.Children.Add(XAxis);

                Line YAxis = new Line();
                YAxis.X1 = MarginLeft;
                YAxis.Y1 = MarginTop;
                YAxis.X2 = MarginLeft;
                YAxis.Y2 = canvasFlowChart.ActualHeight - MarginButtom;
                YAxis.Stroke = new SolidColorBrush(Colors.LightGray);
                canvasFlowChart.Children.Add(YAxis);

                MoneyFlow[] DailyFlow = DailyMoneyFlow.OrderBy(Item => Item.Date).ToArray();

                int XAxisTickSize = (int)(canvasFlowChart.ActualWidth - 10) / DailyFlow.Length;
                double YAxisScale = 100000000000;
                double XAxisCenter = (canvasFlowChart.ActualHeight / 2);

                Line[] ChartLines = new Line[DailyFlow.Length];
                int XPos = MarginLeft;
                ChartLines[0] = new Line();
                ChartLines[0].Stroke = new LinearGradientBrush(Colors.LightGray, Colors.DarkGray, 45);
                ChartLines[0].X1 = XPos;
                ChartLines[0].Y1 = XAxisCenter - ((double)DailyFlow[0].Flow / YAxisScale);
                ChartLines[0].X2 = (XPos += XAxisTickSize);
                ChartLines[0].Y2 = (XAxisCenter - (double)DailyFlow[1].Flow / YAxisScale);
                canvasFlowChart.Children.Add(ChartLines[0]);

                DrawText(canvasFlowChart, (canvasFlowChart.ActualHeight / 2), ChartLines[0].X1, 10, -45, DailyFlow[0].Date);
                DrawPoint(canvasFlowChart, ChartLines[0].X1, ChartLines[0].Y1, DailyFlow[0].Date + "\n" + DailyFlow[0].Flow.ToString("#,#"), (canvasFlowChart.ActualHeight / 2));
                
                for (int nCount = 1; nCount < DailyFlow.Length - 1; nCount++)
                {
                    ChartLines[nCount] = new Line();
                    ChartLines[nCount].Stroke = new LinearGradientBrush(Colors.LightGray, Colors.DarkGray, 45);
                    ChartLines[nCount].X1 = ChartLines[nCount - 1].X2;
                    ChartLines[nCount].Y1 = ChartLines[nCount - 1].Y2;
                    ChartLines[nCount].X2 = (XPos += XAxisTickSize);
                    ChartLines[nCount].Y2 = (XAxisCenter - (double)DailyFlow[nCount + 1].Flow / YAxisScale);
                    canvasFlowChart.Children.Add(ChartLines[nCount]);

                    DrawTick(canvasFlowChart, ChartLines[nCount - 1].X2, (canvasFlowChart.ActualHeight / 2), 5);
                    DrawText(canvasFlowChart, (canvasFlowChart.ActualHeight / 2), ChartLines[nCount].X1, 10, -45, DailyFlow[nCount].Date);
                    DrawPoint(canvasFlowChart, ChartLines[nCount].X1, ChartLines[nCount].Y1, DailyFlow[nCount].Date + "\n" + DailyFlow[nCount].Flow.ToString("#,#"), (canvasFlowChart.ActualHeight / 2));
                }

                DrawTick(canvasFlowChart, ChartLines[DailyFlow.Length - 2].X2, (canvasFlowChart.ActualHeight / 2), 5);
                DrawText(canvasFlowChart, (canvasFlowChart.ActualHeight / 2), ChartLines[DailyFlow.Length - 2].X2, 10, -45, DailyFlow[DailyFlow.Length - 1].Date);
                DrawPoint(canvasFlowChart, ChartLines[DailyFlow.Length - 2].X2, ChartLines[DailyFlow.Length - 2].Y2, DailyFlow[DailyFlow.Length - 1].Date + "\n" + DailyFlow[DailyFlow.Length - 1].Flow.ToString("#,#"), (canvasFlowChart.ActualHeight / 2));
            }
        }

        private void DrawTick(Canvas canvasTarget, double XOrigin, double YOrigin, double Size)
        {
            Line ChartTick = new Line();
            ChartTick.Stroke = new LinearGradientBrush(Colors.DarkGray, Colors.LightGray, 90);
            ChartTick.X1 = XOrigin;
            ChartTick.Y1 = YOrigin;
            ChartTick.X2 = XOrigin;
            ChartTick.Y2 = YOrigin + Size;
            canvasTarget.Children.Add(ChartTick);
        }

        private void DrawText(Canvas canvasTarget, double Top, double Left, int FontSize, int Angle, string Text)
        {
            TextBlock textblockLabel = new TextBlock();
            textblockLabel.Text = Text;
            textblockLabel.FontSize = FontSize;
            Canvas.SetTop(textblockLabel, Top);
            Canvas.SetLeft(textblockLabel, Left);
            //textblockDateLabel.RenderTransformOrigin = new Point(ChartLines[DailyFlow.Length].X1, canvasFlowChart.Height / 4);
            textblockLabel.LayoutTransform = new RotateTransform(Angle);
            canvasFlowChart.Children.Add(textblockLabel);
        }

        private void DrawPoint(Canvas canvasTarget, double XPos, double YPos, string ToolTipText, double XOrigin)
        {
            int dotSize = 7;

            Ellipse currentDot = new Ellipse();
            currentDot.Stroke = new SolidColorBrush(YPos >= XOrigin ? Colors.Red : Colors.Green);
            currentDot.StrokeThickness = 2;
            Canvas.SetZIndex(currentDot, 2);
            currentDot.Height = dotSize;
            currentDot.Width = dotSize;
            currentDot.Fill = new SolidColorBrush(Colors.Green);
            currentDot.Margin = new Thickness(XPos - (dotSize / 2), YPos - (dotSize / 2), 0, 0); // Sets the position.
            ToolTip tooltipDot = new System.Windows.Controls.ToolTip();
            tooltipDot.Content = ToolTipText;
            currentDot.ToolTip = tooltipDot;
            canvasTarget.Children.Add(currentDot);
        }

        private void radiobuttonCountDays_Checked(object sender, RoutedEventArgs e)
        {
            CountsWorkDays = false;
            //SaveLocalData();
        }

        private void radiobuttonCountWorkDays_Checked(object sender, RoutedEventArgs e)
        {
            CountsWorkDays = true;
            //SaveLocalData();
        }

        private void TextBoxDifferance_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
            {
                if (TextBoxDifferance.Text != "")
                    FlowRate = decimal.Parse(TextBoxDifferance.Text);
                if (FlowRate < 0)
                    LabelVolumeControl.Content = "% میانگین دوره کمتر باشند";
                else
                    LabelVolumeControl.Content = "% میانگین دوره بیشتر باشند";
                Filtrate(false);
            }            
        }

        private void TextBoxCoBuyPercentage_TextChanged(object sender, TextChangedEventArgs e)
        {
            //CoBuyPercentage = decimal.Parse(TextBoxCoBuyPercentage.Text);
        }

        private void TextBoxCoSellPercentage_TextChanged(object sender, TextChangedEventArgs e)
        {
            //CoSellPercentage = decimal.Parse(TextBoxCoSellPercentage.Text);
        }

        private void radiobuttonSmartMoneyIn_Checked(object sender, RoutedEventArgs e)
        {
            if (radiobuttonSmartMoneyIn.IsChecked.Value)
            {                
                if (this.IsLoaded)
                {
                    SmartMoneyIn = true;
                    ToolTip tooltipTemp = new ToolTip { Content = "متوسط حجم خریداری شده هر کد خریدار، بیشتر از حجم فروخته شده توسط هرکد فروشنده باشد" };
                    wrappanelVolumeControl.ToolTip = tooltipTemp;
                    tooltipTemp = new ToolTip { Content = "آخرین قیمت از قیمت پایانی بیشتر باشد" };
                    wrappanelCompareLastNClosing.ToolTip = tooltipTemp;
                    tooltipTemp = new ToolTip { Content = "درصد تغییرات آخرین قیمت بیشتر از صفر باشد" };
                    wrappanelControlLastPricePercentage.ToolTip = tooltipTemp;
                }
                Filtrate(true);
            }
        }

        private void radiobuttonSmartMoneyOut_Checked(object sender, RoutedEventArgs e)
        {
            if (radiobuttonSmartMoneyOut.IsChecked.Value)
            {
                if (this.IsLoaded)
                {
                    SmartMoneyIn = false;
                    ToolTip tooltipTemp = new ToolTip { Content = "متوسط حجم خریداری شده هر کد خریدار، کمتر از حجم فروخته شده توسط هرکد فروشنده باشد" };
                    wrappanelVolumeControl.ToolTip = tooltipTemp;
                    tooltipTemp = new ToolTip { Content = "آخرین قیمت از قیمت پایانی کمتر باشد" };
                    wrappanelCompareLastNClosing.ToolTip = tooltipTemp;
                    tooltipTemp = new ToolTip { Content = "درصد تغییرات آخرین قیمت کمتر از صفر باشد" };
                    wrappanelControlLastPricePercentage.ToolTip = tooltipTemp;
                }
                Filtrate(true);
            }
        }

        private void checkboxCompareSides_Checked(object sender, RoutedEventArgs e)
        {
            CompareSides = checkboxCompareSides.IsChecked.Value;
            Filtrate(false);
        }

        private void checkboxCompareSides_Unchecked(object sender, RoutedEventArgs e)
        {
            CompareSides = checkboxCompareSides.IsChecked.Value;
            Filtrate(false);
        }

        private void checkboxCompareLastNClosing_Checked(object sender, RoutedEventArgs e)
        {
            CompareLastNClosing = checkboxCompareLastNClosing.IsChecked.Value;
            Filtrate(false);
        }

        private void checkboxCompareLastNClosing_Unchecked(object sender, RoutedEventArgs e)
        {
            CompareLastNClosing = checkboxCompareLastNClosing.IsChecked.Value;
            Filtrate(false);
        }
        
        private void checkboxControlLastPricePercentage_Checked(object sender, RoutedEventArgs e)
        {
            ControlLastPricePercentage = checkboxControlLastPricePercentage.IsChecked.Value;
            Filtrate(false);
        }

        private void checkboxControlLastPricePercentage_Unchecked(object sender, RoutedEventArgs e)
        {
            ControlLastPricePercentage = checkboxControlLastPricePercentage.IsChecked.Value;
            Filtrate(false);
        }

        //private void buttonInvestigateSingleAsset_Click(object sender, RoutedEventArgs e)
        //{
        //    Asset AssetSelected = (Asset)ComboBoxAsset.SelectedItem;
        //    //GetAssetHistory(sender, AssetSelected);
        //    InvestigateSmartMoney(sender, AssetSelected.Name);
        //    LabelFaultRecord.Content = "Faults: " + FaultyRecords;
        //    //DataGridSmartMoney.ItemsSource = (new ObservableCollection<SmartMoney>(ProcessAssetData(sender, AssetSelected)));
        //}

        private void buttonReload_Click(object sender, RoutedEventArgs e)
        {
            AssetList.Clear();
            LoadFreshWindow();
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            backgroundWorker_Loader.CancelAsync();
            //backgroundWorker_Loader.Dispose();
            //backgroundWorker_Loader = null;
            //GC.Collect();

            ProgressStopped = true;
            grid_Wait.Visibility = Visibility.Collapsed;
            dispatcherTimer.Stop();
        }

        private void checkboxShowOnlyToday_Checked(object sender, RoutedEventArgs e)
        {
            ListDisplayDate = checkboxShowOnlyToday.IsChecked.Value ? comboboxYear.SelectedValue + "/" + comboboxMonth.SelectedValue + "/" + comboboxDay.SelectedValue : "";
            //DataGridSmartMoney.ItemsSource = ListDisplayDate != "" ? (new ObservableCollection<SmartMoney>(SmartMoneyTotal.Where(Items => Items.Date == ListDisplayDate))) : (new ObservableCollection<SmartMoney>(SmartMoneyTotal));
            Filtrate(false);
        }

        private void checkboxShowOnlyToday_Unchecked(object sender, RoutedEventArgs e)
        {
            ListDisplayDate = "";
            //DataGridSmartMoney.ItemsSource = (new ObservableCollection<SmartMoney>(SmartMoneyTotal));
            Filtrate(false);
        }

        private void comboboxMonth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListDisplayDate = checkboxShowOnlyToday.IsChecked.Value ? comboboxYear.SelectedValue + "/" + comboboxMonth.SelectedValue + "/" + comboboxDay.SelectedValue : "";
            //DataGridSmartMoney.ItemsSource = ListDisplayDate != "" ? (new ObservableCollection<SmartMoney>(SmartMoneyTotal.Where(Items => Items.Date == ListDisplayDate))) : (new ObservableCollection<SmartMoney>(SmartMoneyTotal));
            Filtrate(false);
        }

        private void comboboxYear_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListDisplayDate = checkboxShowOnlyToday.IsChecked.Value ? comboboxYear.SelectedValue + "/" + comboboxMonth.SelectedValue + "/" + comboboxDay.SelectedValue : "";
            //DataGridSmartMoney.ItemsSource = ListDisplayDate != "" ? (new ObservableCollection<SmartMoney>(SmartMoneyTotal.Where(Items => Items.Date == ListDisplayDate))) : (new ObservableCollection<SmartMoney>(SmartMoneyTotal));
            Filtrate(false);
        }

        private void comboboxDay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListDisplayDate = checkboxShowOnlyToday.IsChecked.Value ? comboboxYear.SelectedValue + "/" + comboboxMonth.SelectedValue + "/" + comboboxDay.SelectedValue : "";
            //DataGridSmartMoney.ItemsSource = ListDisplayDate != "" ? (new ObservableCollection<SmartMoney>(SmartMoneyTotal.Where(Items => Items.Date == ListDisplayDate))) : (new ObservableCollection<SmartMoney>(SmartMoneyTotal));
            Filtrate(false);
        }

        private void DataGridSmartMoney_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LabelSelectedRows.Content = "انتخاب: " + DataGridSmartMoney.SelectedItems.Count;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DrawFlowChart();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawFlowChart();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabItemDailyFlow.IsSelected)
                DrawFlowChart();
        }

        private void canvasFlowChart_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawFlowChart();
        }

        private void RadioButtonConsiderVolume_Checked(object sender, RoutedEventArgs e)
        {
            ConsiderVolume = true;
            TextBoxDifferance.IsEnabled = RadioButtonConsiderVolume.IsChecked.Value;
            Filtrate(true);
        }

        private void RadioButtonDoNotConsiderVolume_Checked(object sender, RoutedEventArgs e)
        {
            ConsiderVolume = false;
            TextBoxDifferance.IsEnabled = RadioButtonConsiderVolume.IsChecked.Value;
            Filtrate(true);
        }

        private void TextBoxSignalRepeat_TextChanged(object sender, TextChangedEventArgs e)
        {
            int Result = 3;
            int.TryParse(TextBoxSignalRepeat.Text, out Result);
            SignalRepeat = Result;
            Filtrate(false);
        }

        private void TextBoxSignalBriefing_TextChanged(object sender, TextChangedEventArgs e)
        {
            decimal Result = 3;
            decimal.TryParse(TextBoxSignalBriefing.Text, out Result);
            SignalBriefing = Result;
            Filtrate(false);
        }

        private void TextBoxSignalBuyStrength_TextChanged(object sender, TextChangedEventArgs e)
        {
            decimal Result = (decimal)1.5;
            decimal.TryParse(TextBoxSignalBuyStrength.Text, out Result);
            SignalBuyerStrength = Result;
            Filtrate(false);
        }

        private void TextBoxSignalShareOfFreePublic_TextChanged(object sender, TextChangedEventArgs e)
        {
            decimal Result = 0;
            decimal.TryParse(TextBoxSignalShareOfFreePublic.Text, out Result);
            SignalShareOfFreePublic = Result;
            Filtrate(false);
        }

        private void TextBoxSignalBuyerCount_TextChanged(object sender, TextChangedEventArgs e)
        {
            int Result = 1;
            int.TryParse(TextBoxSignalBuyerCount.Text, out Result);
            SignalBuyerCount = Result;
            Filtrate(false);
        }

        private void TextBoxSignalCoBuy_TextChanged(object sender, TextChangedEventArgs e)
        {
            decimal Result = 0;
            decimal.TryParse(TextBoxSignalCoBuy.Text, out Result);
            SignalCoBuy = Result;
            Filtrate(false);
        }

        private void TextBoxSignalCoSell_TextChanged(object sender, TextChangedEventArgs e)
        {
            decimal Result = 0;
            decimal.TryParse(TextBoxSignalCoSell.Text, out Result);
            SignalCoSell = Result;
            Filtrate(false);
        }

        private void CoBuyGreater_Checked(object sender, RoutedEventArgs e)
        {
            CoBuyCompareGreater = CoBuyGreater.IsChecked.Value;
            if (CoBuyGreater.IsChecked.Value)
                LabelCoBuy.Content = "خرید حقوقی >= %";
            if (!CoBuyGreater.IsChecked.Value)
                LabelCoBuy.Content = "خرید حقوقی <= %";
            Filtrate(false);
        }

        private void CoBuyGreater_Unchecked(object sender, RoutedEventArgs e)
        {
            CoBuyCompareGreater = CoBuyGreater.IsChecked.Value;
            if (CoBuyGreater.IsChecked.Value)
                LabelCoBuy.Content = "خرید حقوقی >= %";
            if (!CoBuyGreater.IsChecked.Value)
                LabelCoBuy.Content = "خرید حقوقی <= %";
            Filtrate(false);
        }

        private void CoSellGreater_Checked(object sender, RoutedEventArgs e)
        {
            CoSellCompareGreater = CoSellGreater.IsChecked.Value;
            if (CoSellGreater.IsChecked.Value)
                LabelCoSell.Content = "فروش حقوقی >= %";
            if (!CoSellGreater.IsChecked.Value)
                LabelCoSell.Content = "فروش حقوقی <= %";
            Filtrate(false);
        }

        private void CoSellGreater_Unchecked(object sender, RoutedEventArgs e)
        {
            CoSellCompareGreater = CoSellGreater.IsChecked.Value;
            if (CoSellGreater.IsChecked.Value)
                LabelCoSell.Content = "فروش حقوقی >= %";
            if (!CoSellGreater.IsChecked.Value)
                LabelCoSell.Content = "فروش حقوقی <= %";
            Filtrate(false);
        }

        private void CheckBoxBuyerCount_Checked(object sender, RoutedEventArgs e)
        {
            BuyerCountGreater = !CheckBoxBuyerCount.IsChecked.Value;
            if (CheckBoxBuyerCount.IsChecked.Value)
                LabelBuyerCount.Content = "خریداران <=";
            if (!CheckBoxBuyerCount.IsChecked.Value)
                LabelBuyerCount.Content = "خریداران >=";
            Filtrate(false);
        }

        private void CheckBoxBuyerCount_Unchecked(object sender, RoutedEventArgs e)
        {
            BuyerCountGreater = !CheckBoxBuyerCount.IsChecked.Value;
            if (CheckBoxBuyerCount.IsChecked.Value)
                LabelBuyerCount.Content = "خریداران <=";
            if (!CheckBoxBuyerCount.IsChecked.Value)
                LabelBuyerCount.Content = "خریداران >=";
            Filtrate(false);
        }

        private void CheckBoxShareOfFreePublic_Checked(object sender, RoutedEventArgs e)
        {
            ShareOfFreePublic = !CheckBoxShareOfFreePublic.IsChecked.Value;
            if (CheckBoxShareOfFreePublic.IsChecked.Value)
                LabelShareOfFreePublic.Content = "معاملات <=";
            if (!CheckBoxShareOfFreePublic.IsChecked.Value)
                LabelShareOfFreePublic.Content = "معاملات >=";
            Filtrate(false);
        }

        private void CheckBoxShareOfFreePublic_Unchecked(object sender, RoutedEventArgs e)
        {
            ShareOfFreePublic = !CheckBoxShareOfFreePublic.IsChecked.Value;
            if (CheckBoxShareOfFreePublic.IsChecked.Value)
                LabelShareOfFreePublic.Content = "معاملات <=";
            if (!CheckBoxShareOfFreePublic.IsChecked.Value)
                LabelShareOfFreePublic.Content = "معاملات >=";
            Filtrate(false);
        }

        private void CheckBoxBuyerStrength_Checked(object sender, RoutedEventArgs e)
        {
            BuyerStrengthGreater = !CheckBoxBuyerStrength.IsChecked.Value;
            if (CheckBoxBuyerStrength.IsChecked.Value)
                LabelBuyerStrength.Content = "قدرت خریدار <=";
            if (!CheckBoxBuyerStrength.IsChecked.Value)
                LabelBuyerStrength.Content = "قدرت خریدار >=";
            Filtrate(false);
        }

        private void CheckBoxBuyerStrength_Unchecked(object sender, RoutedEventArgs e)
        {
            BuyerStrengthGreater = !CheckBoxBuyerStrength.IsChecked.Value;
            if (CheckBoxBuyerStrength.IsChecked.Value)
                LabelBuyerStrength.Content = "قدرت خریدار <=";
            if (!CheckBoxBuyerStrength.IsChecked.Value)
                LabelBuyerStrength.Content = "قدرت خریدار >=";
            Filtrate(false);
        }

        private void CheckBoxBriefing_Checked(object sender, RoutedEventArgs e)
        {
            BriefingGreater = !CheckBoxBriefing.IsChecked.Value;
            if (CheckBoxBriefing.IsChecked.Value)
                LabelBriefing.Content = "بریفینگ <=";
            if (!CheckBoxBriefing.IsChecked.Value)
                LabelBriefing.Content = "بریفینگ >=";
            Filtrate(false);
        }

        private void CheckBoxBriefing_Unchecked(object sender, RoutedEventArgs e)
        {
            BriefingGreater = !CheckBoxBriefing.IsChecked.Value;
            if (CheckBoxBriefing.IsChecked.Value)
                LabelBriefing.Content = "بریفینگ <=";
            if (!CheckBoxBriefing.IsChecked.Value)
                LabelBriefing.Content = "بریفینگ >=";
            Filtrate(false);
        }

        private void checkboxSignalSettings_Checked(object sender, RoutedEventArgs e)
        {
            GridSignalSettings.IsEnabled = checkboxSignalSettings.IsChecked.Value;
            SignalSettingsActive = checkboxSignalSettings.IsChecked.Value;
            Filtrate(false);
        }

        private void checkboxSignalSettings_Unchecked(object sender, RoutedEventArgs e)
        {
            GridSignalSettings.IsEnabled = checkboxSignalSettings.IsChecked.Value;
            SignalSettingsActive = checkboxSignalSettings.IsChecked.Value;
            Filtrate(false);
        }

        private void TextBoxSignalCoBuyerGreatness_TextChanged(object sender, TextChangedEventArgs e)
        {
            decimal Result = 0;
            decimal.TryParse(TextBoxSignalCoBuyerGreatness.Text, out Result);
            SignalCoBuyerGreatness = Result;
            Filtrate(false);
        }

        private void TextBoxSignalBasisVolumeGreatness_TextChanged(object sender, TextChangedEventArgs e)
        {
            decimal Result = 0;
            decimal.TryParse(TextBoxSignalBasisVolumeGreatness.Text, out Result);
            SignalBasisVolumeGreatness = Result;
            Filtrate(false);
        }

        private void CheckBoxBasisVolumeSmaller_Checked(object sender, RoutedEventArgs e)
        {
            BasisVolumeSmaller = CheckBoxBasisVolumeSmaller.IsChecked.Value;
            if (CheckBoxBasisVolumeSmaller.IsChecked.Value)
                LabelBasisVolume.Content = "حجم روز/مبنا <=";
            if (!CheckBoxBasisVolumeSmaller.IsChecked.Value)
                LabelBasisVolume.Content = "حجم روز/مبنا >=";
            Filtrate(false);
        }

        private void CheckBoxBasisVolumeSmaller_Unchecked(object sender, RoutedEventArgs e)
        {
            BasisVolumeSmaller = CheckBoxBasisVolumeSmaller.IsChecked.Value;
            if (CheckBoxBasisVolumeSmaller.IsChecked.Value)
                LabelBasisVolume.Content = "حجم روز/مبنا <=";
            if (!CheckBoxBasisVolumeSmaller.IsChecked.Value)
                LabelBasisVolume.Content = "حجم روز/مبنا >=";
            Filtrate(false);
        }

        private void CheckBoxCoBuyerGreater_Checked(object sender, RoutedEventArgs e)
        {
            CoBuyerGreater = CheckBoxCoBuyerGreater.IsChecked.Value;
            Filtrate(false);
        }

        private void CheckBoxCoBuyerGreater_Unchecked(object sender, RoutedEventArgs e)
        {
            CoBuyerGreater = CheckBoxCoBuyerGreater.IsChecked.Value;
            Filtrate(false);
        }

        private void TextBoxPerCapitaBuyValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            decimal Result = 0;
            decimal.TryParse(TextBoxPerCapitaBuyValue.Text, out Result);
            PerCapitaBuyValue = Result;
            Filtrate(false);
        }

        private void CheckBoxPerCapitaBuyValue_Checked(object sender, RoutedEventArgs e)
        {
            PerCapitaBuyValueIsGreater = !CheckBoxPerCapitaBuyValue.IsChecked.Value;
            if (!CheckBoxPerCapitaBuyValue.IsChecked.Value)
                LabelPerCapitaBuyValue.Content = "سرانه خرید (میلیون ریال) >=";
            if (CheckBoxPerCapitaBuyValue.IsChecked.Value)
                LabelPerCapitaBuyValue.Content = "سرانه خرید (میلیون ریال) <=";
            Filtrate(false);
        }

        private void CheckBoxPerCapitaBuyValue_Unchecked(object sender, RoutedEventArgs e)
        {
            PerCapitaBuyValueIsGreater = !CheckBoxPerCapitaBuyValue.IsChecked.Value;
            if (!CheckBoxPerCapitaBuyValue.IsChecked.Value)
                LabelPerCapitaBuyValue.Content = "سرانه خرید (میلیون ریال) >=";
            if (CheckBoxPerCapitaBuyValue.IsChecked.Value)
                LabelPerCapitaBuyValue.Content = "سرانه خرید (میلیون ریال) <=";
            Filtrate(false);
        }

        private void TextBoxBrifingToBuyerStrength_TextChanged(object sender, TextChangedEventArgs e)
        {
            decimal Result = 0;
            decimal.TryParse(TextBoxBrifingToBuyerStrength.Text, out Result);
            BriefingToBuyerStrength = Result;
            Filtrate(false);
        }

        private void CheckBoxBrifingToBuyerStrength_Checked(object sender, RoutedEventArgs e)
        {
            BriefingToBuyerStrengthIsGreater = !CheckBoxBrifingToBuyerStrength.IsChecked.Value;
            if (!CheckBoxBrifingToBuyerStrength.IsChecked.Value)
                LabelBrifingToBuyerStrength.Content = "بریفینگ >= ";
            if (CheckBoxBrifingToBuyerStrength.IsChecked.Value)
                LabelBrifingToBuyerStrength.Content = "بریفینگ <= ";
            Filtrate(false);
        }

        private void CheckBoxBrifingToBuyerStrength_Unchecked(object sender, RoutedEventArgs e)
        {
            BriefingToBuyerStrengthIsGreater = !CheckBoxBrifingToBuyerStrength.IsChecked.Value;
            if (!CheckBoxBrifingToBuyerStrength.IsChecked.Value)
                LabelBrifingToBuyerStrength.Content = "بریفینگ >= ";
            if (CheckBoxBrifingToBuyerStrength.IsChecked.Value)
                LabelBrifingToBuyerStrength.Content = "بریفینگ <= ";
            Filtrate(false);
        }

        private void TextBoxBriefingToBuyerStrengthDiff_TextChanged(object sender, TextChangedEventArgs e)
        {
            decimal Result = 0;
            decimal.TryParse(TextBoxBriefingToBuyerStrengthDiff.Text, out Result);
            BriefingToBuyerStrengthDiff = Result;
            Filtrate(false);
        }

        private void checkboxColorDefine_Checked(object sender, RoutedEventArgs e)
        {
            ColorDefine = checkboxColorDefine.IsChecked.Value;
            WrapPanelSpectrum.Visibility = ColorDefine ? Visibility.Visible : Visibility.Collapsed;
            Filtrate(false);
        }

        private void checkboxColorDefine_Unchecked(object sender, RoutedEventArgs e)
        {
            ColorDefine = checkboxColorDefine.IsChecked.Value;
            WrapPanelSpectrum.Visibility = ColorDefine ? Visibility.Visible : Visibility.Collapsed;
            Filtrate(false);
        }

        private void checkboxListPriorityAssets_Checked(object sender, RoutedEventArgs e)
        {
            ListPriorityAssets = checkboxListPriorityAssets.IsChecked.Value;
        }

        private void checkboxListPriorityAssets_Unchecked(object sender, RoutedEventArgs e)
        {
            ListPriorityAssets = checkboxListPriorityAssets.IsChecked.Value;
        }

        private void checkboxListInvestmentFunds_Unchecked(object sender, RoutedEventArgs e)
        {
            ListInvestmentFunds = checkboxListInvestmentFunds.IsChecked.Value;
        }

        private void checkboxListInvestmentFunds_Checked(object sender, RoutedEventArgs e)
        {
            ListInvestmentFunds = checkboxListInvestmentFunds.IsChecked.Value;
        }

        private void checkboxListInvestmentCos_Unchecked(object sender, RoutedEventArgs e)
        {
            ListInvestmentCos = checkboxListInvestmentCos.IsChecked.Value;
        }

        private void checkboxListInvestmentCos_Checked(object sender, RoutedEventArgs e)
        {
            ListInvestmentCos = checkboxListInvestmentCos.IsChecked.Value;
        }

        private void checkboxSingleAsset_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void checkboxSingleAsset_Unchecked(object sender, RoutedEventArgs e)
        {
            ComboBoxAsset.Text = "";
            Filtrate(true);
        }

        private void checkboxPtoE_Checked(object sender, RoutedEventArgs e)
        {
            if (checkboxPtoE.IsChecked.HasValue)
            {
                ConsiderPtoE = checkboxPtoE.IsChecked.Value;
                Filtrate(true);
            }
        }

        private void checkboxPtoE_Unchecked(object sender, RoutedEventArgs e)
        {
            if (checkboxPtoE.IsChecked.HasValue)
            {
                ConsiderPtoE = checkboxPtoE.IsChecked.Value;
                Filtrate(true);
            }
        }

        private void TextBoxMarketGrowth_TextChanged(object sender, TextChangedEventArgs e)
        {
            decimal Result = 1000;
            decimal.TryParse(TextBoxMarketGrowth.Text, out Result);
            ConsideredMarketGrowth = Result;
            Filtrate(false);
        }

        private void TextBoxSignalPtoEMax_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TextBoxSignalPtoEMax.Text != "")
                PtoEMax = decimal.Parse(TextBoxSignalPtoEMax.Text);
            Filtrate(false);
        }

        private void ButtonClearHistoryPeriods_Click(object sender, RoutedEventArgs e)
        {
            LabelCompareHistoryDays.Content = "";
        }

        private void ButtonAddHistoryPeriod_Click(object sender, RoutedEventArgs e)
        {
            int CompareTo = 0;
                if (int.TryParse(TextBoxCompareHistoryDays.Text, out CompareTo))
                {
                if (DaysToGoBack > CompareTo)
                    LabelCompareHistoryDays.Content += LabelCompareHistoryDays.Content.ToString().CompareTo("") > 0 ? "," + CompareTo : CompareTo.ToString();
                else
                    MessageBox.Show("تاریخ انتخابی باید از دوره اصلی کوچکتر باشد");
                }
        }

        private void buttonToken_Click(object sender, RoutedEventArgs e)
        {
            WindowToken tokenWin = new WindowToken();
            tokenWin.ShowDialog();
        }
    }
}
