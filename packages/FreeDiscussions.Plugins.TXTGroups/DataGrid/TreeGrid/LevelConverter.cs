// ------------------------------------------------------
// ---------- Copyright (c) 2017 Colton Murphy ----------
// ------------------------------------------------------
// ------------------------------------------------------
// ------------------------------------------------------
// ------------------------------------------------------
// ------------------------------------------------------
// ------------------------------------------------------
// ------------------------------------------------------
// ------------------------------------------------------
// ------------------------------------------------------
// ------------------------------------------------------
// ------------------------------------------------------

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FreeDiscussions.Plugins.TXTGroups.DataGrid
{
	public class LevelConverter : IValueConverter
	{
		public GridLength LevelWidth { get; set; }

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			// Return the width multiplied by the level
			return ((int)value * LevelWidth.Value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}