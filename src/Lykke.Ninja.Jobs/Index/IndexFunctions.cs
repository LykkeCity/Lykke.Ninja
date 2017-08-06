using System.Threading.Tasks;
using Common.Log;
using Core.Transaction;
using Lykke.JobTriggers.Triggers.Attributes;

namespace Jobs.Index
{
    public class IndexFunctions
    {
        private readonly ITransactionOutputRepository _outputRepository;
        private readonly IConsole _console;

        public IndexFunctions(ITransactionOutputRepository outputRepository, IConsole console)
        {
            _outputRepository = outputRepository;
            _console = console;
        }



        //[TimerTrigger("23:59:59")]
        //public async Task SetIndexes()
        //{
        //    _console.WriteLine($"{nameof(IndexFunctions)}.{nameof(SetIndexes)} Started");

            
        //    await _outputRepository.SetIndexes();
        //}
    }
}
