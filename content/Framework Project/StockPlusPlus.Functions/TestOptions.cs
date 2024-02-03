using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPlusPlus.Functions;

internal class TestOptions
{
    public TestOptions(string message)
    {
        Message = message;
    }

    public string Message { get; }
}
