using System;
using System.Collections.Generic;
using System.Text;

namespace FreeDiscussions.Plugins.TXTGroups
{
	/// <summary>
	///		Parse (and format) Dates in Rfc822 Format, e.g.
	///			Mon, 29 Dec 2003 16:27:03 +0100
	///			
	///		The DateTime.Parse method seems to have difficulties
	///		with the timezone.	
	/// </summary>
	public class Rfc822Date
	{
		private Rfc822Date()
		{
		}

		public static string Format(DateTime date)
		{
			StringBuilder buf = new StringBuilder();
			buf.Append(DAY_NAMES[(int)date.DayOfWeek]);
			buf.Append(", ");
			buf.Append(date.Day);
			buf.Append(" ");
			buf.Append(MONTH_NAMES[date.Month - 1]);
			buf.Append(" ");
			buf.Append(date.Year);
			buf.Append(" ");
			TwoDigit(buf, date.Hour);
			buf.Append(":");
			TwoDigit(buf, date.Minute);
			buf.Append(":");
			TwoDigit(buf, date.Second);
			buf.Append(" ");

			// timezone
			int totalMin = (int)TimeZone.CurrentTimeZone.GetUtcOffset(date).TotalMinutes;
			buf.Append((totalMin >= 0 ? '+' : '-'));
			totalMin = Math.Abs(totalMin);
			TwoDigit(buf, totalMin / 60);
			TwoDigit(buf, totalMin % 60);

			return buf.ToString();
		}

		private static void TwoDigit(StringBuilder buf, int value)
		{
			buf.Append((char)('0' + (value / 10)));
			buf.Append((char)('0' + (value % 10)));
		}
		public static int ToInt(string str, int defaultValue)
		{
			int a;

			if (!int.TryParse(str, out a))
				return defaultValue;

			return a;
		}


		public static DateTime Parse(string str)
		{
			if (String.IsNullOrEmpty(str))
				return DateTime.MinValue;

			int day = -1;
			int month = -1;
			int year = -1;
			int hour = -1;
			int minute = -1;
			int second = -1;
			int timezone = 0;

			str = str.ToLower();

			string[] split = str.Split(" ".ToCharArray());
			foreach (string _cur in split)
			{
				string cur = _cur.Trim();
				int num = ToInt(cur, -1);

				// day
				if (day == -1 && num >= 1 && num <= 31 && cur.Length <= 2)
				{
					day = num;
					continue;
				}

				// year
				if (year == -1 && cur.Length == 2)
				{
					year = (num > 70 ? 1900 : 2000) + num;
					continue;
				}

				if (year == -1 && cur.Length == 4 && num > 1970 && num < 2100)
				{
					year = num;
					continue;
				}

				// hour
				if (hour == -1 && num == -1 && cur.IndexOf(':') != -1)
				{
					string[] time = cur.Split(":".ToCharArray());
					hour = ToInt(time[0], -1);
					minute = ToInt(time[1], -1);
					second = 0;
					if (time.Length >= 3)
						second = ToInt(time[2], 0);
					continue;
				}

				// month name				
				if (month == -1 && num == -1 && cur.Length >= 3)
				{
					for (int i = 0; i < MONTH_NAMES.Length; i++)
					{
						if (cur.StartsWith(MONTH_NAMES[i]))
						{
							month = i + 1;
							break;
						}
					}
					if (month != -1)
						continue;
				}

				// timezone
				if ((cur.StartsWith("+") || cur.StartsWith("-")) && cur.Length == 5)
				{
					timezone = (num / 100) * 60 + (num % 100);
					continue;
				}

				if (num == -1 && cur.Length >= 3)
				{
					for (int i = 0; i < ZONE_NAMES.Length; i++)
					{
						if (ZONE_NAMES[i] == cur)
						{
							timezone = ZONE_DIFF[i] * 60;
							break;
						}
					}
				}
			}

			if (day == -1 || month == -1 || year == -1 || hour == -1 || minute == -1)
				return DateTime.MinValue;

			DateTime dateTime = new DateTime(year, month, day, hour, minute, second, 0);
			dateTime = dateTime.AddMinutes(-timezone);

			DateTime result = dateTime.ToLocalTime();
			return result;
		}

		private static string[] MONTH_NAMES =
		{
			"jan", "feb", "mar", "apr",
			"may",  "jun" , "jul" , "aug",
			"sep",  "oct",  "nov",  "dec"
		};

		private static string[] ZONE_NAMES =
			{ "ut", "gmt", "est", "edt", "cst", "cdt", "mst", "mdt", "pst", "pdt" };

		public static int[] ZONE_DIFF =
			{   0,   0,    -5,   -4,     -6,    -5,   -7,   -6,      -8, -7 };

		public static string[] DAY_NAMES =
		{
			"sun", "mon", "tue", "wed", "thu", "fri", "sat"
		};
	}
}

