﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace MySnooper
{
    public static class ExtensionMethods
    {
        public static void DeSerialize<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, string str)
        {
            string[] list = str.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < list.Length; i++)
                dictionary.Add((TKey)(object)list[i].ToLower(), (TValue)(object)list[i]);
        }

        public static string Serialize<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var item in dictionary)
            {
                sb.Append(item.Value.ToString());
                sb.Append(',');
            }
            return sb.ToString();
        }

        public static void AddValueChanged(this DependencyProperty property, object sourceObject, EventHandler handler)
        {
            DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(property, property.OwnerType);
            dpd.AddValueChanged(sourceObject, handler);
        }
    }
}