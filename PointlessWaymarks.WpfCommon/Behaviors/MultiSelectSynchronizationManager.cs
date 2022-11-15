﻿using System.Collections;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace PointlessWaymarks.WpfCommon.Behaviors
{
    public class MultiSelectSynchronizationManager
    {
        private readonly Selector _multiSelector;
        private MultiSelectTwoListSynchronizer _synchronizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiSelectSynchronizationManager"/> class.
        /// </summary>
        /// <param name="selector">The selector.</param>
        internal MultiSelectSynchronizationManager(Selector selector)
        {
            _multiSelector = selector;
        }

        /// <summary>
        /// Starts synchronizing the list.
        /// </summary>
        public void StartSynchronizingList()
        {
            IList list = MultiSelectorBehaviours.GetSynchronizedSelectedItems(_multiSelector);

            if (list != null)
            {
                _synchronizer = new MultiSelectTwoListSynchronizer(GetSelectedItemsCollection(_multiSelector), list);
                _synchronizer.StartSynchronizing();
            }
        }

        /// <summary>
        /// Stops synchronizing the list.
        /// </summary>
        public void StopSynchronizing()
        {
            _synchronizer.StopSynchronizing();
        }

        public static IList GetSelectedItemsCollection(Selector selector)
        {
            if (selector is MultiSelector)
            {
                return (selector as MultiSelector).SelectedItems;
            }
            else if (selector is ListBox)
            {
                return (selector as ListBox).SelectedItems;
            }
            else
            {
                throw new InvalidOperationException("Target object has no SelectedItems property to bind.");
            }
        }
    }
}
