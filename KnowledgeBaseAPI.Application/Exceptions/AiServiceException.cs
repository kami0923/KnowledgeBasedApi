using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeBaseAPI.Application.Exceptions;

public class AiServiceException : Exception
{
    public int? StatusCode { get; }

    public AiServiceException(string message, int? statusCode = null) : base(message)
    {
        StatusCode = statusCode;
    }
}
