using System.Windows.Media;

namespace Spi
{
    public class SmartMoney
    {
        public string AssetName { get; set; }
        public string FullName { get; set; }
        public string Date { get; set; }

        public double Spectrum { get; set; }
        public decimal PtoE { get; set; }

        public int High { get; set; }
        public int Low { get; set; }
        public int Close { get; set; }
        public int Open { get; set; }
        public int Final { get; set; }
        public decimal LastPricePercentage { get; set; }
        /// <summary>
        /// Real Buyers participation by percent
        /// </summary>
        public decimal RealBuyPart { get; set; }

        /// <summary>
        /// Real Sellers participation by percent
        /// </summary>
        public decimal RealSellPart { get; set; }

        public decimal RealPerCodeBuyValue { get; set; }
        public decimal RealPerCodeSellValue { get; set; }

        public decimal RealBuyerNumber { get; set; }
        public decimal RealSellerNumber { get; set; }

        /// <summary>
        /// Co Buyers participation by percent
        /// </summary>
        public decimal CoBuyPart { get; set; }

        /// <summary>
        /// Co Sellers participation by percent
        /// </summary>
        public decimal CoSellPart { get; set; }

        /// <summary>
        /// Count of Buyers to number of Sellers: lower than 1 more chance of Smart Money flowed into the stock
        /// </summary>
        public decimal BuyerSellerRatio { get; set; } //The less the number is, the more Smart Money probable

        /// <summary>
        /// Price that each buyer paid to the price each seller paid: The less the value is, the more Smart Money flowed into the stock
        /// </summary>
        

        public decimal RealSellVolume { get; set; }
        public decimal RealSellValue { get; set; }
        public decimal RealBuyVolume { get; set; }
        public decimal RealBuyValue { get; set; }

        public decimal RealPeriodInOut { get; set; }
        public decimal RealInOut { get; set; }
        public decimal RealPerCodeSellVolume { get; set; }
        public decimal RealPerCodeBuyVolume { get; set; }

        public decimal BriefingStrength { get; set; }
        public decimal RealBuyToSellVolume { get; set; }
        public decimal RealBuyToSellValue { get; set; } //The greater the number is 

        public decimal TradedVolume { get; set; }
        public decimal TradedVolumePercent { get; set; }
        public decimal AverageVolume { get; set; }
        public decimal AverageVolumePercent { get; set; }

        public decimal TotalTradedVolume { get; set; }
        public decimal TotalTradedToStockCount { get; set; }
        public decimal TotalTradedToPublicFloat { get; set; }

        public decimal VolumeGrowthRatio {get; set;}
        public decimal StockCount { get; set; }
        public decimal PublicFloat { get; set; }
        public decimal PublicFloatPercentage { get; set; }
        public int BasisVolume { get; set; }
        public decimal ShareOfPublicFloat { get; set; }
        public int RepeatCount { get; set; }

        public decimal Density { get; set; }

        public Brush BackgroundColor { get; set; }

        public decimal MarketCapital { get; set; }
        //public decimal StartCap { get; set; }
        //public decimal EndCap { get; set; }

        public decimal MarketGrowth { get; set; }
        public decimal MarketGrowthPercentage { get; set; }
    }
}
