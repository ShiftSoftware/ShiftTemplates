namespace StockPlusPlus.Web.Pages.WarrantyClaim
{
    public static class SystemExtentsions
    {
        public static string ToCurrencyFormat(this decimal value)
        {
            return value.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-us"));
        }
        public static string? ToCurrencyFormat(this decimal? value)
        {
            if (value is null)
                return null;

            return value.Value.ToCurrencyFormat();
        }


        public static string ToJPYCurrencyFormat(this decimal value)
        {
            return value.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("ja-JP"));
        }
        public static string? ToJPYCurrencyFormat(this decimal? value)
        {
            if (value is null)
                return null;

            return value.Value.ToJPYCurrencyFormat();
        }
    }
}
