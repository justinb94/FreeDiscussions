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

using FreeDiscussions.Plugins.Download.DataGrid;
using System;
using System.Windows;
using Usenet.Nntp.Responses;

namespace FreeDiscussions.Plugins.Download
{
    public class Item : TreeGridElement
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string SizeLoaded { get; set; }
        public string SizeTotal { get; set; }

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

    public class TopItem : TreeGridElement
    {
        public static readonly DependencyProperty NameProperty = DependencyProperty.Register(
        name: "Name",
        propertyType: typeof(string),
        ownerType: typeof(TopItem),
        typeMetadata: new FrameworkPropertyMetadata(defaultValue: false));

        public string Name
        {
            get => (string)GetValue(NameProperty);
            set => SetValue(NameProperty, value);
        }

        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        name: "Status",
        propertyType: typeof(string),
        ownerType: typeof(TopItem),
        typeMetadata: new FrameworkPropertyMetadata(defaultValue: false));

        public string Status
        {
            get => (string)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }
    }
}