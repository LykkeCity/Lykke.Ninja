using System.Threading.Tasks;
using Core.Block;
using Core.Transaction;

namespace Jobs.BlockTasks
{
    public class InputFunctions
    {
        private readonly ITransactionInputRepository _inputRepository;
        private readonly IBlockService _blockService;

        public InputFunctions(ITransactionInputRepository inputRepository, 
            IBlockService blockService)
        {
            _inputRepository = inputRepository;
            _blockService = blockService;
        }

        public async Task SetNotFound()
        {
            
        }
    }
}
