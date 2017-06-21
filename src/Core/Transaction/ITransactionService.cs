using System.Threading.Tasks;
using QBitNinja.Client.Models;

namespace Core.Transaction
{
    public interface ITransactionService
    {
        Task Insert(GetBlockResponse block);
    }
}
