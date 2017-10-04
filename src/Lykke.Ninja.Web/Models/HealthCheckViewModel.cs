using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Ninja.Core.BlockStatus;
using Lykke.Ninja.Core.Ninja.Block;
using Lykke.Ninja.Core.Transaction;

namespace Lykke.Ninja.Web.Models
{
    public class HealthCheckViewModel
    {

        public HealthCheckViewModel()
        {
            FailedBlocks = Enumerable.Empty<BlockStatusViewModel>();
            QueuedBlocks = Enumerable.Empty<BlockStatusViewModel>();
            WaitingInputs = Enumerable.Empty<TransactionInputViewModel>();
            NotFoundInputs = Enumerable.Empty<TransactionInputViewModel>();
        }

        public int NinjaTopLag { get; set; }
        public bool ConsistencyOk => NotFoundInputsCount == 0 && FailedBlocksCount == 0;

        public UnconfirmedReportViewModel Unconfirmed { get; set; }

        public long FailedBlocksCount { get; set; }
        public long QueuedBlocksCount { get; set; }
        public long NotFoundInputsCount { get; set; }

        public int ParseBlockTasksQueuedCount { get; set; }
        public IEnumerable<BlockStatusViewModel> ProcessingBlocks { get; set; }
        public BlockStatusViewModel LastQueuedBlock { get; set; }

        public IEnumerable<BlockStatusViewModel> FailedBlocks { get; set; }

        public IEnumerable<BlockStatusViewModel> QueuedBlocks { get; set; }

        public long WaitingInputsCount { get; set; }

        public IEnumerable<TransactionInputViewModel> WaitingInputs { get; set; }


        public IEnumerable<TransactionInputViewModel> NotFoundInputs { get; set; }
        
        public static HealthCheckViewModel Create(int parseBlockTasksQueuedCount, 
            IBlockStatus lastQueuedBlock,
            IEnumerable<ITransactionInput> waitingInputs,
            long waitingInputsCount,
            IEnumerable<ITransactionInput> notFoundInputs,
            long notFoundInputsCount,
            IEnumerable<IBlockStatus> failedBlocks,
            long failedBlocksCount,
            IEnumerable<IBlockStatus> queuedBlocks,
            long queuedBlocksCount,
            IEnumerable<IBlockStatus> processingBlocks,
            INinjaBlockHeader ninjaTop,
            int lastSuccesfullyProcessedBlockHeight,
            long failedUnconfirmedTxCount,
            long allTxCount,
            long waitingTxCount)
        {
            return new HealthCheckViewModel
            {
                ParseBlockTasksQueuedCount = parseBlockTasksQueuedCount,
                LastQueuedBlock = BlockStatusViewModel.Create(lastQueuedBlock),
                NotFoundInputs = notFoundInputs.Select(TransactionInputViewModel.Create),
                WaitingInputs = waitingInputs.Select(TransactionInputViewModel.Create),
                FailedBlocks = failedBlocks.Select(BlockStatusViewModel.Create),
                QueuedBlocks = queuedBlocks.Select(BlockStatusViewModel.Create),
                FailedBlocksCount = failedBlocksCount,
                QueuedBlocksCount = queuedBlocksCount,
                NotFoundInputsCount = notFoundInputsCount,
                WaitingInputsCount = waitingInputsCount,
                ProcessingBlocks = processingBlocks.Select(BlockStatusViewModel.Create),
                NinjaTopLag = ninjaTop.BlockHeight - lastSuccesfullyProcessedBlockHeight,
                Unconfirmed = UnconfirmedReportViewModel.Create(failedUnconfirmedTxCount, allTxCount, waitingTxCount)
            };
        }
    }

    public class BlockStatusViewModel
    {
        public int Height { get; set; }

        public string BlockId { get; set; }

        public string ProcessingStatus { get; set; }
        public DateTime QueuedAt { get; set; }
        public DateTime StatusChangedAt { get; set; }

        public static BlockStatusViewModel Create(IBlockStatus source)
        {
            return new BlockStatusViewModel
            {
                BlockId = source.BlockId,
                Height = source.Height,
                ProcessingStatus = source.ProcessingStatus.ToString(),
                QueuedAt = source.QueuedAt,
                StatusChangedAt = source.StatusChangedAt
            };
        }
    }

    public class TransactionInputViewModel
    {
        public string Id { get; set; }

        public string TransactionId { get; set; }

        public string BlockId { get; set; }

        public int BlockHeight { get; set; }

        public uint Index { get; set; }

        public static TransactionInputViewModel Create(ITransactionInput source)
        {
            return new TransactionInputViewModel
            {
                BlockHeight = source.BlockHeight,
                BlockId = source.BlockId,
                Id = source.Id,
                Index = source.Index,
                TransactionId = source.TransactionId
            };
        }
    }

    public class UnconfirmedReportViewModel
    {
        public long AllTxCount { get; set; }
        public long WaitingTxCount { get; set; }
        public long FailedTxCount { get; set; }

        public static UnconfirmedReportViewModel Create(long failedTxCount,
            long allTxCount,
            long waitingTxCount)
        {
            return new UnconfirmedReportViewModel
            {
                FailedTxCount = failedTxCount,
                AllTxCount = allTxCount,
                WaitingTxCount = waitingTxCount
            };
        }
    }
}
