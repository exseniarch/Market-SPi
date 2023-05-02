using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Spi
{
    public class DateTools
    {
        public static string MonthName(int nMonth)
        {
            switch (nMonth)
            {
                case 1:
                    return "فروردین";
                case 2:
                    return "اردیبهشت";
                case 3:
                    return "خرداد";
                case 4:
                    return "تیر";
                case 5:
                    return "مرداد";
                case 6:
                    return "شهریور";
                case 7:
                    return "مهر";
                case 8:
                    return "آبان";
                case 9:
                    return "آذر";
                case 10:
                    return "دی";
                case 11:
                    return "بهمن";
                case 12:
                    return "اسفند";
            }
            return "";
        }

         
        public static string ADMonthName(int nMonth)
        {
            switch (nMonth)
            {
                case 1:
                    return "ژانویه";
                case 2:
                    return "فوریه";
                case 3:
                    return "مارچ";
                case 4:
                    return "اپریل";
                case 5:
                    return "می";
                case 6:
                    return "جون";
                case 7:
                    return "جولای";
                case 8:
                    return "اوگوست";
                case 9:
                    return "سپتامبر";
                case 10:
                    return "اکتبر";
                case 11:
                    return "نوامبر";
                case 12:
                    return "دسامبر";
            }
            return "";
        }

         
        public static string WeekDayToPersian(string strEnglishDay)
        {
            switch (strEnglishDay)
            {
                case "Monday":
                    return "دوشنبه";
                case "Tuesday":
                    return "سه شنبه";
                case "Wednesday":
                    return "چهارشنبه";
                case "Thursday":
                    return "پنجشنبه";
                case "Friday":
                    return "جمعه";
                case "Saturday":
                    return "شنبه";
                case "Sunday":
                    return "یکشنبه";
            }
            return "";
        }

         
        public static string PersianWeekDay(string strPersianDate)
        {
            PersianCalendar perCal = new PersianCalendar();

            int nYear = int.Parse(strPersianDate.ToString().Split('-')[0].Split('/')[0]);
            int nMonth = int.Parse(strPersianDate.ToString().Split('-')[0].Split('/')[1]);
            int nDay = int.Parse(strPersianDate.ToString().Split('-')[0].Split('/')[2]);

            int nHour = int.Parse(strPersianDate.ToString().Split('-')[1].Split(':')[0]);
            int nMinute = int.Parse(strPersianDate.ToString().Split('-')[1].Split(':')[1]);
            int nSecond = int.Parse(strPersianDate.ToString().Split('-')[1].Split(':')[2]);

            return WeekDayToPersian(perCal.ToDateTime(nYear, nMonth, nDay, nHour, nMinute, nSecond, 0).DayOfWeek.ToString());
        }

        //
        // Summary:
        //تاریخ امروز را بر می گرداند
        // Returns:
        //     A System.DateTime whose value is the current local date and time.
         
        public static string PersianToday()
        {
            PersianCalendar perDate = new PersianCalendar();
            return perDate.GetYear(DateTime.Now).ToString("0000") + "/" + perDate.GetMonth(DateTime.Now).ToString("00") + "/" + perDate.GetDayOfMonth(DateTime.Now).ToString("00");
        }

        /// <summary>
        /// Time with hh:mm:ss format
        /// </summary>
         
        public static string TimeLabel()
        {
            return DateTime.Now.Hour.ToString("00") + ":" + DateTime.Now.Minute.ToString("00") + ":" + DateTime.Now.Second.ToString("00");
        }

        /// <summary>
        /// Persian DateTime with yyyy/mm/dd-hh:mm:ss format
        /// </summary>
         
        public static string PersianNow()
        {
            return PersianToday() + "-" + TimeLabel();
        }

        /// <summary>
        /// Persian DateTime with yyyymmddhhmmss format
        /// </summary>
         
        public static string DBSortablePersianNow()
        {
            PersianCalendar perDate = new PersianCalendar();
            DateTime RightNow = DateTime.Now;
            return perDate.GetYear(RightNow).ToString("0000") + perDate.GetMonth(RightNow).ToString("00") + perDate.GetDayOfMonth(RightNow).ToString("00") + RightNow.Hour.ToString("00") + RightNow.Minute.ToString("00") + RightNow.Second.ToString("00");
        }

         
        public static string PersianNowGoForward(int SecondsForward)
        {
            return ForwardFromNow(SecondsForward);
        }

         
        private static string ForwardFromNow(int SecondsForward)
        {
            DateTime RightNow = DateTime.Now;
            RightNow = RightNow.AddSeconds(1.0);
            PersianCalendar perDate = new PersianCalendar();
            return perDate.GetYear(RightNow).ToString("0000") + "/" + perDate.GetMonth(RightNow).ToString("00") + "/" + perDate.GetDayOfMonth(RightNow).ToString("00") + "-" + RightNow.Hour.ToString("00") + ":" + RightNow.Minute.ToString("00") + ":" + RightNow.Second.ToString("00");
        }

        /// <summary>
        /// Checks if the RegularDate string is in accptable format. (e.g. 1390/1/12 or 1390/01/12)
        /// </summary>
        /// <param name="strDate">Regular Date String</param>
        /// <returns></returns>
         
        public static bool ValidateFormat(string RegularDate)
        {
            try
            {
                string Result = int.Parse(RegularDate.Split('/')[0]).ToString("0000") + "/" + int.Parse(RegularDate.Split('/')[1]).ToString("00") + "/" + int.Parse(RegularDate.Split('/')[2]).ToString("00");
                return true;
            }
            catch (Exception exp)
            {
                return false;
            }
        }

        /// <summary>
        /// Converts RegularDateString to SortableFormat e.g.From 1391/2/15 to 1391/02/15
        /// </summary>
        /// <param name="strDate">Regular Date String</param>
        /// <returns></returns>
         
        public static string SortableDate(string RegularDate)
        {
            return int.Parse(RegularDate.Split('/')[0]).ToString("0000") + "/" + int.Parse(RegularDate.Split('/')[1]).ToString("00") + "/" + int.Parse(RegularDate.Split('/')[2]).ToString("00");
        }

        /// <summary>
        /// Converts a date string into correct format
        /// </summary>
        /// <param name="strDate"></param>
        /// <param name="CorrectDirection">If true, first and last positions splittd by / would be exchanged and then folded</param>
        /// <returns></returns>
         
        public static string SortableDateCorrectDirection(string strDate)
        {
            return int.Parse(strDate.Split('/')[2]).ToString("0000") + "/" + int.Parse(strDate.Split('/')[1]).ToString("00") + "/" + int.Parse(strDate.Split('/')[0]).ToString("00");
            //return int.Parse(strDate.Split('/')[0]).ToString("0000") + "/" + int.Parse(strDate.Split('/')[1]).ToString("00") + "/" + int.Parse(strDate.Split('/')[2]).ToString("00");
        }

         
        public static bool IsPersianDate(string StringToCheck)
        {
            return Regex.IsMatch(" " + StringToCheck.Trim() + " ", " [0-9]{4,4}/([0-1]{0,1}[0-9]{1,1})/([0-3]{0,1}[0-9]{1,1}) ") || Regex.IsMatch(" " + StringToCheck.Trim() + " ", " ([0-3]{0,1}[0-9]{1,1})/([0-1]{0,1}[0-9]{1,1})/[0-9]{4,4} ");
        }

         
        public static bool HasCorrectDirection(string StringToCheck)
        {
            if (Regex.IsMatch(" " + StringToCheck.Trim() + " ", " [0-9]{4,4}/([0-1]{0,1}[0-9]{1,1})/([0-3]{0,1}[0-9]{1,1}) "))
                return true;
            else if (Regex.IsMatch(" " + StringToCheck.Trim() + " ", " ([0-3]{0,1}[0-9]{1,1})/([0-1]{0,1}[0-9]{1,1})/[0-9]{4,4} "))
                return false;
            return false;
        }

         
        public static bool WrongDateDirection(string StringToCheck)
        {
            return Regex.IsMatch(" " + StringToCheck.Trim() + " ", " [0-9]{4,4}/([0-1]{0,1}[0-9]{1,1})/([0-3]{0,1}[0-9]{1,1}) ");
        }

         
        public static string SortableTime(string strTime)
        {
            if (strTime.Split(':').Length == 3)
                return int.Parse(strTime.Split(':')[0]).ToString("00") + ":" + int.Parse(strTime.Split(':')[1]).ToString("00") + ":" + int.Parse(strTime.Split(':')[2]).ToString("00");
            else
                return int.Parse(strTime.Split(':')[0]).ToString("00") + ":" + int.Parse(strTime.Split(':')[1]).ToString("00") + ":00";
        }

         
        public static string ADToJalali(int nYear, int nMonth, int nDay)
        {
            PersianCalendar perDate = new PersianCalendar();
            DateTime dtDate = new DateTime(nYear, nMonth, nDay);
            return perDate.GetYear(dtDate).ToString("0000") + "/" + perDate.GetMonth(dtDate).ToString("00") + "/" + perDate.GetDayOfMonth(dtDate).ToString("00");
        }

         
        public static string ADDateTimeToJalali(DateTime AnnoDomini)
        {
            PersianCalendar perDate = new PersianCalendar();
            return perDate.GetYear(AnnoDomini).ToString("0000") + "/" + perDate.GetMonth(AnnoDomini).ToString("00") + "/" + perDate.GetDayOfMonth(AnnoDomini).ToString("00") + "-" + perDate.GetHour(AnnoDomini).ToString("00") + ":" + perDate.GetMinute(AnnoDomini).ToString("00") + ":" + perDate.GetSecond(AnnoDomini).ToString("00");
        }

         
        public static string DateTimeToPersian(DateTime AnnoDomini)
        {
            PersianCalendar perDate = new PersianCalendar();
            return perDate.GetYear(AnnoDomini).ToString("0000") + "/" + perDate.GetMonth(AnnoDomini).ToString("00") + "/" + perDate.GetDayOfMonth(AnnoDomini).ToString("00") + "-" + perDate.GetHour(AnnoDomini).ToString("00") + ":" + perDate.GetMinute(AnnoDomini).ToString("00") + ":" + perDate.GetSecond(AnnoDomini).ToString("00");
        }

         
        public static string ToPersian(DateTime AnnoDomini, string SelectedTime)
        {
            PersianCalendar perDate = new PersianCalendar();
            return perDate.GetYear(AnnoDomini).ToString("0000") + "/" + perDate.GetMonth(AnnoDomini).ToString("00") + "/" + perDate.GetDayOfMonth(AnnoDomini).ToString("00") + "-" + SelectedTime;
        }


        /// <summary>
        /// Returns 
        /// </summary>
        /// <param name="AnnoDomini"></param>
        /// <returns></returns>
         
        public static string GetDate(DateTime AnnoDomini)
        {
            PersianCalendar perDate = new PersianCalendar();
            return perDate.GetYear(AnnoDomini).ToString("0000") + "/" + perDate.GetMonth(AnnoDomini).ToString("00") + "/" + perDate.GetDayOfMonth(AnnoDomini).ToString("00");
        }

         
        public static string GetTime(DateTime AnnoDomini)
        {
            PersianCalendar perDate = new PersianCalendar();
            return perDate.GetHour(AnnoDomini).ToString("00") + ":" + perDate.GetMinute(AnnoDomini).ToString("00") + ":" + perDate.GetSecond(AnnoDomini).ToString("00");
        }

         
        public static string ToSortablePersian(DateTime AnnoDomini)
        {
            PersianCalendar perDate = new PersianCalendar();
            return perDate.GetYear(AnnoDomini).ToString("0000") + "/" + perDate.GetMonth(AnnoDomini).ToString("00") + "/" + perDate.GetDayOfMonth(AnnoDomini).ToString("00") + "-" + perDate.GetHour(AnnoDomini).ToString("00") + ":" + perDate.GetMinute(AnnoDomini).ToString("00") + ":" + perDate.GetSecond(AnnoDomini).ToString("00");
        }

        public static string ToDBSortablePersian(DateTime AnnoDomini)
        {
            PersianCalendar perDate = new PersianCalendar();
            return perDate.GetYear(AnnoDomini).ToString("0000") + perDate.GetMonth(AnnoDomini).ToString("00") + perDate.GetDayOfMonth(AnnoDomini).ToString("00") + perDate.GetHour(AnnoDomini).ToString("00") + perDate.GetMinute(AnnoDomini).ToString("00") + perDate.GetSecond(AnnoDomini).ToString("00");
        }

         
        public static DateTime SortableToAD(string SortablePersianDate)
        {
            PersianCalendar per = new PersianCalendar();
            //if(SortablePersianDate.Length == 10)
            //    return per.ToDateTime(int.Parse(SortablePersianDate.Split('-')[0].Split('/')[0]), int.Parse(SortablePersianDate.Split('-')[0].Split('/')[1]), int.Parse(SortablePersianDate.Split('-')[0].Split('/')[2]), 0, 0, 0, 0);
            //else
            return per.ToDateTime(int.Parse(SortablePersianDate.Split('-')[0].Split('/')[0]), int.Parse(SortablePersianDate.Split('-')[0].Split('/')[1]), int.Parse(SortablePersianDate.Split('-')[0].Split('/')[2]), int.Parse(SortablePersianDate.Split('-')[1].Split(':')[0]), int.Parse(SortablePersianDate.Split('-')[1].Split(':')[1]), int.Parse(SortablePersianDate.Split('-')[1].Split(':')[2]), 0);
        }
    }
}
