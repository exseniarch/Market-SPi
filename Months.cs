namespace Spi
{
    public class Months
    {
        public string Name { get; set; }
        public string Number { get; set; }

        public Months(string MonthName, string MonthNumber)
        {
            Name = MonthName;
            Number = MonthNumber;
        }
    }
}
