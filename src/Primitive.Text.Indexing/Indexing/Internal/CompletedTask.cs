using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primitive.Text.Indexing.Internal
{
    internal static class CompletedTask
    {
        public static readonly Task Instance = Task.FromResult<object>(null);
    }
}
