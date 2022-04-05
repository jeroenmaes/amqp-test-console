using System;
using System.Collections.Generic;
using System.Text;

namespace AmqpTest
{
    public static class MessageStatistics
    {
        public static long TotalSentMessages { get; set; }
        public static long TotalReceivedMessages { get; set; }

    }
}
