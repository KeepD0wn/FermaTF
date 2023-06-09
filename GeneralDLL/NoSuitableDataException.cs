using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralDLL
{
    public class NoSuitableDataException : ArgumentException
    {
        public NoSuitableDataException(string message)
        : base(message)
        { }
    }
}
