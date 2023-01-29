﻿//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Kermalis.VGMusicStudio.WinForms.API.Taskbar
{
    /// <summary>
    /// Represents a custom category on the taskbar's jump list
    /// </summary>
    public class JumpListCustomCategory
    {
        private string name;

        internal JumpListItemCollection<IJumpListItem> JumpListItems
        {
            get;
            private set;
        }

        /// <summary>
        /// Category name
        /// </summary>
        public string Name
        {
            get => name;
            set
            {
                if (value != name)
                {
                    name = value;
                    CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }
        }


        /// <summary>
        /// Add JumpList items for this category
        /// </summary>
        /// <param name="items">The items to add to the JumpList.</param>
        public void AddJumpListItems(params IJumpListItem[] items)
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    JumpListItems.Add(item);
                }
            }
        }

        /// <summary>
        /// Event that is triggered when the jump list collection is modified
        /// </summary>
        internal event NotifyCollectionChangedEventHandler CollectionChanged = delegate { };

        /// <summary>
        /// Creates a new custom category instance
        /// </summary>
        /// <param name="categoryName">Category name</param>
        public JumpListCustomCategory(string categoryName)
        {
            Name = categoryName;

            JumpListItems = new JumpListItemCollection<IJumpListItem>();
            JumpListItems.CollectionChanged += OnJumpListCollectionChanged;
        }

        internal void OnJumpListCollectionChanged(object sender, NotifyCollectionChangedEventArgs args) => CollectionChanged(this, args);


        internal void RemoveJumpListItem(string path)
        {
            var itemsToRemove = new List<IJumpListItem>(
                from i in JumpListItems
                where string.Equals(path, i.Path, StringComparison.OrdinalIgnoreCase)
                select i);

            // Remove matching items
            for (var i = 0; i < itemsToRemove.Count; i++)
            {
                JumpListItems.Remove(itemsToRemove[i]);
            }
        }
    }
}
