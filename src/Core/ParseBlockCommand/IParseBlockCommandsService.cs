using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.ParseBlockCommand
{
    public interface IParseBlockCommandsService
    {
        Task ProduceParseBlockCommand(int blockHeight);
    }
}
