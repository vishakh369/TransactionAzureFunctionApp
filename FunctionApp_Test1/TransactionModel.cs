using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionApp_Test1
{
    internal class TransactionModel
    {
        public int TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Direction { get; set; }
        public int AccountId { get; set; }

    }

    internal class WalletModel
    {
        public int WalletId { get; set; }
        public decimal BalanceAmt { get; set; }
    }
}
