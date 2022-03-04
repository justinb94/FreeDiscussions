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

using FreeDiscussions.Plugins.TXTGroups.DataGrid;
using System;
using System.Windows;
using Usenet.Nntp.Responses;

namespace FreeDiscussions.Plugins.TXTGroups
{
    public class Item : TreeGridElement
	{
		public string Subject { get; set; }
		public string From { get; set; }
		public DateTime Date { get; set; }
		public NntpArticleResponse Article { get; set; }

        public static readonly DependencyProperty IsReadProperty = DependencyProperty.Register(
        name: "IsRead",
        propertyType: typeof(bool),
        ownerType: typeof(Item),
        typeMetadata: new FrameworkPropertyMetadata(defaultValue: false));

        public bool IsRead
        {
            get => (bool)GetValue(IsReadProperty);
            set => SetValue(IsReadProperty, value);
        }
    }
}