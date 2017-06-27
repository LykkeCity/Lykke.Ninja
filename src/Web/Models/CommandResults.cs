using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Web.Models
{
    public class CommandResult
    {
        public string[] ErrorMessages { get; set; }
        public bool Success { get; set; }
    }

    public class CommandResultWithModel<T>
    {
        public string[] ErrorMessages { get; set; }
        public bool Success { get; set; }

        public T Data { get; set; }
    }

    public static class CommandResultBuilder
    {
        public static CommandResult Ok()
        {
            return new CommandResult
            {
                Success = true,
                ErrorMessages = new string[0]
            };
        }

        public static CommandResult Fail(params string[] errorMessage)
        {
            return new CommandResult
            {
                Success = false,
                ErrorMessages = errorMessage
            };
        }

        public static CommandResultWithModel<T> Ok<T>(T model)
        {
            return new CommandResultWithModel<T>
            {
                Success = true,
                Data = model,
                ErrorMessages = new string[0]
            };
        }

        public static CommandResultWithModel<T> Fail<T>(params string[] errorMessage)
        {
            return new CommandResultWithModel<T>
            {
                Success = false,
                ErrorMessages = errorMessage
            };
        }
    }
}
