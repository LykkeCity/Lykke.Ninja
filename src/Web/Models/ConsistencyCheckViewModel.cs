﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.BlockStatus;
using Core.Transaction;

namespace Web.Models
{
    public class ConsistencyCheckViewModel
    {

        public ConsistencyCheckViewModel()
        {
            FailedBlocks = Enumerable.Empty<BlockStatusViewModel>();
            QueuedBlocks = Enumerable.Empty<BlockStatusViewModel>();
            WaitingInputs = Enumerable.Empty<TransactionInputViewModel>();
            NotFoundInputs = Enumerable.Empty<TransactionInputViewModel>();
        }

        public bool ConsistencyOk => NotFoundInputsCount == 0 && FailedBlocksCount == 0;

        public long FailedBlocksCount { get; set; }
        public long QueuedBlocksCount { get; set; }
        public long NotFoundInputsCount { get; set; }

        public int ParseBlockTasksQueuedCount { get; set; }

        public BlockStatusViewModel LastQueuedBlock { get; set; }

        public IEnumerable<BlockStatusViewModel> FailedBlocks { get; set; }

        public IEnumerable<BlockStatusViewModel> QueuedBlocks { get; set; }

        public long WaitingInputsCount { get; set; }

        public IEnumerable<TransactionInputViewModel> WaitingInputs { get; set; }


        public IEnumerable<TransactionInputViewModel> NotFoundInputs { get; set; }

        public static ConsistencyCheckViewModel Create(int parseBlockTasksQueuedCount, 
            IBlockStatus lastQueuedBlock,
            IEnumerable<ITransactionInput> waitingInputs,
            long waitingInputsCount,
            IEnumerable<ITransactionInput> notFoundInputs,
            long notFoundInputsCount,
            IEnumerable<IBlockStatus> failedBlocks,
            long failedBlocksCount,
            IEnumerable<IBlockStatus> queuedBlocks,
            long queuedBlocksCount)
        {
            return new ConsistencyCheckViewModel
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
                WaitingInputsCount = waitingInputsCount
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
}
