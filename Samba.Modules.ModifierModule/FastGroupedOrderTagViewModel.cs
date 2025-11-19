using System.Collections.Generic;
using System.Linq;
using Samba.Domain.Models.Tickets;

namespace Samba.Modules.ModifierModule
{
    public class FastGroupedOrderTagViewModel
    {
        public string Name { get; set; }
        public IEnumerable<FastGroupedOrderTagButtonViewModel> OrderTags { get; set; }
        public int ColumnCount { get; set; }
        public int ButtonHeight { get; set; }

        public FastGroupedOrderTagViewModel(Order selectedItem, IGrouping<string, OrderTagGroup> orderTagGroups)
        {
            Name = orderTagGroups.Key;
            OrderTags = orderTagGroups
                .Select(x => new FastGroupedOrderTagButtonViewModel(selectedItem, x))
                .ToList();

            var first = orderTagGroups.First();
            ColumnCount = first.ColumnCount;
            ButtonHeight = first.ButtonHeight;
        }
    }
}
