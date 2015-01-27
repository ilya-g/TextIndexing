using System;
using System.Collections.Generic;

namespace Primitive.Text.Indexing.Internal
{
    internal class StringComparisonComparer : IComparer<string>
    {
        private readonly StringComparison comparisonType;

        public StringComparisonComparer(StringComparison comparisonType)
        {
            this.comparisonType = comparisonType;
        }

        public StringComparison ComparisonType { get { return comparisonType; } }

        public int Compare(string x, string y)
        {
            return string.Compare(x, y, comparisonType);
        }

    }
}