namespace Spi
{
    //public class AssetData
    //{
    //    public string Date { get; set; }
    //    public String Time { get; set; }
    //    public decimal Open { get; set; }
    //    public decimal High { get; set; }
    //    public decimal Low { get; set; }
    //    public decimal Close { get; set; }
    //    public decimal Volume { get; set; }
    //    public decimal MoneyFlow { get; set; }
    //}

    public class Asset
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string AssetSign { get; set; }

        public string Date { get; set; }
        public int EPS { get; set; }
        public decimal PtoE { get; set; }

        public int High { get; set; }
        public int Low { get; set; }
        public int Close { get; set; }
        public int Open { get; set; }

        public int Final { get; set; }

        public decimal LastPricePercentage { get; set; }
        /// <summary>
        /// تعداد معاملات
        /// </summary>
        public int TradedCount { get; set; }
        /// <summary>
        /// حجم معاملات
        /// </summary>
        public decimal TradedVolume { get; set; }

        /// <summary>
        /// ارزش معاملات
        /// </summary>
        public decimal TradedValue { get; set; }
        
        public decimal AverageVolume { get; set; }

        public decimal CoBuyVolume { get; set; }
        public decimal CoSellVolume { get; set; }
        public decimal CoBuyCount { get; set; }
        public decimal CoSellCount { get; set; }
        public decimal RealBuyVolume { get; set; }
        public decimal RealSellVolume { get; set; }
        public decimal RealBuyCount { get; set; }
        public decimal RealSellCount { get; set; }

        public decimal RealSellValue { get; set; }
        public decimal RealBuyValue { get; set; }
        public decimal CoSellValue { get; set; }
        public decimal CoBuyValue { get; set; }
        
        public decimal StockCount { get; set; }
        public decimal PublicFloat { get; set; }
        public int BasisVolume { get; set; }

        public Asset[] History { get; set; }
    }
}
