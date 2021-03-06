﻿using System;
using System.Windows;

namespace CodeBase.Domain.Services
{
    public class MessageHelper
    {
        /// <summary>Info message box</summary>
        public static void Alert(string text, string title = "Alert") =>
            MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Information);

        /// <summary>Warning message box</summary>
        public static void Warning(string text, string title = "Warning") =>
            MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Warning);

        /// <summary>Error message box</summary>
        public static void Error(string text, string title = "Error") =>
            MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Error);

        /// <summary>Displayes exception content</summary>
        public static void ThrowException(Exception exception) =>
            Error(exception.ToString(), exception.GetType().Name);
    }
}
